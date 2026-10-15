// Service worker: makes the game load and play with no network, and fires the alerts the page left
// behind if the browser ever wakes us. Deliberately self-contained (no imports, no build step).
const CACHE = 'deadswitch-v1.36.1';
const FONTS = 'deadswitch-fonts-v1';
// The shell is enough to boot. Every other file is whatever the page has loaded: the page hands its
// list over once the worker is ready (js/main.js), and a new build re-fetches everything the last
// one held, so adding a file to the game never means remembering to add it here.
const SHELL = ['./', './index.html', './css/style.css', './js/main.js', './manifest.webmanifest', './icon.svg'];

/** Fetches a file past the HTTP cache, so a new build is never filled with the last one's files. */
const fresh = (url) => fetch(new Request(url, { cache: 'reload' })).then((res) => (res.ok ? res : Promise.reject(new Error(url))));

self.addEventListener('install', (e) => {
  e.waitUntil((async () => {
    const cache = await caches.open(CACHE);
    // The shell has to land, or this worker does not install and the last good one stays.
    await Promise.all(SHELL.map(async (url) => cache.put(url, await fresh(url))));
    // Everything the previous build had cached comes across as this build's copy, so a deploy does
    // not leave an offline launch with only a shell to boot from.
    const old = (await caches.keys()).filter((k) => k.startsWith('deadswitch-v') && k !== CACHE);
    for (const name of old) {
      const prev = await caches.open(name);
      await Promise.all((await prev.keys()).map(async (req) => {
        if (await cache.match(req)) return;
        try {
          await cache.put(req.url, await fresh(req.url));
        } catch (err) {
          // Offline right now: the old copy beats none. A file the server no longer has is let go.
          const keep = err instanceof TypeError ? await prev.match(req) : null;
          if (keep) await cache.put(req.url, keep);
        }
      }));
    }
    await self.skipWaiting();
  })());
});

// The page lists every file it loaded; whatever is not cached yet is fetched now, so the first
// visit is already enough to play offline.
self.addEventListener('message', (e) => {
  const urls = e.data && Array.isArray(e.data.cache) ? e.data.cache : [];
  e.waitUntil(caches.open(CACHE).then((cache) => Promise.all(urls
    .filter((u) => typeof u === 'string' && new URL(u).origin === location.origin)
    .map(async (u) => { if (!(await cache.match(u))) await cache.put(u, await fresh(u)).catch(() => {}); }))));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(caches.keys()
    .then((keys) => Promise.all(keys.filter((k) => k !== CACHE && k !== FONTS).map((k) => caches.delete(k))))
    .then(() => self.clients.claim())
    // Whatever woke us is a chance to say what came due; fireDue says nothing if anyone is watching.
    .then(() => fireDue('activate')));
});

/** The game's own files: the network's copy when it answers within a few seconds, so an update shows
 *  on the launch after it is deployed rather than the one after that; the stored copy otherwise. */
async function networkFirst(e, req, cacheName) {
  const cache = await caches.open(cacheName);
  const live = fetch(req, { cache: 'no-cache' }).then(async (res) => {
    if (res && res.ok) await cache.put(req, res.clone());
    return res && res.ok ? res : Promise.reject(new Error('bad response'));
  });
  e.waitUntil(live.catch(() => {})); // the copy lands in the store even if the slow path answered first
  const stored = () => cache.match(req);
  const slow = new Promise((resolve) => setTimeout(resolve, 4000)).then(stored).then((r) => r || live);
  return Promise.race([live, slow]).catch(async () => (await stored()) || new Response('', { status: 504 }));
}

/** Serve what we have, then quietly replace it, so the next launch has the newest build. */
async function staleWhileRevalidate(e, req, cacheName) {
  const cache = await caches.open(cacheName);
  const hit = await cache.match(req);
  const live = fetch(req, { cache: 'no-cache' }).then(async (res) => {
    // The fonts stylesheet comes back opaque (no-cors): without it the stored font files go unused.
    if (res && (res.ok || (res.type === 'opaque' && cacheName === FONTS))) await cache.put(req, res.clone());
    return res;
  }).catch(() => null);
  e.waitUntil(live); // the worker must not be stopped before the new copy is stored
  return hit || (await live) || new Response('', { status: 504 });
}

self.addEventListener('fetch', (e) => {
  const { request } = e;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (request.mode === 'navigate') {
    // Fresh page when there is a network, the cached shell when there is not. A launch that cannot
    // reach the network is the whole point of an installed app, so a bad response falls back too.
    // A network that answers nothing (a tunnel, a captive portal) gets four seconds before the shell.
    const shell = () => caches.match('./index.html').then((r) => r || caches.match('./'));
    const net = fetch(request).then((res) => (res && res.ok ? res : Promise.reject(new Error('bad response'))));
    const slow = new Promise((resolve) => setTimeout(resolve, 4000)).then(shell).then((r) => r || net);
    e.respondWith(Promise.race([net, slow]).catch(shell));
    return;
  }
  if (url.origin === location.origin) {
    e.respondWith(networkFirst(e, request, CACHE));
    return;
  }
  if (/fonts\.(googleapis|gstatic)\.com$/.test(url.hostname)) {
    e.respondWith(staleWhileRevalidate(e, request, FONTS));
  }
});

// ---------- alerts ----------
// The page leaves the times things are due in the save database, because by the time they are due
// the page itself may be gone. Nothing here simulates anything: it only reads what was left.

/** A transaction on a connection that closes behind it, so a later version bump is never blocked. */
function once(db, mode) {
  const t = db.transaction('save', mode);
  db.close(); // waits for the transaction above to finish
  return t;
}

function readDue() {
  return new Promise((resolve) => {
    let req;
    try {
      req = indexedDB.open('deadswitch', 1);
      // Should we ever open it before the page has, the store the page expects has to be there.
      req.onupgradeneeded = () => req.result.createObjectStore('save');
    } catch {
      resolve([]);
      return;
    }
    req.onerror = () => resolve([]);
    req.onsuccess = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains('save')) {
        resolve([]);
        return;
      }
      const get = once(db, 'readonly').objectStore('save').get('due');
      get.onsuccess = () => resolve(Array.isArray(get.result) ? get.result : []);
      get.onerror = () => resolve([]);
    };
  });
}

/** What is left to say. Anything already said is dropped, or the next wake repeats it. */
function writeDue(list) {
  return new Promise((resolve) => {
    let req;
    try {
      req = indexedDB.open('deadswitch', 1);
      // Should we ever open it before the page has, the store the page expects has to be there.
      req.onupgradeneeded = () => req.result.createObjectStore('save');
    } catch {
      resolve();
      return;
    }
    req.onerror = () => resolve();
    req.onsuccess = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains('save')) {
        resolve();
        return;
      }
      once(db, 'readwrite').objectStore('save').put(list, 'due');
      resolve();
    };
  });
}

const STALE = 8 * 60 * 60 * 1000; // the longest an absence counts for, so nothing older is worth saying
// An attack warning is only news while the attack is: a quarter of an hour after it was due, it landed.
const FRESH = { attack: 15 * 60 * 1000 };

/** Leaves a note that the browser woke us, and why. Without it there is no way to tell a wake that
 *  found nothing from a wake that never happened, which is the whole question with these alerts. */
function markWake(why, fired) {
  return new Promise((resolve) => {
    let req;
    try {
      req = indexedDB.open('deadswitch', 1);
      // Should we ever open it before the page has, the store the page expects has to be there.
      req.onupgradeneeded = () => req.result.createObjectStore('save');
    } catch {
      resolve();
      return;
    }
    req.onerror = () => resolve();
    req.onsuccess = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains('save')) {
        resolve();
        return;
      }
      const st = once(db, 'readwrite').objectStore('save');
      const get = st.get('wake');
      get.onsuccess = () => {
        const was = get.result || { count: 0 };
        st.put({ at: Date.now(), why, fired, count: (was.count || 0) + 1 }, 'wake');
        resolve();
      };
      get.onerror = () => resolve();
    };
  });
}

async function fireDue(why) {
  // Someone watching the game does not need telling.
  const open = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
  if (open.some((c) => c.visibilityState === 'visible')) return;
  const now = Date.now();
  const all = await readDue();
  // The browser decides when it wakes us, and it is usually long after the first thing came due.
  // Per kind of news, the most recent one that has passed is the only one worth saying: three stale
  // raid warnings are a nuisance, but a raid, an order and a finished build are three things the
  // commander wants to know, and each gets its own tag so it does not bury the others.
  const latest = new Map();
  for (const x of all) {
    if (x.at <= now && now - x.at < (FRESH[x.kind || 'attack'] ?? STALE)) latest.set(x.kind || 'attack', x);
  }
  for (const [kind, item] of latest) {
    try {
      await self.registration.showNotification(item.title, {
        body: item.body, tag: `deadswitch-${kind}`, renotify: true, timestamp: item.at,
        icon: './icons/icon-192.png', badge: './icons/icon-192.png',
      });
    } catch { /* permission taken back since: the wake is still worth writing down */ }
  }
  // What has been said is spent: without this the next wake repeats it and the one still to come
  // never gets a turn.
  if (latest.size) await writeDue(all.filter((x) => x.at > now));
  await markWake(why, latest.size);
}

self.addEventListener('periodicsync', (e) => {
  if (e.tag === 'deadswitch-check') e.waitUntil(fireDue('periodicsync'));
});
self.addEventListener('sync', (e) => {
  if (e.tag === 'deadswitch-check') e.waitUntil(fireDue('sync'));
});
// A server could send one of these; there is none, but the handler costs nothing and is ready.
self.addEventListener('push', (e) => e.waitUntil(fireDue('push')));

self.addEventListener('notificationclick', (e) => {
  e.notification.close();
  e.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((list) => {
    const open = list.find((c) => 'focus' in c);
    return open ? open.focus() : self.clients.openWindow('./');
  }));
});
