/* ==========================================================================
   Добавки към началната страница (зарежда се след app.js):
   акаунт (вход/регистрация), любими, сортиране и филтри, проследяване на поръчка,
   куриер/подарък/запазени адреси при поръчка, абонамент и проверка „не съм робот“.
   ========================================================================== */
(function () {
  'use strict';
  const tr = (s) => window.I18N.t(s);
  const esc = PB.esc;
  const $ = (id) => document.getElementById(id);
  const openModal = (el) => el.classList.add('active');
  const closeModal = (el) => el.classList.remove('active');

  function makeModal(id, title, bodyHtml, maxWidth) {
    let el = $(id);
    if (el) el.remove();
    el = document.createElement('div');
    el.className = 'modal-overlay';
    el.id = id;
    el.innerHTML = `<div class="modal-card" style="max-width:${maxWidth || 480}px" role="dialog" aria-modal="true" aria-label="${esc(title)}">
      <div class="modal-header"><h3>${esc(title)}</h3><button class="close-btn" type="button" aria-label="${esc(tr('Затвори'))}">✕</button></div>
      <div class="modal-body">${bodyHtml}</div></div>`;
    document.body.appendChild(el);
    el.querySelector('.close-btn').addEventListener('click', () => closeModal(el));
    el.addEventListener('mousedown', (e) => { if (e.target === el) closeModal(el); });
    return el;
  }
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') document.querySelectorAll('.modal-overlay.active').forEach(closeModal);
  });

  // ============================ Проверка „не съм робот“ ============================
  window.PBCaptcha = {};
  async function mountCaptchas() {
    window.PBCaptcha.review = await PB.captcha.mount($('review-captcha'));
    window.PBCaptcha.beekeeper = await PB.captcha.mount($('bk-captcha'));
  }

  // ============================ Акаунт ============================
  let me = null;

  async function renderAccountNav() {
    me = await PB.me(true);
    const host = $('nav-account');
    if (!host) return;
    if (!me) {
      host.innerHTML = `<button type="button" class="icon-btn" id="login-open-btn" aria-label="${esc(tr('Вход'))}">👤 <span>${esc(tr('Вход'))}</span></button>`;
      $('login-open-btn').addEventListener('click', () => openAuthModal('login'));
      return;
    }
    const isAdmin = me.roles.includes('Admin');
    const isBk = me.roles.includes('Beekeeper');
    host.innerHTML = `<button type="button" class="icon-btn" id="account-btn" aria-haspopup="true" aria-expanded="false">👤 <span>${esc((me.fullName || me.email).split(' ')[0])}</span></button>
      <div class="account-menu" id="account-menu" role="menu">
        <div class="who">${esc(me.email)}</div>
        ${isAdmin ? `<a href="admin.html">🛠️ ${esc(tr('Админ панел'))}</a>` : ''}
        ${isBk ? `<a href="panel.html">🐝 ${esc(tr('Панел на пчеларя'))}</a>` : ''}
        <a href="account.html">📦 ${esc(tr('Моите поръчки'))}</a>
        <a href="account.html#wishlist">♡ ${esc(tr('Любими'))}</a>
        <a href="account.html#subscriptions">🔁 ${esc(tr('Абонаменти'))}</a>
        <a href="account.html#profile">⚙️ ${esc(tr('Профил и адреси'))}</a>
        <button type="button" id="logout-btn">🚪 ${esc(tr('Изход'))}</button>
      </div>`;
    const menu = $('account-menu');
    $('account-btn').addEventListener('click', (e) => {
      e.stopPropagation();
      const open = menu.classList.toggle('open');
      $('account-btn').setAttribute('aria-expanded', open);
    });
    document.addEventListener('click', () => menu.classList.remove('open'));
    $('logout-btn').addEventListener('click', async () => {
      await PB.logout();
      await afterAuthChange();
      PB.toast('👋 ' + tr('Излязохте от акаунта.'));
    });
  }

  async function afterAuthChange() {
    await renderAccountNav();
    await PB.wishlist.sync();
    window.afterProductsRender();
    if (activeWishlistOnlyChecked()) loadProducts();
  }

  let authCaptcha = null;
  async function openAuthModal(mode) {
    const el = makeModal('auth-modal', mode === 'login' ? tr('Вход') : tr('Регистрация'), `
      <div class="tabs" role="tablist">
        <button type="button" data-mode="login" class="${mode === 'login' ? 'active' : ''}">${esc(tr('Вход'))}</button>
        <button type="button" data-mode="register" class="${mode === 'register' ? 'active' : ''}">${esc(tr('Регистрация'))}</button>
      </div>
      <form id="auth-form" novalidate>
        <div class="field ${mode === 'login' ? 'hidden' : ''}" data-reg><label for="au-name">${esc(tr('Име и фамилия'))} *</label><input id="au-name" autocomplete="name" maxlength="100"></div>
        <div class="field"><label for="au-email">${esc(tr('Имейл'))} *</label><input id="au-email" type="email" autocomplete="email" required></div>
        <div class="field"><label for="au-pass">${esc(tr('Парола'))} *</label><input id="au-pass" type="password" autocomplete="${mode === 'login' ? 'current-password' : 'new-password'}" required>
          <span class="hint ${mode === 'login' ? 'hidden' : ''}" data-reg>${esc(tr('Минимум 8 символа: главна и малка буква, цифра и специален знак.'))}</span></div>
        <div id="au-captcha" class="${mode === 'login' ? 'hidden' : ''}" data-reg></div>
        <div id="au-error" class="alert error hidden" role="alert"></div>
        <button class="pbtn primary" style="width:100%" type="submit">${esc(mode === 'login' ? tr('Вход') : tr('Създай акаунт'))}</button>
        <p class="muted" style="font-size:.82rem;margin-top:12px">${esc(tr('Не е нужен акаунт, за да поръчате – той ви дава история на поръчките, любими, запазени адреси и абонаменти.'))}</p>
      </form>`, 440);
    el.dataset.mode = mode;
    el.querySelectorAll('.tabs button').forEach((b) => b.addEventListener('click', () => openAuthModal(b.dataset.mode)));
    authCaptcha = mode === 'register' ? await PB.captcha.mount($('au-captcha')) : null;
    $('auth-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      const err = $('au-error');
      err.classList.add('hidden');
      const email = $('au-email').value.trim(), pass = $('au-pass').value;
      if (!email || !pass) { err.textContent = tr('Попълнете имейл и парола.'); err.classList.remove('hidden'); return; }
      const btn = e.submitter || el.querySelector('button[type=submit]');
      btn.disabled = true;
      try {
        if (mode === 'login') await PB.login(email, pass);
        else {
          const name = $('au-name').value.trim();
          if (!name) throw new Error(tr('Въведете име.'));
          await PB.register({ fullName: name, email, password: pass }, authCaptcha ? authCaptcha.headers() : {});
        }
        closeModal(el);
        await afterAuthChange();
        PB.toast('✅ ' + tr('Добре дошли') + ', ' + ((me && me.fullName) || ''));
      } catch (ex) {
        err.textContent = ex.message;
        err.classList.remove('hidden');
        if (authCaptcha) authCaptcha.reload();
      } finally { btn.disabled = false; }
    });
    openModal(el);
    setTimeout(() => (mode === 'login' ? $('au-email') : $('au-name')).focus(), 50);
  }
  window.openAuthModal = openAuthModal;

  // ============================ Любими ============================
  function activeWishlistOnlyChecked() { return !!$('tb-wish') && $('tb-wish').checked; }

  window.afterProductsRender = function () {
    document.querySelectorAll('[data-heart]').forEach((b) => {
      const on = PB.wishlist.has(b.dataset.heart);
      b.classList.toggle('active', on);
      b.setAttribute('aria-pressed', on);
      if (b.classList.contains('heart-btn')) b.textContent = on ? '♥' : '♡';
      else b.textContent = (on ? '♥ ' : '♡ ') + tr('Любими');
    });
    // връзка от страница на пчелар: отваря избрания продукт в бърз преглед
    try {
      const pending = sessionStorage.getItem('pb_open_product');
      if (pending && PRODUCTS_DATA.length) {
        sessionStorage.removeItem('pb_open_product');
        window.openQuickView(pending);
      }
    } catch (e) { /* без sessionStorage */ }
  };
  document.addEventListener('click', async (e) => {
    const b = e.target.closest('[data-heart]');
    if (!b) return;
    e.stopPropagation();
    const on = await PB.wishlist.toggle(b.dataset.heart);
    PB.toast(on ? '♥ ' + tr('Добавено в любими') : tr('Премахнато от любими'));
    if (activeWishlistOnlyChecked()) loadProducts();
  });
  PB.wishlist.onChange(() => window.afterProductsRender());

  // ============================ Сортиране и филтри ============================
  function buildToolbar() {
    const host = $('catalog-toolbar');
    if (!host) return;
    host.innerHTML = `
      <select id="tb-sort" aria-label="${esc(tr('Сортиране'))}">
        <option value="">${esc(tr('Сортиране: по подразбиране'))}</option>
        <option value="price-asc">${esc(tr('Цена: ниска към висока'))}</option>
        <option value="price-desc">${esc(tr('Цена: висока към ниска'))}</option>
        <option value="name">${esc(tr('Име (А–Я)'))}</option>
        <option value="new">${esc(tr('Най-нови'))}</option>
        <option value="rating">${esc(tr('Най-високо оценени'))}</option>
      </select>
      <input id="tb-min" type="number" min="0" step="0.5" placeholder="${esc(tr('Цена от'))}" aria-label="${esc(tr('Цена от'))}">
      <input id="tb-max" type="number" min="0" step="0.5" placeholder="${esc(tr('до'))}" aria-label="${esc(tr('до'))}">
      <label><input type="checkbox" id="tb-stock"> ${esc(tr('Само налични'))}</label>
      <label><input type="checkbox" id="tb-wish"> ♥ ${esc(tr('Само любими'))}</label>`;
    let timer;
    const apply = () => {
      activeSort = $('tb-sort').value;
      activeMinPrice = $('tb-min').value;
      activeMaxPrice = $('tb-max').value;
      activeInStockOnly = $('tb-stock').checked;
      activeWishlistOnly = $('tb-wish').checked;
      clearTimeout(timer);
      timer = setTimeout(() => loadProducts(), 250);
    };
    host.querySelectorAll('select,input').forEach((i) => i.addEventListener('input', apply));
  }
  window.resetCatalogToolbar = function () {
    if (!$('tb-sort')) return;
    $('tb-sort').value = ''; $('tb-min').value = ''; $('tb-max').value = '';
    $('tb-stock').checked = false; $('tb-wish').checked = false;
  };

  // ============================ Проследяване на поръчка ============================
  const TRACK_STATES = ['Приета', 'Обработва се', 'Изпратена', 'Доставена'];
  function openTrackModal(prefillNumber) {
    const el = makeModal('track-modal', tr('Проследяване на поръчка'), `
      <form id="track-form">
        <div class="field"><label for="tr-num">${esc(tr('Номер на поръчка'))}</label><input id="tr-num" placeholder="BG-261118-1234" required value="${esc(prefillNumber || '')}"></div>
        <div class="field"><label for="tr-phone">${esc(tr('Телефон, с който сте поръчали'))}</label><input id="tr-phone" type="tel" placeholder="0888 123 456" required></div>
        <button class="pbtn primary" style="width:100%" type="submit">${esc(tr('Провери'))}</button>
      </form>
      <div id="track-result" aria-live="polite"></div>`, 520);
    $('track-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      const out = $('track-result');
      out.innerHTML = '';
      try {
        const r = await PB.api(`/orders/track?number=${encodeURIComponent($('tr-num').value.trim())}&phone=${encodeURIComponent($('tr-phone').value.trim())}`);
        const cancelled = r.status === 'Отказана';
        out.innerHTML = `<div class="card" style="margin-top:16px">
          <p style="margin:0 0 6px"><b>${esc(r.orderNumber)}</b> · <span class="badge ${cancelled ? 'bad' : r.status === 'Доставена' ? 'ok' : ''}">${esc(tr(r.status))}</span></p>
          <p class="muted" style="margin:0 0 6px;font-size:.88rem">${esc(PB.date(r.createdAt, true))} · ${esc(r.courier)} (${esc(tr(r.deliveryType === 'Адрес' ? 'до адрес' : 'до офис'))}) · ${esc(r.deliveryCity)}</p>
          <p style="margin:0 0 6px;font-size:.9rem">${esc(tr('Плащане'))}: ${esc(r.paymentMethod)} — <b>${esc(tr(r.paymentStatus))}</b></p>
          ${PB.stepper(r.status, r.events)}
          <ul class="timeline">${r.events.map((ev) => `<li class="${ev.status === 'Отказана' ? 'cancelled' : ''}"><b>${esc(tr(ev.status))}</b>${ev.note ? ' — ' + esc(ev.note) : ''}<time>${esc(PB.date(ev.at, true))}</time></li>`).join('')}</ul>
          <ul style="margin:6px 0 0;padding-left:18px;font-size:.9rem">${r.items.map((i) => `<li>${i.quantity}× ${esc(i.productName)} (${esc(i.weight)})</li>`).join('')}</ul>
          <p style="margin:10px 0 0;font-weight:700">${esc(tr('Общо'))}: ${PB.money(r.total)}</p>
          <p style="margin:12px 0 0;display:flex;gap:8px;flex-wrap:wrap">
            <a class="pbtn ghost small" target="_blank" rel="noopener" href="/api/orders/receipt?number=${encodeURIComponent(r.orderNumber)}&phone=${encodeURIComponent($('tr-phone').value.trim())}">🧾 ${esc(tr('Разписка'))}</a>
            ${r.status === 'Приета' && r.paymentStatus !== 'Платена' ? `<button type="button" class="pbtn danger small" id="tr-cancel">✕ ${esc(tr('Откажи поръчката'))}</button>` : ''}</p></div>`;
        const cancelBtn = $('tr-cancel');
        if (cancelBtn) cancelBtn.addEventListener('click', async () => {
          if (!confirm(tr('Сигурни ли сте, че искате да откажете поръчката?'))) return;
          try {
            await PB.api('/orders/cancel', { method: 'POST', body: JSON.stringify({ orderNumber: r.orderNumber, phone: $('tr-phone').value.trim() }) });
            PB.toast('✅ ' + tr('Поръчката е отказана.'));
            $('track-form').requestSubmit();
          } catch (ex) { PB.toast('❌ ' + ex.message); }
        });
      } catch (ex) {
        out.innerHTML = `<div class="alert error" role="alert">${esc(ex.status === 404 ? tr('Не намерихме поръчка с тези данни. Проверете номера и телефона.') : ex.message)}</div>`;
      }
    });
    openModal(el);
  }
  window.openTrackModal = openTrackModal;

  // ============================ Поръчка: куриер, подарък, адреси, плащане ============================
  async function setupCheckout() {
    const cfg = await PB.config();
    window.PB_SUB_DISCOUNT = cfg.subscriptionDiscountPercent;
    const row = $('ck-courier-row');
    if (row && cfg.couriers.length) {
      row.innerHTML = cfg.couriers.map((c, i) => `<label><input type="radio" name="courier" value="${esc(c.name)}" ${i === 0 ? 'checked' : ''}>
        <span>${esc(c.name)} <small class="muted">${esc(tr('офис'))} ${Number(c.officePrice).toFixed(2)} € · ${esc(tr('адрес'))} ${Number(c.addressPrice).toFixed(2)} €</small></span></label>`).join('');
      selectedCourier = cfg.couriers[0].name;
    }
    const sync = () => window.setDelivery(document.querySelector('input[name="courier"]:checked')?.value || 'Еконт',
      document.querySelector('input[name="delivery-type"]:checked')?.value || 'Офис');
    document.querySelectorAll('input[name="courier"], input[name="delivery-type"]').forEach((r) => r.addEventListener('change', sync));
    row && row.addEventListener('change', sync);

    $('ck-gift')?.addEventListener('change', (e) => $('ck-gift-msg').classList.toggle('hidden', !e.target.checked));

    const card = $('pay-card');
    if (card) {
      if (!cfg.cardPaymentsEnabled) {
        card.disabled = true;
        const span = card.parentElement.querySelector('span');
        if (span) span.innerHTML += ` <small class="muted">(${esc(tr('в момента не е налично'))})</small>`;
      }
    }
    document.querySelectorAll('input[name="payment"]').forEach((r) => r.addEventListener('change', () => {
      const isCard = document.querySelector('input[name="payment"]:checked')?.value === 'card';
      const s = $('ck-summary-payment');
      if (s) s.textContent = isCard ? tr('Банкова карта (онлайн)') : tr('Наложен платеж');
      document.querySelectorAll('.payment-radio-item').forEach((l) => {
        const on = l.querySelector('input').checked;
        l.classList.toggle('active', on);
        l.style.borderColor = on ? 'rgba(245,158,11,.6)' : 'rgba(255,255,255,.1)';
      });
    }));

    // запазени адреси за влезли потребители
    $('proceed-checkout-btn')?.addEventListener('click', prefillCheckout);
    $('ck-saved-address')?.addEventListener('change', (e) => {
      const a = savedAddresses.find((x) => String(x.id) === e.target.value);
      if (!a) return;
      $('ck-name').value = a.fullName; $('ck-phone').value = a.phone; $('ck-address').value = a.address;
      const sel = $('ck-city');
      if (![...sel.options].some((o) => o.value === a.city)) sel.add(new Option(a.city, a.city));
      sel.value = a.city;
    });
  }

  let savedAddresses = [];
  async function prefillCheckout() {
    const user = await PB.me();
    const wrap = $('ck-saved-wrap');
    if (!user) { wrap?.classList.add('hidden'); return; }
    if (!$('ck-name').value) $('ck-name').value = user.fullName || '';
    if (!$('ck-phone').value && user.phone) $('ck-phone').value = user.phone;
    if (!$('ck-email').value) $('ck-email').value = user.email || '';
    try {
      savedAddresses = await PB.api('/account/addresses');
      const sel = $('ck-saved-address');
      sel.innerHTML = `<option value="">${esc(tr('— изберете запазен адрес —'))}</option>` +
        savedAddresses.map((a) => `<option value="${a.id}">${esc(a.label)}: ${esc(a.city)}, ${esc(a.address)}</option>`).join('');
      wrap.classList.toggle('hidden', savedAddresses.length === 0);
      const def = savedAddresses.find((a) => a.isDefault);
      if (def && !$('ck-address').value) { sel.value = def.id; sel.dispatchEvent(new Event('change')); }
    } catch (e) { /* без запазени адреси */ }
  }

  window.renderQuoteLines = function (q) {
    const host = $('ck-quote-lines');
    if (!host) return;
    const fmt = (n) => Number(n).toFixed(2) + ' €';
    host.innerHTML = `<div><span>${esc(tr('Артикули'))}</span><span>${fmt(q.subtotal)}</span></div>` +
      (q.discount > 0 ? `<div><span>${esc(tr('Отстъпка'))}</span><span>−${fmt(q.discount)}</span></div>` : '') +
      `<div><span>${esc(tr('Доставка'))} (${esc(selectedCourier)}, ${esc(tr(selectedDeliveryType === 'Адрес' ? 'до адрес' : 'до офис'))})</span><span>${q.shipping === 0 ? esc(tr('БЕЗПЛАТНА')) : fmt(q.shipping)}</span></div>` +
      (q.exact ? '' : `<div class="muted" style="font-size:.78rem"><span>${esc(tr('Изчислява се...'))}</span><span></span></div>`);
  };

  // ============================ Абонамент ============================
  window.openSubscribeModal = async function (productId) {
    const user = await PB.me();
    if (!user) { PB.toast('🔐 ' + tr('Влезте в акаунта си, за да се абонирате.')); openAuthModal('login'); return; }
    const p = PRODUCTS_DATA.find((x) => x.id === String(productId));
    if (!p) return;
    const cfg = await PB.config();
    let addrs = [];
    try { addrs = await PB.api('/account/addresses'); } catch (e) { /* няма */ }
    const a = addrs.find((x) => x.isDefault) || addrs[0] || {};
    const first = new Date(Date.now() + 7 * 864e5).toISOString().slice(0, 10);
    const el = makeModal('sub-modal', tr('Абонамент за') + ' ' + p.name.replace(/&amp;/g, '&'), `
      <p class="muted" style="margin-top:0">${esc(tr('Получавайте продукта редовно с отстъпка'))} <b>${cfg.subscriptionDiscountPercent}%</b>. ${esc(tr('Плащане при доставка. Можете да паузирате или прекратите по всяко време от профила си.'))}</p>
      <form id="sub-form">
        <div class="field"><label>${esc(tr('Опаковка'))}</label><select id="sb-variant">${p.sizes.filter((s) => s.inStock).map((s) => `<option value="${s.variantId}">${s.label.replace(/&amp;/g, '&')} — ${s.price.toFixed(2)} €</option>`).join('')}</select></div>
        <div class="grid cols-2">
          <div class="field"><label>${esc(tr('Количество'))}</label><input id="sb-qty" type="number" min="1" max="20" value="1"></div>
          <div class="field"><label>${esc(tr('Доставка на всеки'))}</label><select id="sb-int"><option value="1">1 ${esc(tr('месец'))}</option><option value="2">2 ${esc(tr('месеца'))}</option><option value="3">3 ${esc(tr('месеца'))}</option></select></div>
        </div>
        <div class="field"><label>${esc(tr('Първа доставка'))}</label><input id="sb-date" type="date" value="${first}" min="${first}"></div>
        <div class="grid cols-2">
          <div class="field"><label>${esc(tr('Име'))}</label><input id="sb-name" value="${esc(a.fullName || user.fullName || '')}" required></div>
          <div class="field"><label>${esc(tr('Телефон'))}</label><input id="sb-phone" value="${esc(a.phone || user.phone || '')}" required></div>
        </div>
        <div class="grid cols-2">
          <div class="field"><label>${esc(tr('Град'))}</label><input id="sb-city" value="${esc(a.city || '')}" required></div>
          <div class="field"><label>${esc(tr('Адрес или офис на куриер'))}</label><input id="sb-addr" value="${esc(a.address || '')}" required></div>
        </div>
        <div class="grid cols-2">
          <div class="field"><label>${esc(tr('Куриер'))}</label><select id="sb-courier">${cfg.couriers.map((c) => `<option>${esc(c.name)}</option>`).join('')}</select></div>
          <div class="field"><label>${esc(tr('Доставка до'))}</label><select id="sb-dt"><option value="Офис">${esc(tr('Офис на куриера'))}</option><option value="Адрес">${esc(tr('Личен адрес'))}</option></select></div>
        </div>
        <div id="sb-error" class="alert error hidden" role="alert"></div>
        <button class="pbtn primary" style="width:100%" type="submit">🔁 ${esc(tr('Абонирай се'))}</button>
      </form>`, 560);
    $('sub-form').addEventListener('submit', async (e) => {
      e.preventDefault();
      const err = $('sb-error'); err.classList.add('hidden');
      try {
        await PB.api('/account/subscriptions', {
          method: 'POST',
          body: JSON.stringify({
            productVariantId: +$('sb-variant').value, quantity: +$('sb-qty').value, intervalMonths: +$('sb-int').value,
            customerName: $('sb-name').value.trim(), customerPhone: $('sb-phone').value.trim(), customerEmail: user.email,
            deliveryCity: $('sb-city').value.trim(), deliveryAddress: $('sb-addr').value.trim(),
            courier: $('sb-courier').value, deliveryType: $('sb-dt').value,
            firstDeliveryDate: new Date($('sb-date').value + 'T09:00:00Z').toISOString()
          })
        });
        closeModal(el);
        document.getElementById('quickview-modal')?.classList.remove('active');
        PB.toast('🔁 ' + tr('Абонаментът е създаден. Управлявайте го от профила си.'));
      } catch (ex) { err.textContent = ex.message; err.classList.remove('hidden'); }
    });
    openModal(el);
  };

  // ============================ Връщане от плащане и друго ============================
  function handleReturnParams() {
    const q = new URLSearchParams(location.search);
    if (q.get('paid')) {
      PB.toast('✅ ' + tr('Благодарим! Плащането за поръчка') + ' ' + q.get('paid') + ' ' + tr('се обработва.'));
      setTimeout(() => openTrackModal(q.get('paid')), 600);
    } else if (q.get('payment') === 'cancelled') {
      PB.toast('⚠️ ' + tr('Плащането беше прекъснато. Поръчката е записана – платете при доставка или се свържете с нас.'));
    }
    if (q.has('paid') || q.has('payment')) history.replaceState(null, '', location.pathname + location.hash);
    if (location.hash === '#track' || new URLSearchParams(location.search).get('track')) openTrackModal();
  }

  // ============================ Бюлетин ============================
  async function setupNewsletter() {
    const form = $('newsletter-form');
    if (!form) return;
    const cap = await PB.captcha.mount($('nl-cap'));
    const out = $('nl-msg');
    form.addEventListener('submit', async (e) => {
      e.preventDefault();
      out.style.color = '';
      try {
        const r = await PB.api('/newsletter/subscribe', { method: 'POST', body: JSON.stringify({ email: $('nl-email').value.trim() }), headers: cap.headers() });
        out.style.color = 'var(--ok, #34D399)';
        out.textContent = r.message || tr('Благодарим!');
        form.reset();
      } catch (ex) { out.style.color = 'var(--bad, #F87171)'; out.textContent = ex.message; }
      cap.reload();
    });
  }

  window.refreshCatalogStock = () => loadCatalog();

  // ============================ Старт ============================
  document.addEventListener('DOMContentLoaded', async () => {
    window.I18N.mountTools($('site-tools'));
    buildToolbar();
    mountCaptchas();
    setupCheckout();
    setupNewsletter();
    await renderAccountNav();
    await PB.wishlist.sync();
    window.afterProductsRender();
    handleReturnParams();
    window.addEventListener('langchange', () => { buildToolbar(); renderAccountNav(); mountCaptchas(); setupCheckoutLabels(); });
    if ('serviceWorker' in navigator && location.protocol !== 'file:') navigator.serviceWorker.register('/sw.js').catch(() => {});
  });
  function setupCheckoutLabels() { window.setDelivery(selectedCourier, selectedDeliveryType); }
})();
