/* ==========================================================================
   Общи помощници за всички страници: API, екраниране на HTML, известия,
   проверка „не съм робот“, акаунт и любими.
   Зарежда се след i18n.js и преди останалите скриптове.
   ========================================================================== */
(function () {
  'use strict';
  const tr = (s) => (window.I18N ? window.I18N.t(s) : s);
  const PB = (window.PB = {});

  PB.esc = function (v) {
    return String(v ?? '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
  };

  /** Позволява само безопасни адреси на снимки/връзки (същото правило като на сървъра). */
  PB.safeUrl = function (u, fallback) {
    u = String(u || '').trim();
    if (!u) return fallback || '';
    if (/^(https?:\/\/|\.?\/(?!\/)|[a-z0-9_-]+\/)/i.test(u) && !/^(javascript|data|vbscript):/i.test(u) && !u.includes('..')) return u;
    return fallback || '';
  };

  PB.money = (n) => Number(n || 0).toFixed(2) + ' €';
  PB.date = (iso, withTime) => {
    const d = new Date(iso);
    const loc = window.I18N && window.I18N.lang === 'en' ? 'en-GB' : 'bg-BG';
    return withTime ? d.toLocaleString(loc, { dateStyle: 'medium', timeStyle: 'short' }) : d.toLocaleDateString(loc, { dateStyle: 'medium' });
  };

  /** Заявка към API-то. Връща JSON; при opts.meta връща {data, total}. Грешките са Error със status. */
  PB.api = async function (path, opts = {}) {
    const headers = Object.assign({}, opts.body && !(opts.body instanceof FormData) ? { 'Content-Type': 'application/json' } : {}, opts.headers || {});
    const { meta, ...rest } = opts;
    let res;
    try {
      res = await fetch('/api' + path, { credentials: 'same-origin', ...rest, headers });
    } catch (e) {
      const err = new Error(tr('Няма връзка със сървъра. Опитайте отново.'));
      err.status = 0;
      throw err;
    }
    let data = null;
    if ((res.headers.get('content-type') || '').includes('json')) {
      try { data = await res.json(); } catch (e) { /* няма тяло */ }
    }
    if (!res.ok) {
      let msg = '';
      if (data) msg = data.errors ? Object.values(data.errors).flat().join(' ') : (data.message || data.detail || data.title || '');
      if (res.status === 429) msg = tr('Твърде много опити. Изчакайте малко и опитайте отново.');
      const err = new Error(msg || tr('Грешка') + ' ' + res.status);
      err.status = res.status;
      throw err;
    }
    return meta ? { data, total: parseInt(res.headers.get('X-Total-Count') || '0', 10) } : data;
  };

  /** Визуална лента с фазите на поръчката. events (по избор) дава датите на всяка фаза. */
  PB.STEPS = ['Приета', 'Обработва се', 'Изпратена', 'Доставена'];
  PB.stepper = function (status, events) {
    if (status === 'Отказана') {
      return '<div class="stepper cancelled" role="img" aria-label="' + PB.esc(tr('Отказана')) + '"><span class="step-cancel">✕ ' + PB.esc(tr('Отказана')) + '</span></div>';
    }
    const cur = Math.max(0, PB.STEPS.indexOf(status));
    const when = {};
    (events || []).forEach((e) => { if (!when[e.status]) when[e.status] = e.at; });
    return '<ol class="stepper" aria-label="' + PB.esc(tr('Фаза на поръчката')) + '">' + PB.STEPS.map((s, i) =>
      '<li class="' + (i < cur ? 'done' : i === cur ? (i === PB.STEPS.length - 1 ? 'done current' : 'current') : '') + '"' + (i === cur ? ' aria-current="step"' : '') + '>' +
      '<span class="dot">' + (i < cur || (i === cur && i === PB.STEPS.length - 1) ? '✓' : i + 1) + '</span><b>' + PB.esc(tr(s)) + '</b>' +
      (when[s] ? '<time>' + PB.esc(PB.date(when[s], true)) + '</time>' : '') + '</li>').join('') + '</ol>';
  };

  PB.toast = function (msg) {
    if (typeof window.showToast === 'function') return window.showToast(msg);
    let c = document.getElementById('pb-toasts');
    if (!c) {
      c = document.createElement('div');
      c.id = 'pb-toasts';
      c.style.cssText = 'position:fixed;bottom:18px;left:50%;transform:translateX(-50%);z-index:9999;display:flex;flex-direction:column;gap:8px;align-items:center;max-width:92vw';
      document.body.appendChild(c);
    }
    const t = document.createElement('div');
    t.textContent = msg;
    t.setAttribute('role', 'status');
    t.style.cssText = 'background:#2B1A0E;color:#FAF5EE;border:1px solid #F59E0B;padding:10px 16px;border-radius:10px;box-shadow:0 8px 24px rgba(0,0,0,.4);font-size:.92rem';
    c.appendChild(t);
    setTimeout(() => t.remove(), 3500);
  };

  // ---------- Конфигурация ----------
  let configPromise = null;
  PB.config = function () {
    if (!configPromise) {
      configPromise = PB.api('/config').catch(() => ({ cardPaymentsEnabled: false, captchaEnabled: false, freeShippingFrom: 25, subscriptionDiscountPercent: 5, couriers: [] }));
    }
    return configPromise;
  };

  // ---------- „Не съм робот“ ----------
  /** Показва проста задача и връща {headers(), reload(), valid()}. Токенът е еднократен – презареждайте след всеки опит. */
  PB.captcha = {
    async mount(host) {
      const cfg = await PB.config();
      if (!cfg.captchaEnabled || !host) return { headers: () => ({}), reload: async () => {}, valid: () => true };
      host.innerHTML = '<div class="captcha-box"><span>🤖 <b data-q>…</b></span><input inputmode="numeric" autocomplete="off" maxlength="3" aria-label="' +
        PB.esc(tr('Отговор')) + '"><button type="button" title="' + PB.esc(tr('Нов въпрос')) + '" aria-label="' + PB.esc(tr('Нов въпрос')) + '">↻</button></div>';
      const q = host.querySelector('[data-q]');
      const input = host.querySelector('input');
      let token = '';
      const reload = async () => {
        input.value = '';
        try {
          const c = await PB.api('/captcha');
          token = c.token;
          q.textContent = c.question;
        } catch (e) { q.textContent = tr('Неуспешно зареждане'); token = ''; }
      };
      host.querySelector('button').addEventListener('click', reload);
      await reload();
      return {
        headers: () => ({ 'X-Captcha-Token': token, 'X-Captcha-Answer': input.value.trim() }),
        reload,
        valid: () => token !== '' && input.value.trim() !== ''
      };
    }
  };

  // ---------- Акаунт ----------
  let meCache;
  PB.me = async function (force) {
    if (meCache !== undefined && !force) return meCache;
    try { meCache = await PB.api('/account/me'); } catch (e) { meCache = null; }
    return meCache;
  };
  PB.login = async (email, password) => { meCache = await PB.api('/account/login', { method: 'POST', body: JSON.stringify({ email, password }) }); return meCache; };
  PB.register = async (dto, captchaHeaders) => { meCache = await PB.api('/account/register', { method: 'POST', body: JSON.stringify(dto), headers: captchaHeaders }); return meCache; };
  PB.logout = async () => { try { await PB.api('/account/logout', { method: 'POST' }); } catch (e) { /* без значение */ } meCache = null; };

  // ---------- Любими (гост: в браузъра; влязъл: в базата) ----------
  const WL_KEY = 'pchelari_wishlist';
  const readLocal = () => { try { return (JSON.parse(localStorage.getItem(WL_KEY)) || []).map(Number).filter(Boolean); } catch (e) { return []; } };
  const writeLocal = (a) => { try { localStorage.setItem(WL_KEY, JSON.stringify(a)); } catch (e) { /* частен режим */ } };
  const WL = (PB.wishlist = { ids: new Set(readLocal()), listeners: [] });
  WL.onChange = (fn) => WL.listeners.push(fn);
  const emit = () => WL.listeners.forEach((f) => { try { f(); } catch (e) { /* ignore */ } });
  WL.has = (id) => WL.ids.has(Number(id));
  WL.sync = async function () {
    const me = await PB.me();
    if (me) {
      try {
        const local = readLocal();
        const merged = local.length ? await PB.api('/account/wishlist/merge', { method: 'POST', body: JSON.stringify({ productIds: local }) }) : null;
        if (local.length) writeLocal([]);
        const ids = Array.isArray(merged) ? merged : await PB.api('/account/wishlist/ids');
        WL.ids = new Set((ids || []).map(Number));
      } catch (e) { /* остава локалният списък */ }
    } else {
      WL.ids = new Set(readLocal());
    }
    emit();
  };
  WL.toggle = async function (id) {
    id = Number(id);
    const on = !WL.has(id);
    on ? WL.ids.add(id) : WL.ids.delete(id);
    emit();
    const me = await PB.me();
    try {
      if (me) await PB.api('/account/wishlist/' + id, { method: on ? 'PUT' : 'DELETE' });
      else writeLocal([...WL.ids]);
    } catch (e) {
      on ? WL.ids.delete(id) : WL.ids.add(id); // връщане назад при грешка
      emit();
      PB.toast('❌ ' + e.message);
    }
    return on;
  };
})();
