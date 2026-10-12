// Service worker: makes the game load and play with no network, and fires the alerts the page left
// behind if the browser ever wakes us. Deliberately self-contained (no imports, no build step).
const CACHE = 'deadswitch-v1.19.2';
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
    .then(() => self.clients.claim())
    // Whatever woke us is a chance to say what came due; fireDue says nothing if anyone is watching.
    .then(() => fireDue('activate')));
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

const STALE = 8 * 60 * 60 * 1000; // the longest an absence counts for, so nothing older is worth saying

/** Leaves a note that the browser woke us, and why. Without it there is no way to tell a wake that
 *  found nothing from a wake that never happened, which is the whole question with these alerts. */
function markWake(why, fired) {
  return new Promise((resolve) => {
    let req;
    try {
      req = indexedDB.open('deadswitch', 1);
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
  // The browser decides when it wakes us, and it is usually long after the first thing came due, so
  // take the most recent one that has passed rather than the oldest and drop the rest: one alert is a
  // warning, several are a nuisance. A tight staleness window here meant a late wake said nothing.
  const passed = (await readDue()).filter((x) => x.at <= now && now - x.at < STALE);
  const item = passed[passed.length - 1];
  if (item) {
    await self.registration.showNotification(item.title, {
      body: item.body, tag: 'deadswitch-attack', renotify: true, icon: './icon.svg', badge: './icon.svg',
    });
  }
  await markWake(why, !!item);
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
