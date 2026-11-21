/* ==========================================================================
   Известие за бисквитки + общи връзки към правните страници във всички футъри.
   Сайтът ползва само необходимо хранилище (вход, количка, любими, тема, език) – без проследяване и реклами,
   затова банерът е информативен: потребителят потвърждава, че е запознат. Решението се пази в браузъра.
   Зарежда се на всички страници.
   ========================================================================== */
(function () {
  'use strict';
  const KEY = 'pchelari_consent';
  const tr = (s) => (window.I18N ? window.I18N.t(s) : s);
  const read = () => { try { return localStorage.getItem(KEY); } catch (e) { return null; } };
  const write = () => { try { localStorage.setItem(KEY, new Date().toISOString().slice(0, 10)); } catch (e) { /* частен режим */ } };

  const LINKS = [
    ['/contact.html', 'Контакти'], ['/delivery.html', 'Доставка и плащане'], ['/returns.html', 'Връщане и рекламации'],
    ['/terms.html', 'Общи условия'], ['/privacy.html', 'Поверителност'], ['/cookies.html', 'Бисквитки']
  ];

  function linksHtml() {
    return LINKS.map(([href, label]) => `<a href="${href}">${tr(label)}</a>`).join(' · ');
  }

  // Връзки във футъра на самостоятелните страници (на началната страница са в самия HTML)
  function addFooterLinks() {
    document.querySelectorAll('.site-footer .wrap').forEach((wrap) => {
      if (wrap.querySelector('.legal-links')) return;
      const div = document.createElement('div');
      div.className = 'legal-links';
      div.innerHTML = linksHtml();
      wrap.appendChild(div);
    });
  }

  function addSkipLink() {
    const main = document.querySelector('main');
    if (!main || document.querySelector('.skip-link')) return;
    if (!main.id) main.id = 'main-content';
    const a = document.createElement('a');
    a.className = 'skip-link';
    a.href = '#' + main.id;
    a.textContent = tr('Към съдържанието');
    document.body.insertBefore(a, document.body.firstChild);
  }

  function showBanner() {
    if (read() || document.querySelector('.consent-banner')) return;
    const b = document.createElement('div');
    b.className = 'consent-banner';
    b.setAttribute('role', 'region');
    b.setAttribute('aria-label', tr('Известие за бисквитки'));
    b.innerHTML = `<p>🍪 ${tr('Сайтът използва само необходими бисквитки и локално хранилище (вход, количка, любими, тема и език). Не проследяваме посетителите и не показваме реклами.')}
      <a href="/cookies.html">${tr('Повече информация')}</a></p><button type="button">${tr('Разбрах')}</button>`;
    b.querySelector('button').addEventListener('click', () => { write(); b.remove(); });
    document.body.appendChild(b);
  }

  function init() {
    addFooterLinks();
    addSkipLink();
    showBanner();
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
  window.addEventListener('langchange', () => {
    document.querySelectorAll('.legal-links').forEach((d) => { d.innerHTML = linksHtml(); });
  });
})();
