// Where the save lives: one record in IndexedDB, written at most once every WRITE_GAP seconds and
// always on the way out. Beside it, a timestamp mirror in localStorage, written synchronously: an
// IndexedDB write started as the page is closing is not guaranteed to land, and the time the player
// was away has to survive that. If IndexedDB is unavailable (private mode, old browser), everything
// falls back to localStorage and the game plays exactly the same.
const DB = 'deadswitch';
const STORE = 'save';
const KEY = 'current';
const BACKUP = 'previous'; // the record the session booted from, kept in case the live one goes bad
const LEGACY = 'deadswitch.save'; // where saves lived before IndexedDB
const SEEN = 'deadswitch.seen'; // { at } mirror, written on every hide
const WRITE_GAP = 4000; // ms between IndexedDB writes while playing

let db = null;
let broken = false; // IndexedDB refused us: use localStorage for everything
let pending = null; // the newest state waiting to be written
let timer = 0;
let lastWrite = 0;
let spilled = false; // a write fell back to localStorage this session; clear it once IndexedDB takes one again

function open(retry = true) {
  if (db || broken) {
    return Promise.resolve(db);
  }
  return new Promise((resolve) => {
    let req;
    try {
      req = indexedDB.open(DB, 1);
    } catch {
      broken = true;
      resolve(null);
      return;
    }
    req.onupgradeneeded = () => req.result.createObjectStore(STORE);
    req.onsuccess = () => {
      db = req.result;
      // A second tab upgrading or deleting the database must not leave us writing into a dead handle.
      db.onclose = () => { db = null; };
      resolve(db);
    };
    // One refusal is often a passing one (a busy disk, another tab mid-upgrade): ask once more first.
    const refused = () => {
      if (retry) {
        setTimeout(() => resolve(open(false)), 150);
      } else {
        broken = true;
        resolve(null);
      }
    };
    req.onerror = refused;
    req.onblocked = refused;
  });
}

function tx(mode, run) {
  return open().then((d) => (d ? new Promise((resolve) => {
    let t;
    try {
      t = d.transaction(STORE, mode);
    } catch {
      db = null; // a lost connection stays lost: the next call opens a new one
      resolve(null);
      return;
    }
    const req = run(t.objectStore(STORE));
    t.oncomplete = () => resolve(req ? req.result : null);
    t.onerror = () => resolve(null);
    t.onabort = () => resolve(null);
  }) : null));
}

const local = {
  read() {
    try {
      const raw = localStorage.getItem(LEGACY);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  },
  write(rec) {
    try {
      localStorage.setItem(LEGACY, JSON.stringify(rec));
    } catch {
      // Storage blocked: the game still runs, it just will not persist.
    }
  },
  clear() {
    try {
      localStorage.removeItem(LEGACY);
    } catch { /* nothing to do */ }
  },
};

/** The saved record, taking over a pre-IndexedDB save the first time. */
// A browser may evict a site's storage to reclaim space, which for an idle game means losing a run
// that was never anywhere else. Asking once marks the save as worth keeping; it is granted silently
// on an installed app and on sites the player returns to, and refused quietly everywhere else.
export async function keepStorage() {
  try {
    if (!navigator.storage || !navigator.storage.persist || await navigator.storage.persisted()) {
      return false;
    }
    return await navigator.storage.persist();
  } catch {
    return false; // not supported, or the browser said no: nothing changes either way
  }
}

/** How much room the save is allowed and using, for the developer panel. */
export async function storageUse() {
  try {
    const e = navigator.storage && navigator.storage.estimate ? await navigator.storage.estimate() : null;
    const kept = navigator.storage && navigator.storage.persisted ? await navigator.storage.persisted() : false;
    return e ? { kept, used: e.usage || 0, quota: e.quota || 0 } : { kept, used: 0, quota: 0 };
  } catch {
    return { kept: false, used: 0, quota: 0 };
  }
}

// A missing record reads as undefined and a failed read as null, and the difference matters: a run
// that could not be read must never be mistaken for no run at all, or the fresh one overwrites it.
export async function read() {
  let rec = await tx('readonly', (st) => st.get(KEY));
  if (rec === null && !broken) {
    rec = await tx('readonly', (st) => st.get(KEY)); // a dropped connection usually answers twice
  }
  const failed = rec === null && !broken;
  if (failed) {
    return { failed: true }; // whatever localStorage holds cannot be weighed against a run we could not read
  }
  const old = local.read();
  // localStorage holds either a pre-IndexedDB save or what a session wrote after IndexedDB refused a
  // write. Either way the newer record is the run, but only if it is the same run: a session that
  // could not open the database started a fresh game there, and that must never replace the real one.
  const same = rec && rec.state && old && old.state && old.state.rng === rec.state.rng;
  if (old && old.state && (!rec || (same && (old.savedAt || 0) > (rec.savedAt || 0)))) {
    if (!broken) {
      const ok = await tx('readwrite', (st) => st.put(old, KEY));
      if (ok !== null) local.clear(); // one source of truth from here on, once the copy has landed
    }
    return old;
  }
  return rec || null;
}

/** The last moment the player was known to be here, however the page went away. */
export function lastSeen() {
  try {
    return Number(localStorage.getItem(SEEN)) || 0;
  } catch {
    return 0;
  }
}

/** Synchronous and cheap, so it can run in pagehide, where an IndexedDB write may never land. */
export function markSeen(at = Date.now()) {
  try {
    localStorage.setItem(SEEN, String(at));
  } catch { /* nothing to do */ }
}

/** Queues a save. Writes are spaced out while playing; `now` forces one (leaving, or a real choice). */
export function write(state, now = false) {
  pending = { state, savedAt: Date.now() };
  markSeen(pending.savedAt);
  if (broken) {
    local.write(pending);
    pending = null;
    return;
  }
  const due = Date.now() - lastWrite >= WRITE_GAP;
  if (now || due) {
    flush();
  } else if (!timer) {
    timer = setTimeout(flush, WRITE_GAP - (Date.now() - lastWrite));
  }
}

export function flush() {
  clearTimeout(timer);
  timer = 0;
  if (!pending) {
    return;
  }
  const rec = pending;
  pending = null;
  lastWrite = Date.now();
  if (broken) {
    local.write(rec);
    return;
  }
  tx('readwrite', (st) => st.put(rec, KEY)).then((ok) => {
    // IndexedDB gave up on this one (quota, a dropped connection): keep it alive in localStorage.
    // The next write tries IndexedDB again, and read() takes whichever of the two is newer.
    if (ok === null && !broken) {
      spilled = true;
      local.write(rec);
    } else if (ok !== null && spilled) {
      spilled = false;
      local.clear();
    }
  });
}

/** Forgets a write that has not gone out yet: the state it holds is not one to keep. */
export function drop() {
  pending = null;
  clearTimeout(timer);
  timer = 0;
}

/** Keeps the record this session started from, so a save that goes bad is not the end of the run. */
export function keepBackup(rec) {
  if (rec && rec.state && !broken) tx('readwrite', (st) => st.put(rec, BACKUP));
}

export const readBackup = () => tx('readonly', (st) => st.get(BACKUP));

/** Leaves the service worker the times things are due, for when the page itself is no longer running. */
export function setDue(items) {
  if (!broken) tx('readwrite', (st) => st.put(items, 'due'));
}

/** The note the service worker leaves each time the browser wakes it, for the alerts readout. */
export async function readWake() {
  const out = broken ? null : await tx('readonly', (st) => st.get('wake'));
  return out && Number.isFinite(out.at) ? out : null;
}

/** What the service worker would find waiting, for the alerts readout. */
export async function getDue() {
  const out = broken ? null : await tx('readonly', (st) => st.get('due'));
  return Array.isArray(out) ? out : [];
}

export async function clear() {
  pending = null;
  clearTimeout(timer);
  timer = 0;
  local.clear();
  markSeen(0);
  await tx('readwrite', (st) => st.delete(KEY));
}
