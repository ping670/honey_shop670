/* Форма „Контакти“ – към магазина или (с ?beekeeper=ID) към конкретен пчелар. */
(function () {
  'use strict';
  const tr = (s) => (window.I18N ? window.I18N.t(s) : s);
  const $ = (id) => document.getElementById(id);
  let beekeeperId = null;
  let cap = null;

  const msg = (ok, text) => { $('c-msg').innerHTML = `<div class="alert ${ok ? 'success' : 'error'}">${PB.esc(text)}</div>`; };

  async function init() {
    cap = await PB.captcha.mount($('c-cap'));

    const params = new URLSearchParams(location.search);
    const id = parseInt(params.get('beekeeper'), 10);
    if (id > 0) {
      try {
        const b = await PB.api('/beekeepers/' + id);
        beekeeperId = b.id;
        $('contact-title').textContent = tr('Съобщение до пчелин') + ' „' + b.apiaryName + '“';
        const box = $('to-beekeeper');
        box.textContent = tr('Съобщението ще бъде препратено на пчелина и на екипа на платформата.');
        box.classList.remove('hidden');
        if (!$('c-subject').value) $('c-subject').value = tr('Въпрос към') + ' ' + b.apiaryName;
      } catch (e) { /* невалиден пчелар – формата остава към магазина */ }
    }
    const subj = params.get('subject');
    if (subj && !$('c-subject').value) $('c-subject').value = subj.slice(0, 150);

    // Влезлите клиенти получават попълнени име и имейл
    const me = await PB.me();
    if (me) { $('c-name').value = me.fullName || ''; $('c-email').value = me.email || ''; if (me.phone) $('c-phone').value = me.phone; }

    $('contact-form').addEventListener('submit', submit);
  }

  async function submit(e) {
    e.preventDefault();
    $('c-msg').innerHTML = '';
    const dto = {
      name: $('c-name').value.trim(), email: $('c-email').value.trim(), phone: $('c-phone').value.trim() || null,
      subject: $('c-subject').value.trim(), message: $('c-message').value.trim(), beekeeperId
    };
    if (!dto.name || !dto.email || !dto.subject || dto.message.length < 10) { msg(false, tr('Попълнете всички полета (съобщението – поне 10 символа).')); return; }
    const btn = $('c-send'); btn.disabled = true;
    try {
      const r = await PB.api('/contact', { method: 'POST', body: JSON.stringify(dto), headers: cap.headers() });
      $('contact-form').reset();
      msg(true, r.message || tr('Съобщението е изпратено.'));
    } catch (ex) { msg(false, ex.message); }
    finally { btn.disabled = false; if (cap) cap.reload(); }
  }

  init();
})();
