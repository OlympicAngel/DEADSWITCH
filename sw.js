// Service worker: makes the game load and play with no network, and fires the alerts the page left
// behind if the browser ever wakes us. Deliberately self-contained (no imports, no build step).
const CACHE = 'deadswitch-v1.12';
const FONTS = 'deadswitch-fonts-v1';
// The shell is enough to boot; everything else is cached the first time it is asked for, so adding
// a file to the game never means remembering to add it here.
const SHELL = ['./', './index.html', './css/style.css', './js/main.js', './manifest.webmanifest', './icon.svg'];

self.addEventListener('install', (e) => {
  // One missing file must not stop the worker installing: without it the app has no offline copy at
  // all, and a half-cached shell still works because everything else is cached on first use.
  e.waitUntil(caches.open(CACHE)
    .then((c) => Promise.all(SHELL.map((url) => c.add(url).catch(() => {}))))
    .then(() => self.skipWaiting()));
});

self.addEventListener('activate', (e) => {
  e.waitUntil(caches.keys()
    .then((keys) => Promise.all(keys.filter((k) => k !== CACHE && k !== FONTS).map((k) => caches.delete(k))))
    .then(() => self.clients.claim()));
});

/** Serve what we have, then quietly replace it, so a reload always has the newest build. */
async function staleWhileRevalidate(req, cacheName) {
  const cache = await caches.open(cacheName);
  const hit = await cache.match(req);
  const live = fetch(req).then((res) => {
    if (res && res.ok) cache.put(req, res.clone());
    return res;
  }).catch(() => null);
  return hit || live || new Response('', { status: 504 });
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
    e.respondWith(staleWhileRevalidate(request, CACHE));
    return;
  }
  if (/fonts\.(googleapis|gstatic)\.com$/.test(url.hostname)) {
    e.respondWith(staleWhileRevalidate(request, FONTS));
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

async function fireDue() {
  // Someone watching the game does not need telling.
  const open = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
  if (open.some((c) => c.visibilityState === 'visible')) return;
  const now = Date.now();
  for (const item of await readDue()) {
    if (item.at <= now && now - item.at < 30 * 60 * 1000) {
      await self.registration.showNotification(item.title, {
        body: item.body, tag: 'deadswitch-attack', renotify: true, icon: './icon.svg', badge: './icon.svg',
      });
      break; // one alert is a warning; several are a nuisance
    }
  }
}

self.addEventListener('periodicsync', (e) => {
  if (e.tag === 'deadswitch-check') e.waitUntil(fireDue());
});
self.addEventListener('sync', (e) => {
  if (e.tag === 'deadswitch-check') e.waitUntil(fireDue());
});
// A server could send one of these; there is none, but the handler costs nothing and is ready.
self.addEventListener('push', (e) => e.waitUntil(fireDue()));

self.addEventListener('notificationclick', (e) => {
  e.notification.close();
  e.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((list) => {
    const open = list.find((c) => 'focus' in c);
    return open ? open.focus() : self.clients.openWindow('./');
  }));
});
