/* Service worker: бързо зареждане на статичните файлове и страница при липса на връзка.
   Никога не кешира /api, плащания, админ или потребителски данни. */
const VERSION = 'v1';
const STATIC_CACHE = 'pchelari-static-' + VERSION;
const PAGE_CACHE = 'pchelari-pages-' + VERSION;
const OFFLINE_URL = '/offline.html';
const PRECACHE = [OFFLINE_URL, '/pages.css', '/images/icon-192.png'];

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(STATIC_CACHE).then((c) => c.addAll(PRECACHE)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((k) => ![STATIC_CACHE, PAGE_CACHE].includes(k)).map((k) => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

const NEVER_CACHE = [/^\/api\//, /^\/admin/, /^\/panel/, /^\/account/, /^\/uploads\//, /^\/sw\.js$/];

self.addEventListener('fetch', (event) => {
  const req = event.request;
  if (req.method !== 'GET') return;
  const url = new URL(req.url);
  if (url.origin !== self.location.origin) return;
  if (NEVER_CACHE.some((re) => re.test(url.pathname))) return;

  // Страници: първо мрежата, при липса на връзка – последната копие или страницата „Няма връзка“
  if (req.mode === 'navigate') {
    event.respondWith(
      fetch(req)
        .then((res) => {
          if (res.ok) { const copy = res.clone(); caches.open(PAGE_CACHE).then((c) => c.put(req, copy)); }
          return res;
        })
        .catch(() => caches.match(req).then((hit) => hit || caches.match(OFFLINE_URL)))
    );
    return;
  }

  // Статични файлове: от кеша веднага, обновяване във фонов режим
  if (/\.(css|js|png|jpg|jpeg|webp|svg|ico|woff2?|webmanifest|json)$/i.test(url.pathname)) {
    event.respondWith(
      caches.open(STATIC_CACHE).then((cache) =>
        cache.match(req).then((hit) => {
          const network = fetch(req).then((res) => { if (res.ok) cache.put(req, res.clone()); return res; }).catch(() => hit);
          return hit || network;
        })
      )
    );
  }
});
