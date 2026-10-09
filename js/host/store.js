// Where the save lives: one record in IndexedDB, written at most once every WRITE_GAP seconds and
// always on the way out. Beside it, a timestamp mirror in localStorage, written synchronously: an
// IndexedDB write started as the page is closing is not guaranteed to land, and the time the player
// was away has to survive that. If IndexedDB is unavailable (private mode, old browser), everything
// falls back to localStorage and the game plays exactly the same.
const DB = 'deadswitch';
const STORE = 'save';
const KEY = 'current';
const LEGACY = 'deadswitch.save'; // where saves lived before IndexedDB
const SEEN = 'deadswitch.seen'; // { at } mirror, written on every hide
const WRITE_GAP = 4000; // ms between IndexedDB writes while playing

let db = null;
let broken = false; // IndexedDB refused us: use localStorage for everything
let pending = null; // the newest state waiting to be written
let timer = 0;
let lastWrite = 0;

function open() {
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
    req.onerror = () => { broken = true; resolve(null); };
    req.onblocked = () => { broken = true; resolve(null); };
  });
}

function tx(mode, run) {
  return open().then((d) => (d ? new Promise((resolve) => {
    let t;
    try {
      t = d.transaction(STORE, mode);
    } catch {
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
export async function read() {
  const rec = await tx('readonly', (st) => st.get(KEY));
  if (rec) {
    return rec;
  }
  const old = local.read();
  if (old && !broken) {
    await tx('readwrite', (st) => st.put(old, KEY));
    local.clear(); // one source of truth from here on
  }
  return old;
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
    // IndexedDB gave up (quota, private mode): keep the save alive in localStorage instead.
    if (ok === null && !broken) {
      broken = true;
      local.write(rec);
    }
  });
}

/** Leaves the service worker the times things are due, for when the page itself is no longer running. */
export function setDue(items) {
  if (!broken) tx('readwrite', (st) => st.put(items, 'due'));
}

export async function clear() {
  pending = null;
  clearTimeout(timer);
  timer = 0;
  local.clear();
  markSeen(0);
  await tx('readwrite', (st) => st.delete(KEY));
}
