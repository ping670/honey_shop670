/* Въвеждаща анимация: сайтът се отваря със заставка, която се върти със скрол,
   и след това се разкрива истинската страница. Показва се веднъж на сесия. */
(function () {
  'use strict';
  var KEY = 'pb_intro_seen';
  var root = document.documentElement;
  var skip = false;
  try {
    var q = location.search;
    skip = /[?&](nointro|paid|payment)\b/.test(q) || !!location.hash ||
      (window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches);
  } catch (e) { skip = false; }
  if (skip) return;

  root.classList.add('intro-on');

  var TOTAL = 1400;            // „път“ в пиксели от скрола, нужен за пълната анимация
  var target = 0, cur = 0, done = false, el, raf = 0, lines = [];
  var STAGES = [
    { a: 0.06, b: 0.34, html: 'Пчелари <em>България</em><small>Обединени български пчелини</small>' },
    { a: 0.34, b: 0.62, html: 'От Странджа до <em>Родопите</em><small>Над 100 семейни пчелини</small>' },
    { a: 0.62, b: 0.88, html: 'Директно от <em>кошера</em><small>до вашата маса</small>' }
  ];
  var clamp = function (v, lo, hi) { return Math.min(hi, Math.max(lo, v)); };
  // 0→1→0 прозорец с плавни ръбове
  var win = function (p, a, b) { var m = (b - a) * 0.28; return clamp(Math.min((p - a) / m, (b - p) / m), 0, 1); };

  function build() {
    el = document.createElement('div');
    el.id = 'intro'; el.setAttribute('role', 'dialog'); el.setAttribute('aria-label', 'Добре дошли');
    var hex = '<svg class="in-hex" viewBox="-100 -100 200 200" aria-hidden="true">';
    [96, 72, 48, 24].forEach(function (r) {
      var pts = []; for (var i = 0; i < 6; i++) { var a = Math.PI / 3 * i - Math.PI / 6; pts.push((r * Math.cos(a)).toFixed(1) + ',' + (r * Math.sin(a)).toFixed(1)); }
      hex += '<polygon points="' + pts.join(' ') + '"/>';
    });
    hex += '</svg>';
    el.innerHTML = '<div class="in-bg"></div><div class="in-vignette"></div>' + hex +
      '<img class="in-logo" src="images/logo.jpg" alt="">' +
      STAGES.map(function (s) { return '<div class="in-line">' + s.html + '</div>'; }).join('') +
      '<div class="in-hint"><span class="in-mouse"></span>Скролнете, за да влезете</div>' +
      '<div class="in-bar"></div><button type="button" class="in-skip">Пропусни ›</button>';
    document.body.appendChild(el);
    lines = [].slice.call(el.querySelectorAll('.in-line'));
    el.querySelector('.in-skip').addEventListener('click', function () { finish(true); });
    el.addEventListener('click', function (e) { if (e.target.closest('.in-skip')) return; target = Math.min(1, target + 0.12); kick(); });
  }

  function add(deltaPx) { if (done) return; target = clamp(target + deltaPx / TOTAL, 0, 1); kick(); }
  function kick() { if (!raf) raf = requestAnimationFrame(frame); }

  function render(p) {
    var s = el.style;
    s.setProperty('--p', p.toFixed(4));
    s.setProperty('--bg-scale', (1.05 + p * 0.5).toFixed(3));
    s.setProperty('--bg-bright', (0.35 + Math.min(p, 0.9) * 0.45).toFixed(3));
    s.setProperty('--hex-rot', (p * 70).toFixed(1) + 'deg');
    s.setProperty('--hex-scale', (0.6 + p * 1.4).toFixed(3));
    s.setProperty('--hex-op', clamp(Math.min(p / 0.1, (1 - p) / 0.25), 0, 1).toFixed(3));
    s.setProperty('--logo-op', clamp(p / 0.08, 0, 1).toFixed(3));
    s.setProperty('--logo-s', (0.6 + clamp(p / 0.2, 0, 1) * 0.4 + p * 0.3).toFixed(3));
    s.setProperty('--logo-y', (-clamp(p / 0.3, 0, 1) * 90).toFixed(1) + 'px');
    s.setProperty('--hint-op', clamp(1 - p / 0.1, 0, 1).toFixed(3));
    lines.forEach(function (l, i) {
      var w = win(p, STAGES[i].a, STAGES[i].b);
      l.style.opacity = w.toFixed(3);
      l.style.transform = 'translateY(' + ((1 - w) * 24).toFixed(1) + 'px)';
    });
    // финал: приближаване към сайта и разкриване
    var out = clamp((p - 0.88) / 0.12, 0, 1);
    s.setProperty('--out-scale', (1 + out * 0.6).toFixed(3));
    s.setProperty('--out-opacity', (1 - out).toFixed(3));
  }

  function frame() {
    raf = 0;
    cur += (target - cur) * 0.12;
    if (Math.abs(target - cur) < 0.0004) cur = target;
    render(cur);
    if (cur >= 0.999) return finish(false);
    if (cur !== target) raf = requestAnimationFrame(frame);
  }

  function finish(fast) {
    if (done) return; done = true;
    cancelAnimationFrame(raf);
    try { sessionStorage.setItem(KEY, '1'); } catch (e) {}
    window.removeEventListener('wheel', onWheel, { passive: false });
    window.removeEventListener('touchstart', onTouchStart);
    window.removeEventListener('touchmove', onTouchMove);
    window.removeEventListener('keydown', onKey);
    var end = function () {
      if (el && el.parentNode) el.parentNode.removeChild(el);
      root.classList.remove('intro-on');
      window.scrollTo(0, 0);
      window.dispatchEvent(new Event('scroll'));
    };
    if (fast) { el.classList.add('closing'); root.classList.remove('intro-on'); setTimeout(end, 650); }
    else end();
  }

  function onWheel(e) { e.preventDefault(); add(e.deltaMode === 1 ? e.deltaY * 32 : e.deltaY); }
  var ty = null;
  function onTouchStart(e) { ty = e.touches[0].clientY; }
  function onTouchMove(e) { if (ty === null) return; var y = e.touches[0].clientY; add((ty - y) * 2.2); ty = y; e.preventDefault && e.cancelable && e.preventDefault(); }
  function onKey(e) {
    if (e.key === 'Escape') return finish(true);
    if (['ArrowDown', 'PageDown', ' ', 'Enter'].indexOf(e.key) >= 0) { e.preventDefault(); add(e.key === 'ArrowDown' ? 160 : 420); }
    else if (e.key === 'End') { e.preventDefault(); add(TOTAL); }
    else if (['ArrowUp', 'PageUp'].indexOf(e.key) >= 0) { e.preventDefault(); add(-300); }
  }

  function start() {
    build(); render(0);
    window.addEventListener('wheel', onWheel, { passive: false });
    window.addEventListener('touchstart', onTouchStart, { passive: true });
    window.addEventListener('touchmove', onTouchMove, { passive: false });
    window.addEventListener('keydown', onKey);
  }
  if (document.body) start(); else document.addEventListener('DOMContentLoaded', start);
})();
