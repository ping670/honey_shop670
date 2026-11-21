/* Личен профил: поръчки, любими, абонаменти, данни и адреси. */
(function () {
  'use strict';
  const tr = (s) => window.I18N.t(s);
  const esc = PB.esc;
  const $ = (id) => document.getElementById(id);
  const app = $('app');
  let me = null;
  let tab = 'orders';
  let cfg = null;

  const TABS = [
    ['orders', 'Поръчки'], ['wishlist', 'Любими'], ['subscriptions', 'Абонаменти'], ['profile', 'Профил и адреси']
  ];

  async function init() {
    window.I18N.mountTools($('site-tools'));
    cfg = await PB.config();
    me = await PB.me(true);
    const h = location.hash.replace('#', '');
    if (TABS.some((x) => x[0] === h)) tab = h;

    // Връзки от писма: ?reset=1&email=…&token=… (нова парола) и ?confirm=1&email=…&token=… (потвърждение на имейл)
    const q = new URLSearchParams(location.search);
    const linkEmail = q.get('email') || '', linkToken = q.get('token') || '';
    if (q.get('confirm') && linkEmail && linkToken) { history.replaceState(null, '', location.pathname); await confirmEmail(linkEmail, linkToken); return; }
    if (q.get('reset') && linkEmail && linkToken) { history.replaceState(null, '', location.pathname); renderReset(linkEmail, linkToken); return; }

    me ? renderShell() : renderLogin('login');
    window.addEventListener('langchange', () => { if (!special) (me ? renderShell() : renderLogin('login')); });
  }

  let special = false; // докато е показан екран „нова парола“ / „потвърждение“, смяната на език не го презарежда

  async function confirmEmail(email, token) {
    special = true;
    let ok = true, text = '';
    try { const r = await PB.api('/account/confirm-email', { method: 'POST', body: JSON.stringify({ email, token }) }); text = r.message; }
    catch (ex) { ok = false; text = ex.message; }
    app.innerHTML = `<div class="card" style="max-width:480px;margin:20px auto"><div class="alert ${ok ? 'success' : 'error'}">${esc(text)}</div>
      <a class="pbtn primary" href="account.html">${esc(tr('Към профила'))}</a></div>`;
  }

  function renderReset(email, token) {
    special = true;
    app.innerHTML = `<form class="card" id="rf" style="max-width:440px;margin:20px auto" novalidate>
      <h2 style="margin-top:0">${esc(tr('Нова парола'))}</h2><p class="muted">${esc(email)}</p>
      <div class="field"><label for="rf-pass">${esc(tr('Нова парола'))}</label><input id="rf-pass" type="password" autocomplete="new-password" required>
        <span class="hint">${esc(tr('Минимум 8 символа: главна и малка буква, цифра и специален знак.'))}</span></div>
      <div class="field"><label for="rf-pass2">${esc(tr('Повторете паролата'))}</label><input id="rf-pass2" type="password" autocomplete="new-password" required></div>
      <div id="rf-msg" role="alert"></div>
      <button class="pbtn primary" style="width:100%" type="submit">${esc(tr('Запази паролата'))}</button></form>`;
    $('rf').addEventListener('submit', async (e) => {
      e.preventDefault();
      const out = $('rf-msg');
      if ($('rf-pass').value !== $('rf-pass2').value) { out.innerHTML = `<div class="alert error">${esc(tr('Паролите не съвпадат.'))}</div>`; return; }
      try {
        const r = await PB.api('/account/reset-password', { method: 'POST', body: JSON.stringify({ email, token, newPassword: $('rf-pass').value }) });
        special = false;
        renderLogin('login', r.message);
      } catch (ex) { out.innerHTML = `<div class="alert error">${esc(ex.message)}</div>`; }
    });
  }

  async function renderForgot() {
    app.innerHTML = `<form class="card" id="ff" style="max-width:440px;margin:20px auto" novalidate>
      <h2 style="margin-top:0">${esc(tr('Забравена парола'))}</h2>
      <p class="muted">${esc(tr('Въведете имейла на акаунта си и ще Ви изпратим връзка за нова парола.'))}</p>
      <div class="field"><label for="ff-email">${esc(tr('Имейл'))}</label><input id="ff-email" type="email" autocomplete="email" required></div>
      <div id="ff-cap"></div><div id="ff-msg" role="alert"></div>
      <button class="pbtn primary" style="width:100%" type="submit">${esc(tr('Изпрати връзка'))}</button>
      <p style="text-align:center;margin-bottom:0"><a href="#" id="ff-back">← ${esc(tr('Обратно към входа'))}</a></p></form>`;
    const cap = await PB.captcha.mount($('ff-cap'));
    $('ff-back').addEventListener('click', (e) => { e.preventDefault(); renderLogin('login'); });
    $('ff').addEventListener('submit', async (e) => {
      e.preventDefault();
      const out = $('ff-msg');
      try {
        const r = await PB.api('/account/forgot-password', { method: 'POST', body: JSON.stringify({ email: $('ff-email').value.trim() }), headers: cap.headers() });
        out.innerHTML = `<div class="alert success">${esc(r.message)}</div>`;
      } catch (ex) { out.innerHTML = `<div class="alert error">${esc(ex.message)}</div>`; }
      cap.reload();
    });
  }

  // ------------------------------ Вход ------------------------------
  async function renderLogin(mode, notice) {
    app.innerHTML = `<div class="card" style="max-width:440px;margin:20px auto">
      ${notice ? `<div class="alert success">${esc(notice)}</div>` : ''}
      <div class="tabs"><button class="${mode === 'login' ? 'active' : ''}" data-m="login">${esc(tr('Вход'))}</button><button class="${mode === 'register' ? 'active' : ''}" data-m="register">${esc(tr('Регистрация'))}</button></div>
      <form id="lf" novalidate>
        ${mode === 'register' ? `<div class="field"><label for="lf-name">${esc(tr('Име и фамилия'))}</label><input id="lf-name" autocomplete="name" maxlength="100"></div>` : ''}
        <div class="field"><label for="lf-email">${esc(tr('Имейл'))}</label><input id="lf-email" type="email" autocomplete="email"></div>
        <div class="field"><label for="lf-pass">${esc(tr('Парола'))}</label><input id="lf-pass" type="password" autocomplete="${mode === 'login' ? 'current-password' : 'new-password'}">
          ${mode === 'register' ? `<span class="hint">${esc(tr('Минимум 8 символа: главна и малка буква, цифра и специален знак.'))}</span>` : ''}</div>
        <div id="lf-cap"></div>
        <div id="lf-err" class="alert error hidden" role="alert"></div>
        <button class="pbtn primary" style="width:100%" type="submit">${esc(mode === 'login' ? tr('Вход') : tr('Създай акаунт'))}</button>
        ${mode === 'login' ? `<p style="text-align:center;margin:12px 0 0"><a href="#" id="lf-forgot">${esc(tr('Забравена парола?'))}</a></p>`
          : `<p class="hint muted" style="font-size:.8rem;margin:10px 0 0">${esc(tr('С регистрацията приемате'))} <a href="/terms.html">${esc(tr('Общите условия'))}</a> ${esc(tr('и'))} <a href="/privacy.html">${esc(tr('Политиката за поверителност'))}</a>.</p>`}
      </form></div>`;
    app.querySelectorAll('.tabs button').forEach((b) => b.addEventListener('click', () => renderLogin(b.dataset.m)));
    const forgot = $('lf-forgot');
    if (forgot) forgot.addEventListener('click', (e) => { e.preventDefault(); renderForgot(); });
    const cap = mode === 'register' ? await PB.captcha.mount($('lf-cap')) : null;
    $('lf').addEventListener('submit', async (e) => {
      e.preventDefault();
      const err = $('lf-err'); err.classList.add('hidden');
      try {
        if (mode === 'login') await PB.login($('lf-email').value.trim(), $('lf-pass').value);
        else {
          if (!$('lf-name').value.trim()) throw new Error(tr('Въведете име.'));
          await PB.register({ fullName: $('lf-name').value.trim(), email: $('lf-email').value.trim(), password: $('lf-pass').value }, cap ? cap.headers() : {});
        }
        me = await PB.me(true);
        renderShell();
      } catch (ex) { err.textContent = ex.message; err.classList.remove('hidden'); if (cap) cap.reload(); }
    });
  }

  // ------------------------------ Рамка ------------------------------
  function renderShell() {
    const isAdmin = me.roles.includes('Admin'), isBk = me.roles.includes('Beekeeper');
    app.innerHTML = `<div style="display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;align-items:center">
        <p class="muted" style="margin:0">${esc(me.fullName)} · ${esc(me.email)}</p>
        <div style="display:flex;gap:8px;flex-wrap:wrap">
          ${isBk ? `<a class="pbtn ghost small" href="panel.html">🐝 ${esc(tr('Панел на пчеларя'))}</a>` : ''}
          ${isAdmin ? `<a class="pbtn ghost small" href="admin.html">🛠️ ${esc(tr('Админ панел'))}</a>` : ''}
          <button class="pbtn danger small" id="logout">${esc(tr('Изход'))}</button></div></div>
      ${me.emailConfirmed === false ? `<div class="alert" id="confirm-banner" style="display:flex;gap:10px;flex-wrap:wrap;align-items:center;justify-content:space-between">
        <span>✉️ ${esc(tr('Имейлът Ви още не е потвърден. Потвърдете го, за да получавате известия за поръчките.'))}</span>
        <button class="pbtn ghost small" id="resend-confirm">${esc(tr('Изпрати писмо'))}</button></div>` : ''}
      <div class="tabs" style="margin-top:14px" role="tablist">${TABS.map(([k, l]) => `<button role="tab" data-t="${k}" class="${k === tab ? 'active' : ''}">${esc(tr(l))}</button>`).join('')}</div>
      <section id="tab-body"></section>`;
    $('logout').addEventListener('click', async () => { await PB.logout(); me = null; renderLogin('login'); });
    const resend = $('resend-confirm');
    if (resend) resend.addEventListener('click', async () => {
      resend.disabled = true;
      try { const r = await PB.api('/account/send-confirmation', { method: 'POST' }); PB.toast('✉️ ' + r.message); }
      catch (ex) { PB.toast('❌ ' + ex.message); resend.disabled = false; }
    });
    app.querySelectorAll('.tabs button').forEach((b) => b.addEventListener('click', () => { tab = b.dataset.t; history.replaceState(null, '', '#' + tab); renderShell(); }));
    ({ orders: renderOrders, wishlist: renderWishlist, subscriptions: renderSubs, profile: renderProfile })[tab]();
  }

  const body = () => $('tab-body');
  const loading = () => { body().innerHTML = `<p class="muted">${esc(tr('Зареждане...'))}</p>`; };
  const fail = (ex) => { body().innerHTML = `<div class="alert error">${esc(ex.message)}</div>`; };

  // ------------------------------ Поръчки ------------------------------
  async function renderOrders() {
    loading();
    try {
      const orders = await PB.api('/account/orders');
      if (!orders.length) { body().innerHTML = `<div class="card"><p>${esc(tr('Все още нямате поръчки.'))}</p><a class="pbtn primary" href="/#products">${esc(tr('Към каталога'))}</a></div>`; return; }
      body().innerHTML = orders.map((o) => `<div class="card" data-o="${esc(o.orderNumber)}">
        <div style="display:flex;justify-content:space-between;flex-wrap:wrap;gap:8px">
          <div><b>${esc(o.orderNumber)}</b> <span class="muted">· ${esc(PB.date(o.createdAt))}</span></div>
          <div><span class="badge ${o.status === 'Отказана' ? 'bad' : o.status === 'Доставена' ? 'ok' : ''}">${esc(tr(o.status))}</span>
            <span class="badge ${o.paymentStatus === 'Платена' ? 'ok' : ''}">${esc(tr(o.paymentStatus || o.paymentMethod))}</span></div></div>
        ${PB.stepper(o.status, o.events)}
        <ul style="margin:10px 0;padding-left:18px">${o.items.map((i) => `<li>${i.quantity}× ${esc(i.productName)} <span class="muted">(${esc(i.weight)}) · ${esc(i.apiaryName)}</span></li>`).join('')}</ul>
        <div style="display:flex;justify-content:space-between;flex-wrap:wrap;gap:8px;align-items:center">
          <span>${esc(tr('Общо'))}: <b>${PB.money(o.totalAmount)}</b> <span class="muted">· ${esc(o.courier)} (${esc(tr(o.deliveryType === 'Адрес' ? 'до адрес' : 'до офис'))})</span></span>
          <span><button class="pbtn ghost small" data-track>${esc(tr('Проследи'))}</button>
          <a class="pbtn ghost small" target="_blank" rel="noopener" href="/api/account/orders/${encodeURIComponent(o.orderNumber)}/receipt">🧾 ${esc(tr('Разписка'))}</a>
          <button class="pbtn ghost small" data-again>${esc(tr('Поръчай отново'))}</button>
          ${o.status === 'Приета' && o.paymentStatus !== 'Платена' ? `<button class="pbtn danger small" data-cancel>✕ ${esc(tr('Откажи'))}</button>` : ''}</span></div>
        <div data-tl></div></div>`).join('');
      body().querySelectorAll('[data-track]').forEach((b) => b.addEventListener('click', async () => {
        const card = b.closest('.card'); const o = orders.find((x) => x.orderNumber === card.dataset.o);
        const host = card.querySelector('[data-tl]');
        if (host.innerHTML) { host.innerHTML = ''; return; }
        try {
          const r = await PB.api(`/orders/track?number=${encodeURIComponent(o.orderNumber)}&phone=${encodeURIComponent(o.customerPhone)}`);
          host.innerHTML = `<ul class="timeline">${r.events.map((ev) => `<li class="${ev.status === 'Отказана' ? 'cancelled' : ''}"><b>${esc(tr(ev.status))}</b>${ev.note ? ' — ' + esc(ev.note) : ''}<time>${esc(PB.date(ev.at, true))}</time></li>`).join('')}</ul>`;
        } catch (ex) { host.innerHTML = `<div class="alert error">${esc(ex.message)}</div>`; }
      }));
      body().querySelectorAll('[data-cancel]').forEach((b) => b.addEventListener('click', async () => {
        const num = b.closest('.card').dataset.o;
        if (!confirm(tr('Сигурни ли сте, че искате да откажете поръчка') + ' ' + num + '?')) return;
        try {
          await PB.api(`/account/orders/${encodeURIComponent(num)}/cancel`, { method: 'POST', body: JSON.stringify({}) });
          PB.toast('✅ ' + tr('Поръчката е отказана.'));
          renderOrders();
        } catch (ex) { PB.toast('❌ ' + ex.message); }
      }));
      body().querySelectorAll('[data-again]').forEach((b) => b.addEventListener('click', () => {
        const o = orders.find((x) => x.orderNumber === b.closest('.card').dataset.o);
        reorder(o);
      }));
    } catch (ex) { fail(ex); }
  }

  // Подготвя количката със същите артикули (цените и наличностите се проверяват от сървъра при поръчка)
  async function reorder(o) {
    try {
      const products = await PB.api('/products');
      const cart = [];
      for (const it of o.items) {
        const p = products.find((x) => x.id === it.productId);
        const v = p && p.variants.find((x) => x.id === it.productVariantId);
        if (!p || !v || !v.inStock) continue;
        // количката се визуализира като HTML в app.js, затова текстовете се екранират тук (както в mapApiProduct)
        cart.push({ cartItemId: `${p.id}_${v.id}`, id: String(p.id), name: esc(p.name), producer: p.beekeeper ? esc(`${p.beekeeper.apiaryName} (${p.beekeeper.fullName})`) : '',
          image: esc(PB.safeUrl(p.image, './images/hero.png')), sizeLabel: esc(v.label), variantId: v.id, weight: v.weight, price: Number(v.price), qty: Math.min(it.quantity, v.stock || it.quantity) });
      }
      if (!cart.length) { PB.toast('⚠️ ' + tr('Артикулите от тази поръчка вече не са налични.')); return; }
      localStorage.setItem('pchelari_bg_cart', JSON.stringify(cart));
      location.href = '/#products';
    } catch (ex) { PB.toast('❌ ' + ex.message); }
  }

  // ------------------------------ Любими ------------------------------
  async function renderWishlist() {
    loading();
    try {
      const items = await PB.api('/account/wishlist');
      if (!items.length) { body().innerHTML = `<div class="card"><p>${esc(tr('Нямате любими продукти. Натиснете ♡ върху продукт в каталога.'))}</p><a class="pbtn primary" href="/#products">${esc(tr('Към каталога'))}</a></div>`; return; }
      const en = window.I18N.lang === 'en';
      body().innerHTML = `<div class="grid cols-3">${items.map((p) => `<div class="card mini-product">
        <img src="${esc(PB.safeUrl(p.image, './images/hero.png'))}" alt="${esc(p.name)}" loading="lazy">
        <b>${esc(en && p.nameEn ? p.nameEn : p.name)}</b>
        <span class="muted" style="font-size:.85rem">${esc(p.beekeeper ? p.beekeeper.apiaryName : '')}</span>
        <span>${esc(tr('от'))} ${PB.money(Math.min(...p.variants.map((v) => v.price)))} ${p.inStock ? '' : `<span class="badge bad">${esc(tr('Изчерпано'))}</span>`}</span>
        <button class="pbtn ghost small" data-rm="${p.id}">♥ ${esc(tr('Премахни'))}</button></div>`).join('')}</div>`;
      body().querySelectorAll('[data-rm]').forEach((b) => b.addEventListener('click', async () => {
        await PB.api('/account/wishlist/' + b.dataset.rm, { method: 'DELETE' });
        renderWishlist();
      }));
    } catch (ex) { fail(ex); }
  }

  // ------------------------------ Абонаменти ------------------------------
  async function renderSubs() {
    loading();
    try {
      const subs = await PB.api('/account/subscriptions');
      if (!subs.length) { body().innerHTML = `<div class="card"><p>${esc(tr('Нямате абонаменти. Отворете продукт в каталога и изберете „Абонирай се“.'))}</p><a class="pbtn primary" href="/#products">${esc(tr('Към каталога'))}</a></div>`; return; }
      body().innerHTML = subs.map((s) => `<div class="card" data-s="${s.id}">
        <div style="display:flex;justify-content:space-between;flex-wrap:wrap;gap:8px"><b>${s.quantity}× ${esc(s.productName)} <span class="muted">(${esc(s.variantLabel)})</span></b>
          <span class="badge ${s.status === 'Активен' || s.status === 'Active' ? 'ok' : ''}">${esc(tr(s.status))}</span></div>
        <p class="muted" style="margin:6px 0">${esc(tr('На всеки'))} ${s.intervalMonths} ${esc(tr('мес.'))} · ${esc(tr('следваща доставка'))}: <b>${esc(PB.date(s.nextDeliveryDate))}</b> · −${s.discountPercent}% · ${esc(s.deliveryCity)}, ${esc(s.deliveryAddress)}</p>
        <p class="muted" style="margin:0 0 8px;font-size:.85rem">${esc(tr('Създадени поръчки'))}: ${s.ordersCreated}</p>
        <div style="display:flex;gap:8px;flex-wrap:wrap">
          ${s.status === 'Paused' || s.status === 'На пауза' ? `<button class="pbtn ghost small" data-a="resume">▶ ${esc(tr('Поднови'))}</button>` : `<button class="pbtn ghost small" data-a="pause">⏸ ${esc(tr('Пауза'))}</button>`}
          <button class="pbtn danger small" data-a="cancel">✕ ${esc(tr('Прекрати'))}</button></div></div>`).join('');
      body().querySelectorAll('[data-a]').forEach((b) => b.addEventListener('click', async () => {
        if (b.dataset.a === 'cancel' && !confirm(tr('Сигурни ли сте, че искате да прекратите абонамента?'))) return;
        try { await PB.api(`/account/subscriptions/${b.closest('.card').dataset.s}/status`, { method: 'POST', body: JSON.stringify({ action: b.dataset.a }) }); renderSubs(); }
        catch (ex) { PB.toast('❌ ' + ex.message); }
      }));
    } catch (ex) { fail(ex); }
  }

  // ------------------------------ Профил и адреси ------------------------------
  async function renderProfile() {
    loading();
    let addrs = [];
    try { addrs = await PB.api('/account/addresses'); } catch (ex) { return fail(ex); }
    body().innerHTML = `<div class="grid cols-2">
      <form class="card" id="pf"><h3>${esc(tr('Лични данни'))}</h3>
        <div class="field"><label>${esc(tr('Име и фамилия'))}</label><input id="pf-name" value="${esc(me.fullName)}" required maxlength="100"></div>
        <div class="field"><label>${esc(tr('Телефон'))}</label><input id="pf-phone" value="${esc(me.phone || '')}" maxlength="30"></div>
        <div class="field"><label>${esc(tr('Имейл'))}</label><input value="${esc(me.email)}" disabled></div>
        <div id="pf-msg"></div><button class="pbtn primary" type="submit">${esc(tr('Запази'))}</button></form>
      <form class="card" id="pw"><h3>${esc(tr('Смяна на парола'))}</h3>
        <div class="field"><label>${esc(tr('Текуща парола'))}</label><input id="pw-old" type="password" autocomplete="current-password" required></div>
        <div class="field"><label>${esc(tr('Нова парола'))}</label><input id="pw-new" type="password" autocomplete="new-password" required><span class="hint">${esc(tr('Минимум 8 символа: главна и малка буква, цифра и специален знак.'))}</span></div>
        <div id="pw-msg"></div><button class="pbtn primary" type="submit">${esc(tr('Смени паролата'))}</button></form></div>
      <div class="card"><h3>${esc(tr('Запазени адреси'))}</h3>
        ${addrs.length ? `<div class="table-wrap"><table class="data"><thead><tr><th>${esc(tr('Име'))}</th><th>${esc(tr('Получател'))}</th><th>${esc(tr('Адрес'))}</th><th></th></tr></thead><tbody>
          ${addrs.map((a) => `<tr><td>${esc(a.label)} ${a.isDefault ? `<span class="badge ok">${esc(tr('основен'))}</span>` : ''}</td><td>${esc(a.fullName)}<br><span class="muted">${esc(a.phone)}</span></td><td>${esc(a.city)}, ${esc(a.address)}</td>
          <td style="white-space:nowrap"><button class="pbtn ghost small" data-edit="${a.id}">✎</button> <button class="pbtn danger small" data-del="${a.id}">🗑</button></td></tr>`).join('')}</tbody></table></div>` : `<p class="muted">${esc(tr('Нямате запазени адреси.'))}</p>`}
        <h4 id="ad-title" style="margin-bottom:8px">${esc(tr('Нов адрес'))}</h4>
        <form id="ad" class="grid cols-2">
          <div class="field"><label>${esc(tr('Име на адреса'))}</label><input id="ad-label" value="Дом" required maxlength="60"></div>
          <div class="field"><label>${esc(tr('Получател'))}</label><input id="ad-name" value="${esc(me.fullName)}" required maxlength="100"></div>
          <div class="field"><label>${esc(tr('Телефон'))}</label><input id="ad-phone" value="${esc(me.phone || '')}" required maxlength="30"></div>
          <div class="field"><label>${esc(tr('Град'))}</label><input id="ad-city" required maxlength="100"></div>
          <div class="field" style="grid-column:1/-1"><label>${esc(tr('Адрес или офис на куриер'))}</label><input id="ad-addr" required maxlength="250"></div>
          <label style="grid-column:1/-1"><input type="checkbox" id="ad-def"> ${esc(tr('Основен адрес'))}</label>
          <div id="ad-msg" style="grid-column:1/-1"></div>
          <div style="grid-column:1/-1"><button class="pbtn primary" type="submit">${esc(tr('Запази адреса'))}</button></div></form></div>
      <div class="card"><h3>${esc(tr('Поверителност и лични данни'))}</h3>
        <p class="muted">${esc(tr('Можете да изтеглите всички данни, които пазим за Вас, или да изтриете акаунта си. Историята на поръчките се пази анонимно за счетоводството.'))}
          <a href="/privacy.html">${esc(tr('Политика за поверителност'))}</a></p>
        <p><a class="pbtn ghost small" href="/api/account/export" download="moite-danni.json">⬇ ${esc(tr('Изтегли моите данни'))}</a></p>
        <form id="del" class="grid cols-2" novalidate>
          <div class="field" style="grid-column:1/-1"><label for="del-pass">${esc(tr('За изтриване на акаунта въведете паролата си'))}</label>
            <input id="del-pass" type="password" autocomplete="current-password" required></div>
          <div id="del-msg" style="grid-column:1/-1"></div>
          <div style="grid-column:1/-1"><button class="pbtn danger" type="submit">🗑 ${esc(tr('Изтрий акаунта'))}</button></div></form></div>`;

    const msg = (id, ok, text) => { $(id).innerHTML = `<div class="alert ${ok ? 'success' : 'error'}">${esc(text)}</div>`; };
    $('pf').addEventListener('submit', async (e) => {
      e.preventDefault();
      try { me = await PB.api('/account/profile', { method: 'PUT', body: JSON.stringify({ fullName: $('pf-name').value.trim(), phone: $('pf-phone').value.trim() || null }) }); msg('pf-msg', true, tr('Запазено.')); }
      catch (ex) { msg('pf-msg', false, ex.message); }
    });
    $('pw').addEventListener('submit', async (e) => {
      e.preventDefault();
      try { await PB.api('/account/password', { method: 'POST', body: JSON.stringify({ currentPassword: $('pw-old').value, newPassword: $('pw-new').value }) }); $('pw').reset(); msg('pw-msg', true, tr('Паролата е сменена.')); }
      catch (ex) { msg('pw-msg', false, ex.message); }
    });
    $('del').addEventListener('submit', async (e) => {
      e.preventDefault();
      if (!$('del-pass').value) { msg('del-msg', false, tr('Въведете паролата си.')); return; }
      if (!confirm(tr('Акаунтът и личните Ви данни ще бъдат изтрити окончателно. Продължавате ли?'))) return;
      try {
        await PB.api('/account', { method: 'DELETE', body: JSON.stringify({ password: $('del-pass').value }) });
        await PB.logout(); me = null;
        renderLogin('login', tr('Акаунтът е изтрит. Благодарим, че бяхте с нас!'));
      } catch (ex) { msg('del-msg', false, ex.message); }
    });
    let editId = null;
    body().querySelectorAll('[data-edit]').forEach((b) => b.addEventListener('click', () => {
      const a = addrs.find((x) => String(x.id) === b.dataset.edit); editId = a.id;
      $('ad-title').textContent = tr('Редакция на адрес');
      $('ad-label').value = a.label; $('ad-name').value = a.fullName; $('ad-phone').value = a.phone; $('ad-city').value = a.city; $('ad-addr').value = a.address; $('ad-def').checked = a.isDefault;
      $('ad').scrollIntoView({ behavior: 'smooth' });
    }));
    body().querySelectorAll('[data-del]').forEach((b) => b.addEventListener('click', async () => {
      if (!confirm(tr('Да изтрием ли този адрес?'))) return;
      try { await PB.api('/account/addresses/' + b.dataset.del, { method: 'DELETE' }); renderProfile(); } catch (ex) { PB.toast('❌ ' + ex.message); }
    }));
    $('ad').addEventListener('submit', async (e) => {
      e.preventDefault();
      const dto = { label: $('ad-label').value.trim(), fullName: $('ad-name').value.trim(), phone: $('ad-phone').value.trim(), city: $('ad-city').value.trim(), address: $('ad-addr').value.trim(), isDefault: $('ad-def').checked };
      try { await PB.api('/account/addresses' + (editId ? '/' + editId : ''), { method: editId ? 'PUT' : 'POST', body: JSON.stringify(dto) }); renderProfile(); }
      catch (ex) { msg('ad-msg', false, ex.message); }
    });
  }

  init();
})();
