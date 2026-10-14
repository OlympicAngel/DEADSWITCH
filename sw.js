// Service worker: makes the game load and play with no network, and fires the alerts the page left
// behind if the browser ever wakes us. Deliberately self-contained (no imports, no build step).
const CACHE = 'deadswitch-v1.27';
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
        } catch {
          const keep = await prev.match(req);
          if (keep) await cache.put(req.url, keep); // offline right now: the old copy beats none
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

/** Serve what we have, then quietly replace it, so the next launch has the newest build. */
async function staleWhileRevalidate(e, req, cacheName) {
  const cache = await caches.open(cacheName);
  const hit = await cache.match(req);
  const live = fetch(req, { cache: 'no-cache' }).then(async (res) => {
    if (res && res.ok) await cache.put(req, res.clone());
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
    e.respondWith(fetch(request)
      .then((res) => (res && res.ok ? res : Promise.reject(new Error('bad response'))))
      .catch(() => caches.match('./index.html').then((r) => r || caches.match('./'))));
    return;
  }
  if (url.origin === location.origin) {
    e.respondWith(staleWhileRevalidate(e, request, CACHE));
    return;
  }
  if (/fonts\.(googleapis|gstatic)\.com$/.test(url.hostname)) {
    e.respondWith(staleWhileRevalidate(e, request, FONTS));
  }
});

// ---------- alerts ----------
// The page leaves the times things are due in the save database, because by the time they are due
// the page itself may be gone. Nothing here simulates anything: it only reads what was left.

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
      const get = db.transaction('save', 'readonly').objectStore('save').get('due');
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
      db.transaction('save', 'readwrite').objectStore('save').put(list, 'due');
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
      const st = db.transaction('save', 'readwrite').objectStore('save');
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
