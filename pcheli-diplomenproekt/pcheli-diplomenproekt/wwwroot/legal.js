/* Правни страници и контакти: данни на търговеца от конфигурацията (секция „Shop“) и таблица с цените на доставка. */
(function () {
  'use strict';
  const tr = (s) => (window.I18N ? window.I18N.t(s) : s);
  const esc = (s) => PB.esc(s);

  async function init() {
    if (window.I18N) window.I18N.mountTools(document.getElementById('site-tools'));
    const cfg = await PB.config();
    const shop = cfg.shop || { companyName: 'Пчелари България', eik: '', address: '', email: '', phone: '', vatNote: '' };
    const map = { name: shop.companyName, eik: shop.eik, address: shop.address, email: shop.email, phone: shop.phone, vat: shop.vatNote };

    document.querySelectorAll('[data-shop]').forEach((el) => {
      const v = map[el.dataset.shop] || '';
      if (el.dataset.shop === 'email' && v && el.tagName === 'A') el.href = 'mailto:' + v;
      if (el.dataset.shop === 'phone' && v && el.tagName === 'A') el.href = 'tel:' + v.replace(/\s+/g, '');
      el.textContent = v;
      // редът (с етикет) се скрива, ако стойността не е зададена
      const row = el.closest('[data-shop-row]');
      if (row) row.classList.toggle('hidden', !v);
    });

    const host = document.getElementById('delivery-table');
    if (host && cfg.couriers && cfg.couriers.length) {
      host.innerHTML = `<table><thead><tr><th>${esc(tr('Куриер'))}</th><th>${esc(tr('До офис'))}</th><th>${esc(tr('До адрес'))}</th></tr></thead><tbody>` +
        cfg.couriers.map((c) => `<tr><td>${esc(c.name)}</td><td>${PB.money(c.officePrice)}</td><td>${PB.money(c.addressPrice)}</td></tr>`).join('') +
        `</tbody></table><p>${esc(tr('Безплатна доставка при поръчка над'))} <b>${PB.money(cfg.freeShippingFrom)}</b>.</p>`;
    }
    document.querySelectorAll('[data-free-shipping]').forEach((el) => { el.textContent = PB.money(cfg.freeShippingFrom); });
  }
  init();
})();
