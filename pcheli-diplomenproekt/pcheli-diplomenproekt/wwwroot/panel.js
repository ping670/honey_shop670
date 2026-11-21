/* Панел на пчеларя: собствени продукти, наличности, поръчки и публичен профил. */
(function () {
  'use strict';
  const tr = (s) => window.I18N.t(s);
  const esc = PB.esc;
  const $ = (id) => document.getElementById(id);
  const app = $('app');
  let me = null;
  let tab = 'inventory';
  const TABS = [['inventory', 'Наличности'], ['products', 'Продукти'], ['orders', 'Поръчки'], ['profile', 'Публичен профил']];
  const TYPES = ['Мед', 'Пчелен прашец', 'Подаръчен комплект'];
  const CATEGORIES = [['manov', 'Манов'], ['acacia', 'Акация'], ['herbal', 'Билков / горски'], ['lavender', 'Лавандула'], ['linden', 'Липа'], ['pollen', 'Прашец'], ['other', 'Друго']];

  async function init() {
    window.I18N.mountTools($('site-tools'));
    me = await PB.me(true);
    if (!me) return renderLogin();
    if (!me.roles.includes('Beekeeper')) {
      app.innerHTML = `<div class="alert error">${esc(tr('Този акаунт няма достъп до панела на пчеларя. Кандидатствайте от началната страница („За Пчелари“), като сте влезли в акаунта си, или помолете администратор да свърже акаунта ви с вашия пчелин.'))}</div>
        <p><a class="pbtn ghost" href="/">${esc(tr('Към магазина'))}</a> <button class="pbtn danger" id="lo">${esc(tr('Изход'))}</button></p>`;
      $('lo').addEventListener('click', async () => { await PB.logout(); location.reload(); });
      return;
    }
    renderShell();
  }

  function renderLogin() {
    app.innerHTML = `<form class="card" id="lf" style="max-width:420px;margin:20px auto">
      <h2 style="margin-top:0">${esc(tr('Вход за пчелари'))}</h2>
      <div class="field"><label>${esc(tr('Имейл'))}</label><input id="lf-e" type="email" autocomplete="email" required></div>
      <div class="field"><label>${esc(tr('Парола'))}</label><input id="lf-p" type="password" autocomplete="current-password" required></div>
      <div id="lf-err" class="alert error hidden" role="alert"></div>
      <button class="pbtn primary" style="width:100%">${esc(tr('Вход'))}</button></form>`;
    $('lf').addEventListener('submit', async (e) => {
      e.preventDefault();
      try { await PB.login($('lf-e').value.trim(), $('lf-p').value); init(); }
      catch (ex) { $('lf-err').textContent = ex.message; $('lf-err').classList.remove('hidden'); }
    });
  }

  function renderShell() {
    app.innerHTML = `<div class="tabs" role="tablist">${TABS.map(([k, l]) => `<button role="tab" data-t="${k}" class="${k === tab ? 'active' : ''}">${esc(tr(l))}</button>`).join('')}
      <span style="flex:1"></span><button class="pbtn danger small" id="lo">${esc(tr('Изход'))}</button></div><section id="tb"></section>`;
    app.querySelectorAll('[data-t]').forEach((b) => b.addEventListener('click', () => { tab = b.dataset.t; renderShell(); }));
    $('lo').addEventListener('click', async () => { await PB.logout(); location.reload(); });
    ({ inventory: renderInventory, products: renderProducts, orders: renderOrders, profile: renderProfile })[tab]();
  }
  const tb = () => $('tb');
  const fail = (ex) => { tb().innerHTML = `<div class="alert error">${esc(ex.message)}</div>`; };

  // ---------------------------- Наличности ----------------------------
  async function renderInventory() {
    try {
      const inv = await PB.api('/panel/inventory');
      const low = inv.filter((i) => i.isLow).length;
      tb().innerHTML = `<div class="stat-tiles"><div class="stat-tile"><div class="v">${inv.length}</div><div class="l">${esc(tr('Опаковки'))}</div></div>
        <div class="stat-tile"><div class="v" style="color:${low ? 'var(--bad)' : 'var(--ok)'}">${low}</div><div class="l">${esc(tr('Малка наличност'))}</div></div></div>
        <div class="card table-wrap"><table class="data"><thead><tr><th>${esc(tr('Продукт'))}</th><th>${esc(tr('Опаковка'))}</th><th>${esc(tr('Наличност'))}</th><th></th></tr></thead><tbody>
        ${inv.map((i) => `<tr><td>${esc(i.productName)}</td><td>${esc(i.label)}</td>
          <td><input class="pinput" style="width:90px" type="number" min="0" value="${i.stockQuantity}" data-v="${i.variantId}">
          ${i.stockQuantity === 0 ? `<span class="badge bad">${esc(tr('Изчерпано'))}</span>` : i.isLow ? `<span class="badge">${esc(tr('Малко'))}</span>` : ''}</td>
          <td><button class="pbtn ghost small" data-save="${i.variantId}">${esc(tr('Запази'))}</button></td></tr>`).join('')}</tbody></table></div>`;
      tb().querySelectorAll('[data-save]').forEach((b) => b.addEventListener('click', async () => {
        const v = b.dataset.save, q = parseInt(tb().querySelector(`input[data-v="${v}"]`).value, 10);
        try { await PB.api('/panel/inventory/' + v, { method: 'PUT', body: JSON.stringify({ stockQuantity: q }) }); PB.toast('✅ ' + tr('Запазено.')); renderInventory(); }
        catch (ex) { PB.toast('❌ ' + ex.message); }
      }));
    } catch (ex) { fail(ex); }
  }

  // ---------------------------- Продукти ----------------------------
  async function renderProducts() {
    try {
      const items = await PB.api('/panel/products');
      tb().innerHTML = `<p><button class="pbtn primary" id="np">＋ ${esc(tr('Нов продукт'))}</button></p>
        <div class="card table-wrap"><table class="data"><thead><tr><th></th><th>${esc(tr('Продукт'))}</th><th>${esc(tr('Вид'))}</th><th>${esc(tr('Партида'))}</th><th>${esc(tr('Опаковки'))}</th><th></th></tr></thead><tbody>
        ${items.map((p) => `<tr><td><img src="${esc(PB.safeUrl(p.image, './images/hero.png'))}" alt="" style="width:48px;height:48px;object-fit:cover;border-radius:8px"></td>
          <td><b>${esc(p.name)}</b><br><span class="muted">${esc(p.origin)}</span></td><td>${esc(p.type)}</td><td>${esc(p.batchNumber || '—')}</td>
          <td>${p.variants.map((v) => `${esc(v.label)}: ${PB.money(v.price)} (${v.stock} ${esc(tr('бр.'))})`).join('<br>')}</td>
          <td style="white-space:nowrap"><button class="pbtn ghost small" data-e="${p.id}">✎</button> <button class="pbtn danger small" data-d="${p.id}">🗑</button></td></tr>`).join('')}
        ${items.length ? '' : `<tr><td colspan="6" class="muted">${esc(tr('Нямате продукти.'))}</td></tr>`}</tbody></table></div><div id="pform"></div>`;
      $('np').addEventListener('click', () => productForm(null));
      tb().querySelectorAll('[data-e]').forEach((b) => b.addEventListener('click', () => productForm(items.find((p) => String(p.id) === b.dataset.e))));
      tb().querySelectorAll('[data-d]').forEach((b) => b.addEventListener('click', async () => {
        if (!confirm(tr('Да изтрием ли този продукт? Действието е необратимо.'))) return;
        try { await PB.api('/panel/products/' + b.dataset.d, { method: 'DELETE' }); renderProducts(); } catch (ex) { PB.toast('❌ ' + ex.message); }
      }));
    } catch (ex) { fail(ex); }
  }

  function variantRow(v) {
    v = v || { label: '', weight: '', price: '', stock: 100, inStock: true };
    return `<div class="grid" style="grid-template-columns:2fr 1fr 1fr 1fr auto;gap:8px;align-items:end;margin-bottom:6px" data-vr>
      <div class="field" style="margin:0"><label>${esc(tr('Етикет'))}</label><input data-f="label" value="${esc(v.label)}" placeholder="Буркан 0.9 кг" required></div>
      <div class="field" style="margin:0"><label>${esc(tr('Тегло'))}</label><input data-f="weight" value="${esc(v.weight)}" placeholder="0.9kg" required></div>
      <div class="field" style="margin:0"><label>${esc(tr('Цена €'))}</label><input data-f="price" type="number" step="0.01" min="0.01" value="${esc(v.price)}" required></div>
      <div class="field" style="margin:0"><label>${esc(tr('Наличност'))}</label><input data-f="stock" type="number" min="0" value="${esc(v.stock)}"></div>
      <button type="button" class="pbtn danger small" data-rmv>✕</button></div>`;
  }

  function productForm(p) {
    const f = p || { name: '', type: 'Мед', category: 'herbal', origin: '', description: '', harvestDate: '', purity: '', tastingNotes: '', color: '', aroma: '', crystallization: '', image: './images/acacia.jpg', tag: '100% Натурален', quizTags: [], variants: [], contents: '', batchNumber: '', harvestYear: new Date().getFullYear(), labReportUrl: '', labSummary: '', certificates: '', nameEn: '', descriptionEn: '' };
    const input = (id, label, val, extra) => `<div class="field"><label for="${id}">${esc(tr(label))}</label><input id="${id}" value="${esc(val ?? '')}" ${extra || ''}></div>`;
    $('pform').innerHTML = `<form class="card" id="pf"><h3>${p ? esc(tr('Редакция на продукт')) : esc(tr('Нов продукт'))}</h3>
      <div class="grid cols-2">${input('f-name', 'Име *', f.name, 'required maxlength="150"')}${input('f-origin', 'Произход (регион) *', f.origin, 'required maxlength="100"')}
        <div class="field"><label>${esc(tr('Вид'))}</label><select id="f-type">${TYPES.map((t) => `<option ${t === f.type ? 'selected' : ''}>${esc(t)}</option>`).join('')}</select></div>
        <div class="field"><label>${esc(tr('Категория'))}</label><select id="f-cat">${CATEGORIES.map(([k, l]) => `<option value="${k}" ${k === f.category ? 'selected' : ''}>${esc(tr(l))}</option>`).join('')}</select></div></div>
      <div class="field"><label for="f-desc">${esc(tr('Описание'))}</label><textarea id="f-desc" maxlength="2000">${esc(f.description)}</textarea></div>
      <div class="field"><label>${esc(tr('Снимка'))}</label><div style="display:flex;gap:8px;flex-wrap:wrap"><input id="f-image" value="${esc(f.image)}" style="flex:1;min-width:200px"><label class="pbtn ghost small" style="margin:0">⬆ ${esc(tr('Качи'))}<input type="file" id="f-up" accept="image/jpeg,image/png,image/webp" hidden></label></div></div>
      <details class="card" open><summary><b>${esc(tr('Произход и проследимост'))}</b></summary>
        <div class="grid cols-2" style="margin-top:10px">${input('f-batch', 'Партида №', f.batchNumber, 'maxlength="60"')}${input('f-year', 'Година на реколтата', f.harvestYear ?? '', 'type="number" min="1990" max="2100"')}
          ${input('f-cert', 'Сертификати', f.certificates, 'maxlength="300"')}${input('f-lab', 'Лабораторен анализ (кратко резюме)', f.labSummary, 'maxlength="400"')}
          ${input('f-labu', 'Връзка към лабораторния протокол (https://…)', f.labReportUrl, 'maxlength="250"')}${input('f-contents', 'Състав (за комплекти)', f.contents, 'maxlength="500"')}</div>
        <p class="muted" style="font-size:.8rem;margin:0">${esc(tr('Попълвайте само верни данни – те се показват на клиентите като гаранция за произхода.'))}</p></details>
      <details class="card"><summary><b>${esc(tr('Допълнителни характеристики и английски текст'))}</b></summary>
        <div class="grid cols-2" style="margin-top:10px">${input('f-color', 'Цвят', f.color)}${input('f-aroma', 'Аромат', f.aroma)}${input('f-cryst', 'Кристализация', f.crystallization)}${input('f-harv', 'Реколта (текст)', f.harvestDate)}
          ${input('f-purity', 'Качество / чистота', f.purity)}${input('f-taste', 'Вкус', f.tastingNotes)}${input('f-tag', 'Етикет', f.tag)}${input('f-nameen', 'Name (English)', f.nameEn)}</div>
        <div class="field"><label for="f-descen">Description (English)</label><textarea id="f-descen" maxlength="2000">${esc(f.descriptionEn)}</textarea></div></details>
      <h4>${esc(tr('Опаковки'))}</h4><div id="vrs">${(f.variants.length ? f.variants : [null]).map(variantRow).join('')}</div>
      <button type="button" class="pbtn ghost small" id="addv">＋ ${esc(tr('Опаковка'))}</button>
      <div id="pf-err" class="alert error hidden" role="alert" style="margin-top:12px"></div>
      <p style="margin-top:14px"><button class="pbtn primary" type="submit">${esc(tr('Запази'))}</button> <button type="button" class="pbtn ghost" id="cancel">${esc(tr('Отказ'))}</button></p></form>`;
    $('addv').addEventListener('click', () => $('vrs').insertAdjacentHTML('beforeend', variantRow()));
    $('vrs').addEventListener('click', (e) => { if (e.target.matches('[data-rmv]') && $('vrs').children.length > 1) e.target.closest('[data-vr]').remove(); });
    $('cancel').addEventListener('click', () => ($('pform').innerHTML = ''));
    $('f-up').addEventListener('change', async (e) => {
      try { $('f-image').value = await upload(e.target.files[0]); } catch (ex) { PB.toast('❌ ' + ex.message); }
    });
    $('pf').addEventListener('submit', async (e) => {
      e.preventDefault();
      const dto = {
        name: $('f-name').value.trim(), type: $('f-type').value, category: $('f-cat').value, origin: $('f-origin').value.trim(),
        description: $('f-desc').value.trim(), harvestDate: $('f-harv').value.trim(), purity: $('f-purity').value.trim(), tastingNotes: $('f-taste').value.trim(),
        color: $('f-color').value.trim(), aroma: $('f-aroma').value.trim(), crystallization: $('f-cryst').value.trim(), image: $('f-image').value.trim() || './images/acacia.jpg',
        tag: $('f-tag').value.trim() || '100% Натурален', quizTags: p ? (p.quizTags || []).join(',') : '', inStock: true,
        contents: $('f-contents').value.trim(), batchNumber: $('f-batch').value.trim(), harvestYear: $('f-year').value ? +$('f-year').value : null,
        labReportUrl: $('f-labu').value.trim(), labSummary: $('f-lab').value.trim(), certificates: $('f-cert').value.trim(),
        nameEn: $('f-nameen').value.trim(), descriptionEn: $('f-descen').value.trim(),
        variants: [...document.querySelectorAll('[data-vr]')].map((r) => {
          const g = (k) => r.querySelector(`[data-f="${k}"]`).value;
          return { label: g('label').trim(), weight: g('weight').trim(), price: +g('price'), inStock: true, stockQuantity: g('stock') === '' ? null : +g('stock') };
        })
      };
      try { await PB.api(p ? '/panel/products/' + p.id : '/panel/products', { method: p ? 'PUT' : 'POST', body: JSON.stringify(dto) }); PB.toast('✅ ' + tr('Запазено.')); renderProducts(); }
      catch (ex) { $('pf-err').textContent = ex.message; $('pf-err').classList.remove('hidden'); }
    });
    $('pform').scrollIntoView({ behavior: 'smooth' });
  }

  async function upload(file) {
    if (!file) throw new Error(tr('Не е избран файл.'));
    if (file.size > 4 * 1024 * 1024) throw new Error(tr('Снимката е твърде голяма (максимум 4 MB).'));
    const fd = new FormData(); fd.append('file', file);
    const r = await PB.api('/uploads', { method: 'POST', body: fd });
    return r.url;
  }

  // ---------------------------- Поръчки ----------------------------
  async function renderOrders() {
    try {
      const orders = await PB.api('/panel/orders');
      tb().innerHTML = orders.length ? orders.map((o) => `<div class="card">
        <div style="display:flex;justify-content:space-between;flex-wrap:wrap;gap:8px"><b>${esc(o.orderNumber)} <span class="muted">· ${esc(PB.date(o.createdAt, true))}</span></b>
          <span><span class="badge ${o.status === 'Отказана' ? 'bad' : o.status === 'Доставена' ? 'ok' : ''}">${esc(tr(o.status))}</span> <span class="badge">${esc(tr(o.paymentStatus || o.paymentMethod))}</span></span></div>
        <p style="margin:8px 0">👤 ${esc(o.customerName)} · 📞 <a href="tel:${esc(o.customerPhone)}">${esc(o.customerPhone)}</a><br>📍 ${esc(o.deliveryCity)}, ${esc(o.deliveryAddress)} · ${esc(o.courier)} (${esc(tr(o.deliveryType === 'Адрес' ? 'до адрес' : 'до офис'))})
          ${o.isGift ? `<br>🎁 ${esc(tr('Подарък'))}${o.giftMessage ? ': ' + esc(o.giftMessage) : ''}` : ''}${o.notes ? `<br>📝 ${esc(o.notes)}` : ''}</p>
        ${PB.stepper(o.status, o.events)}
        <ul style="margin:0;padding-left:18px">${o.items.map((i) => `<li>${i.quantity}× ${esc(i.productName)} (${esc(i.weight)}) — ${PB.money(i.totalPrice)}</li>`).join('')}</ul>
        ${nextAction(o)}</div>`).join('')
        : `<div class="card"><p class="muted">${esc(tr('Все още няма поръчки за вашите продукти.'))}</p></div>`;
      tb().querySelectorAll('[data-next]').forEach((b) => b.addEventListener('click', async () => {
        b.disabled = true;
        try {
          await PB.api(`/panel/orders/${b.dataset.id}/status`, { method: 'PUT', body: JSON.stringify({ status: b.dataset.next }) });
          PB.toast('✓ ' + tr('Статусът е обновен.'));
          renderOrders();
        } catch (ex) { b.disabled = false; PB.toast('❌ ' + ex.message); }
      }));
    } catch (ex) { fail(ex); }
  }

  // Следващата фаза, която пчеларят може да отбележи (само когато поръчката е изцяло негова)
  function nextAction(o) {
    if (o.status === 'Отказана' || o.status === 'Доставена') return '';
    if (!o.canUpdateStatus) return `<p class="muted" style="margin:10px 0 0;font-size:.85rem">${esc(tr('Поръчката съдържа продукти от няколко пчелина – статусът се променя от администратор.'))}</p>`;
    const next = { 'Приета': ['Обработва се', 'Започни обработка'], 'Обработва се': ['Изпратена', 'Отбележи като изпратена'], 'Изпратена': ['Доставена', 'Отбележи като доставена'] }[o.status];
    return next ? `<p style="margin:12px 0 0"><button class="pbtn primary small" data-next="${next[0]}" data-id="${o.id}">${esc(tr(next[1]))}</button></p>` : '';
  }

  // ---------------------------- Профил ----------------------------
  async function renderProfile() {
    try {
      const d = await PB.api('/panel/me');
      const b = d.beekeeper;
      let photos = [...(d.apiaryPhotos || [])];
      let photo = b.photoUrl || '';
      tb().innerHTML = `<form class="card" id="bf"><h3>${esc(tr('Публичен профил'))} — <a href="beekeeper.html?id=${b.id}" target="_blank" rel="noopener">${esc(tr('Виж страницата'))} ↗</a></h3>
        <div class="grid cols-2"><div class="field"><label>${esc(tr('Телефон'))}</label><input id="b-phone" value="${esc(b.phone)}" maxlength="30"></div>
          <div class="field"><label>${esc(tr('Имейл'))}</label><input id="b-email" type="email" value="${esc(b.email || '')}" maxlength="100"></div></div>
        <div class="field"><label>${esc(tr('Кратко представяне'))}</label><textarea id="b-bio" maxlength="1000">${esc(b.bio)}</textarea></div>
        <div class="field"><label>${esc(tr('Вашата история'))}</label><textarea id="b-story" maxlength="3000" style="min-height:160px">${esc(d.story)}</textarea><span class="hint">${esc(tr('Разделяйте абзаците с празен ред.'))}</span></div>
        <div class="field"><label>${esc(tr('Ваша снимка'))}</label><div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap"><img id="b-photo" class="bk-photo" style="width:70px;height:70px" src="${esc(PB.safeUrl(photo, './images/logo.jpg'))}" alt="">
          <label class="pbtn ghost small" style="margin:0">⬆ ${esc(tr('Качи'))}<input type="file" id="b-up" accept="image/jpeg,image/png,image/webp" hidden></label></div></div>
        <div class="field"><label>${esc(tr('Снимки от пчелина'))} (${esc(tr('до'))} 10)</label><div class="gallery" id="b-gal"></div>
          <label class="pbtn ghost small" style="margin-top:8px;width:fit-content">⬆ ${esc(tr('Добави снимка'))}<input type="file" id="b-gup" accept="image/jpeg,image/png,image/webp" hidden></label></div>
        <div id="b-msg"></div><button class="pbtn primary" type="submit">${esc(tr('Запази профила'))}</button></form>`;
      const drawGal = () => {
        $('b-gal').innerHTML = photos.map((u, i) => `<div style="position:relative"><img src="${esc(PB.safeUrl(u, ''))}" alt=""><button type="button" class="pbtn danger small" style="position:absolute;top:6px;right:6px" data-rm="${i}">✕</button></div>`).join('');
        $('b-gal').querySelectorAll('[data-rm]').forEach((x) => x.addEventListener('click', () => { photos.splice(+x.dataset.rm, 1); drawGal(); }));
      };
      drawGal();
      $('b-up').addEventListener('change', async (e) => { try { photo = await upload(e.target.files[0]); $('b-photo').src = photo; } catch (ex) { PB.toast('❌ ' + ex.message); } });
      $('b-gup').addEventListener('change', async (e) => {
        if (photos.length >= 10) return PB.toast('⚠️ ' + tr('Достигнахте максимума от 10 снимки.'));
        try { photos.push(await upload(e.target.files[0])); drawGal(); } catch (ex) { PB.toast('❌ ' + ex.message); }
      });
      $('bf').addEventListener('submit', async (e) => {
        e.preventDefault();
        try {
          await PB.api('/panel/profile', { method: 'PUT', body: JSON.stringify({ phone: $('b-phone').value.trim(), email: $('b-email').value.trim() || null, bio: $('b-bio').value.trim(), story: $('b-story').value.trim(), photoUrl: photo, apiaryPhotos: photos }) });
          $('b-msg').innerHTML = `<div class="alert success">${esc(tr('Запазено.'))}</div>`;
        } catch (ex) { $('b-msg').innerHTML = `<div class="alert error">${esc(ex.message)}</div>`; }
      });
    } catch (ex) { fail(ex); }
  }

  init();
})();
