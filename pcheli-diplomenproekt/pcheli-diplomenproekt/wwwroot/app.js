/* ==========================================================================
   ПЧЕЛАРИ БЪЛГАРИЯ - Национална платформа за чист мед и пчелен прашец
   Свързва независимите производители с клиентите без посредници
   ========================================================================== */

// --- Свързване с бекенда (ASP.NET Core + SQL Server) ---
// Фронтендът се сервира от същия сървър, затова API-то е на относителен адрес.
const API_BASE_URL = '/api';

// Всички данни идват от базата данни през API-то – няма твърдо кодирани копия.
let BEEKEEPERS_DATA = [];   // регистрирани пчелари
let PRODUCTS_DATA = [];     // пълният каталог (за количка, бърз преглед и въпросника)
let VISIBLE_PRODUCTS = [];  // резултатът от филтрите (заявка към API)

// --- Помощни функции за API ---
async function apiFetch(path, options = {}) {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options
  });
  let data = null;
  try { data = await res.json(); } catch (e) { /* няма съдържание */ }
  if (!res.ok) {
    const err = new Error(extractApiError(data) || `Грешка ${res.status}`);
    err.status = res.status;
    throw err;
  }
  return data;
}

function extractApiError(data) {
  if (!data) return '';
  if (data.errors) return Object.values(data.errors).flat().join(' ');
  return data.title || data.message || '';
}

function mapApiBeekeeper(b) {
  return {
    id: b.id,
    fullName: escapeHtml(b.fullName),
    apiaryName: escapeHtml(b.apiaryName),
    registrationNumber: escapeHtml(b.registrationNumber),
    region: escapeHtml(b.region),
    cityOrVillage: escapeHtml(b.cityOrVillage),
    phone: escapeHtml(b.phone),
    bio: escapeHtml(b.bio),
    experienceYears: b.experienceYears,
    hiveCount: b.hiveCount,
    rating: b.rating,
    isVerified: b.isVerified,
    hasHoney: b.hasHoney,
    hasPollen: b.hasPollen
  };
}

function mapApiProduct(p) {
  const bk = p.beekeeper;
  return {
    id: String(p.id),
    beekeeperId: bk ? bk.id : null,
    producer: bk ? `${escapeHtml(bk.apiaryName)} (${escapeHtml(bk.fullName)})` : 'Български Пчелин',
    babh: bk ? escapeHtml(bk.registrationNumber) : 'БАБХ Сертифициран',
    phone: bk ? escapeHtml(bk.phone) : '',
    name: escapeHtml(p.name),
    type: p.type,
    category: p.category,
    origin: escapeHtml(p.origin),
    tag: escapeHtml(p.tag),
    image: escapeHtml(p.image),
    description: escapeHtml(p.description),
    specs: { color: escapeHtml(p.color), aroma: escapeHtml(p.aroma), crystallization: escapeHtml(p.crystallization) },
    harvestDate: escapeHtml(p.harvestDate),
    pollenPurity: escapeHtml(p.purity),
    tastingNotes: escapeHtml(p.tastingNotes),
    sizes: p.variants.map(v => ({ variantId: v.id, label: escapeHtml(v.label), weight: v.weight, price: Number(v.price), inStock: v.inStock })),
    rating: p.rating,
    inStock: p.inStock,
    quizMatch: p.quizTags || []
  };
}

// --- Въпроси за помощ при избор (Quiz) ---
const QUIZ_STEPS = [
  {
    step: 1,
    title: "За какво търсите пчелен продукт най-вече?",
    options: [
      { label: "За подсилване на имунитета и тонус", val: "immunity", icon: "🛡️" },
      { label: "За чай, кафе и подслаждане на закуска", val: "tea", icon: "☕" },
      { label: "При настинка, гърло и за зимния сезон", val: "cold", icon: "🍋" },
      { label: "За деца и за цялото семейство", val: "kids", icon: "🥞" },
      { label: "Търся силен антиоксидантен и минерален мед", val: "antioxidant", icon: "✨" }
    ]
  },
  {
    step: 2,
    title: "Какъв вкус предпочитате?",
    options: [
      { label: "Мек, лек и нежен вкус (напр. Акация)", val: "light", icon: "🌸" },
      { label: "Ароматен билков букет от планината", val: "herbal", icon: "🌿" },
      { label: "Тъмен, плътен и минерален мед (Манов)", val: "dark", icon: "🌳" }
    ]
  }
];

// --- Състояние на количката (в браузъра се пазят само избраните опаковки) ---
let cartState = [];
try {
  cartState = (JSON.parse(localStorage.getItem('pchelari_bg_cart')) || []).filter(i => i && i.variantId);
} catch (e) {
  cartState = [];
}

let currentQuizStep = 0;
let quizAnswers = {};
let activeFilterCategory = 'all';
let activeFilterType = 'all';
let activeFilterRegion = 'all';
let activeSearchQuery = '';
let appliedDiscount = 0;
let appliedPromoCode = null;
let selectedPaymentMethodName = '💵 Наложен Платеж (при доставка)';

// --- Инициализация при зареждане ---
document.addEventListener('DOMContentLoaded', () => {
  renderQuizStep();
  initStarRatingSelector();
  updateCartUI();
  setupEventListeners();
  setupHeroMap();
  if (window.lucide) lucide.createIcons();

  initData();
});

function showLoadError(containerId, message, retryFn) {
  const el = document.getElementById(containerId);
  if (!el) return;
  el.innerHTML = `
    <div style="grid-column: 1 / -1; text-align: center; padding: 40px 20px; color: var(--text-soft);">
      <span style="font-size: 2rem; display:block; margin-bottom: 8px;">⚠️</span>
      <p style="margin-bottom: 12px;">${escapeHtml(message)}</p>
      <button class="btn btn-outline" onclick="${retryFn}()">Опитай отново</button>
    </div>`;
}

function showLoading(containerId, text) {
  const el = document.getElementById(containerId);
  if (el) el.innerHTML = `<div style="grid-column: 1 / -1; text-align: center; padding: 40px; color: var(--text-muted);">🐝 ${text}</div>`;
}

// Зареждане на всички данни от сървъра
async function initData() {
  await Promise.all([loadCatalog(), loadBeekeepers(), renderUserReviews()]);
}

// Пълен каталог – за количка, бърз преглед и въпросника
async function loadCatalog() {
  showLoading('products-grid', 'Зареждане на продуктите...');
  try {
    const data = await apiFetch('/products');
    PRODUCTS_DATA = data.map(mapApiProduct);
    updateHeroStats();
    reconcileCart();
    await loadProducts();
  } catch (err) {
    showLoadError('products-grid', 'Продуктите не можаха да бъдат заредени от сървъра. ' + err.message, 'loadCatalog');
  }
}

// Филтриран каталог – филтрирането се прави от API-то чрез query параметри
let productsRequestId = 0;
async function loadProducts() {
  const requestId = ++productsRequestId;
  const params = new URLSearchParams();
  if (activeFilterType !== 'all') params.set('type', activeFilterType);
  if (activeFilterCategory !== 'all') params.set('category', activeFilterCategory);
  if (activeFilterRegion !== 'all') params.set('region', activeFilterRegion);
  if (activeSearchQuery) params.set('search', activeSearchQuery);

  try {
    const data = await apiFetch('/products' + (params.toString() ? `?${params}` : ''));
    if (requestId !== productsRequestId) return; // получен е по-нов отговор
    VISIBLE_PRODUCTS = data.map(mapApiProduct);
    renderProducts();
  } catch (err) {
    if (requestId !== productsRequestId) return;
    showLoadError('products-grid', 'Продуктите не можаха да бъдат заредени. ' + err.message, 'loadProducts');
  }
}

// --- Начален екран: живи числа и карта на регионите ---
function animateCount(el, target) {
  if (!el) return;
  const reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduce || target <= 0) { el.textContent = target; return; }
  const duration = 900;
  const start = performance.now();
  const step = (now) => {
    const t = Math.min(1, (now - start) / duration);
    el.textContent = Math.round(target * (1 - Math.pow(1 - t, 3)));
    if (t < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}

function beekeeperWord(n) {
  return n === 1 ? '1 пчелар' : `${n} пчелари`;
}

function updateHeroStats() {
  if (PRODUCTS_DATA.length) animateCount(document.getElementById('hero-stat-products'), PRODUCTS_DATA.length);
  if (!BEEKEEPERS_DATA.length) return;

  const perRegion = {};
  BEEKEEPERS_DATA.forEach(b => { perRegion[b.region] = (perRegion[b.region] || 0) + 1; });

  animateCount(document.getElementById('hero-stat-beekeepers'), BEEKEEPERS_DATA.length);
  animateCount(document.getElementById('hero-stat-regions'), Object.keys(perRegion).length);
  animateCount(document.getElementById('hero-stat-hives'),
    BEEKEEPERS_DATA.reduce((sum, b) => sum + (Number(b.hiveCount) || 0), 0));

  document.querySelectorAll('.map-node').forEach(node => {
    const n = perRegion[node.dataset.region] || 0;
    node.classList.toggle('is-empty', n === 0);
    const label = node.querySelector('.map-count');
    if (label) label.textContent = n ? beekeeperWord(n) : 'очаква първия пчелар';
  });
}

function setupHeroMap() {
  document.querySelectorAll('.map-node').forEach(node => {
    const open = () => {
      if (node.classList.contains('is-empty')) {
        showToast(`🐝 В ${node.dataset.region} още няма пчелар. Ако сте пчелар оттам, присъединете се!`);
        window.openBeekeeperModal();
      } else {
        window.filterByRegion(node.dataset.region);
      }
    };
    node.addEventListener('click', open);
    node.addEventListener('keydown', e => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); open(); } });
  });
}

async function loadBeekeepers() {
  showLoading('beekeepers-grid', 'Зареждане на пчеларите...');
  try {
    const data = await apiFetch('/beekeepers');
    BEEKEEPERS_DATA = data.map(mapApiBeekeeper);
    renderBeekeepers();
    updateHeroStats();
  } catch (err) {
    showLoadError('beekeepers-grid', 'Пчеларите не можаха да бъдат заредени от сървъра. ' + err.message, 'loadBeekeepers');
  }
}

// Синхронизира количката с актуалния каталог (цени, премахнати продукти)
function reconcileCart() {
  cartState = cartState.filter(item => {
    const prod = PRODUCTS_DATA.find(p => p.id === String(item.id));
    const size = prod && prod.sizes.find(s => s.variantId === item.variantId);
    if (!size) return false;
    item.price = size.price;
    return true;
  });
  saveCart();
  updateCartUI();
}

// --- Филтриране ---
window.filterByType = function(type) {
  activeFilterType = type;
  
  document.querySelectorAll('.type-tab-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-type') === type);
  });

  loadProducts();

  const catalogEl = document.getElementById('products');
  if (catalogEl) {
    catalogEl.scrollIntoView({ behavior: 'smooth' });
  }
};

window.filterByRegion = function(regionName) {
  activeFilterRegion = regionName;
  showToast(`📍 Избран регион: ${regionName}`);
  loadProducts();

  const catalogEl = document.getElementById('products');
  if (catalogEl) {
    catalogEl.scrollIntoView({ behavior: 'smooth' });
  }
};

let searchDebounce = null;
window.handleSearchInput = function(query) {
  activeSearchQuery = query.trim();
  clearTimeout(searchDebounce);
  searchDebounce = setTimeout(loadProducts, 300);
};

// --- Визуализация на Каталога ---
function renderProducts() {
  const grid = document.getElementById('products-grid');
  if (!grid) return;

  const filtered = VISIBLE_PRODUCTS;

  if (filtered.length === 0) {
    grid.innerHTML = `
      <div style="grid-column: 1 / -1; text-align: center; padding: 45px 20px; background: rgba(26, 13, 8, 0.5); border: 1px dashed rgba(245, 158, 11, 0.3); border-radius: 16px;">
        <span style="font-size: 2.5rem; display: block; margin-bottom: 10px;">🐝</span>
        <h3 style="color: #FFF; font-size: 1.2rem; margin-bottom: 6px;">Няма намерени продукти по тези критерии</h3>
        <p style="color: var(--text-soft); max-width: 440px; margin: 0 auto 16px; font-size: 0.9rem;">Опитайте да изчистите филтрите или напишете друг регион.</p>
        <button class="btn btn-outline" onclick="resetFilters()">Изчисти филтрите</button>
      </div>
    `;
    return;
  }

  grid.innerHTML = filtered.map(p => {
    const isPollen = p.type === "Пчелен прашец";
    const typeIcon = isPollen ? "🌼" : "🍯";
    const producerText = p.producer || "Сертифициран Български Пчелин";
    const babhText = p.babh || "БАБХ Регистрация";
    const phoneContact = p.phone || "0888 123 456";

    return `
      <div class="product-card" id="card-${p.id}">
        <div class="product-img-wrapper" onclick="openQuickView('${p.id}')">
          <span class="product-tag" style="${isPollen ? 'background: #D97706;' : ''}">${typeIcon} ${p.tag}</span>
          <img src="${p.image}" alt="${p.name}" class="product-img">
          <button class="product-quickview-btn" title="Преглед"><i data-lucide="eye"></i></button>
          <span class="product-origin-tag">📍 ${p.origin}</span>
        </div>
        <div class="product-body">
          <h3 class="product-title">${p.name}</h3>
          
          <div class="product-producer-box">
            <span class="product-producer-name">👨‍🌾 ${producerText}</span>
            <span class="product-babh-tag">${babhText}</span>
          </div>

          <p class="product-desc">${p.description}</p>

          <div style="margin: 8px 0 10px; display: flex; justify-content: space-between; align-items: center;">
            <a href="tel:${phoneContact}" class="product-beekeeper-contact" title="Обадете се на пчеларя">
              <span>📞 Пчелар: <strong>${phoneContact}</strong></span>
            </a>
          </div>

          <select class="product-size-select" id="size-select-${p.id}" onchange="updateProductPrice('${p.id}')">
            ${p.sizes.map((s, index) => `
              <option value="${index}">${s.label} — ${s.price.toFixed(2)} €</option>
            `).join('')}
          </select>

          <div class="product-footer">
            <div class="product-price" id="price-display-${p.id}">
              ${p.sizes[0].price.toFixed(2)} <span>€</span>
            </div>

            <button class="btn btn-primary" onclick="handleAddToCart('${p.id}')">
              <i data-lucide="shopping-bag"></i> В Количката
            </button>
          </div>
        </div>
      </div>
    `;
  }).join('');

  if (window.lucide) lucide.createIcons();
}

window.resetFilters = function() {
  activeFilterType = 'all';
  activeFilterCategory = 'all';
  activeFilterRegion = 'all';
  activeSearchQuery = '';

  const searchInput = document.getElementById('catalog-search-input');
  if (searchInput) searchInput.value = '';

  document.querySelectorAll('.type-tab-btn').forEach(b => {
    b.classList.toggle('active', b.getAttribute('data-type') === 'all');
  });

  document.querySelectorAll('.filter-btn').forEach(b => {
    b.classList.toggle('active', b.getAttribute('data-filter') === 'all');
  });

  loadProducts();
};

function updateProductPrice(productId) {
  const product = PRODUCTS_DATA.find(p => p.id === productId);
  const select = document.getElementById(`size-select-${productId}`);
  const priceDisplay = document.getElementById(`price-display-${productId}`);
  
  if (product && select && priceDisplay) {
    const selectedSizeIndex = parseInt(select.value);
    const sizeObj = product.sizes[selectedSizeIndex];
    priceDisplay.innerHTML = `${sizeObj.price.toFixed(2)} <span>€</span>`;
  }
}

// --- Бърз преглед на продукт ---
window.openQuickView = function(productId) {
  const product = PRODUCTS_DATA.find(p => p.id === productId);
  const modal = document.getElementById('quickview-modal');
  const title = document.getElementById('qv-title');
  const body = document.getElementById('qv-body');

  if (!product || !modal || !body) return;

  if (title) title.textContent = product.name;

  body.innerHTML = `
    <div class="quickview-grid">
      <div class="quickview-media">
        <img src="${product.image}" alt="${product.name}" style="width:100%; border-radius:var(--radius-md); box-shadow:var(--card-shadow); max-height:280px; object-fit:contain; margin:0 auto; display:block;">
      </div>
      <div class="quickview-info">
        <span class="badge-tag">${product.tag}</span>
        <h4 style="font-size:1.3rem; margin:10px 0;">📍 ${product.origin}</h4>
        <div style="background: rgba(245, 158, 11, 0.1); padding: 10px 14px; border-radius: 8px; margin-bottom: 12px; font-size: 0.88rem;">
          <strong style="color: #FCD34D;">Производител:</strong> ${product.producer || 'Български пчелин'}<br>
          <strong style="color: #FCD34D;">БАБХ рег. №:</strong> ${product.babh || 'Регистриран обект'}<br>
          <strong style="color: #FCD34D;">Телефон за връзка:</strong> <a href="tel:${product.phone || '0888123456'}" style="color:#FFF;">${product.phone || '0888 123 456'}</a>
        </div>
        <p style="color:var(--text-muted); margin-bottom:15px; font-size:0.92rem; line-height:1.5;">${product.description}</p>
        
        <div style="background:rgba(26, 13, 8, 0.85); border:1px solid var(--amber-border); padding:12px 14px; border-radius:var(--radius-sm); margin-bottom:15px; color:var(--text-soft); font-size:0.86rem;">
          <p style="margin-bottom:5px;"><strong>Вкус и аромат:</strong> ${product.tastingNotes}</p>
          <p style="margin-bottom:5px;"><strong>Качество:</strong> ${product.pollenPurity}</p>
          <p style="margin-bottom:0;"><strong>Реколта:</strong> ${product.harvestDate}</p>
        </div>

        <div class="quickview-actions">
          <span style="font-size:1.6rem; font-weight:700; font-family:var(--font-serif);" class="text-gold">
            ${product.sizes[0].price.toFixed(2)} €
          </span>
          <button class="btn btn-primary btn-lg quickview-buy-btn" onclick="handleAddToCart('${product.id}'); document.getElementById('quickview-modal').classList.remove('active');">
            <i data-lucide="shopping-bag"></i> Добави в Количката
          </button>
        </div>
      </div>
    </div>
  `;

  modal.classList.add('active');
  if (window.lucide) lucide.createIcons();
};

// --- Списък с производители ---
function renderBeekeepers() {
  const container = document.getElementById('beekeepers-grid');
  if (!container) return;

  container.innerHTML = BEEKEEPERS_DATA.map(b => `
    <div class="beekeeper-card">
      <div class="beekeeper-header">
        <div>
          <h3 class="beekeeper-name">${b.fullName}</h3>
          <div class="beekeeper-apiary">🏡 ${b.apiaryName}</div>
        </div>
        <span class="beekeeper-badge-verified">✓ БАБХ Проверен</span>
      </div>

      <div class="beekeeper-meta-row">
        <span class="beekeeper-meta-item">📍 ${b.region} (${b.cityOrVillage})</span>
        <span class="beekeeper-meta-item">📜 ${b.registrationNumber}</span>
        <span class="beekeeper-meta-item">🐝 ${b.hiveCount} кошера</span>
        <span class="beekeeper-meta-item">⏳ ${b.experienceYears} г. опит</span>
      </div>

      <p class="beekeeper-bio">${b.bio}</p>

      <div class="beekeeper-footer">
        <span class="beekeeper-products-count">
          ${b.hasHoney ? '🍯 Мед' : ''} ${b.hasPollen ? '• 🌼 Прашец' : ''}
        </span>
        <a href="tel:${b.phone}" class="btn btn-outline" style="padding: 6px 14px; font-size: 0.82rem;">
          <span>📞 ${b.phone}</span>
        </a>
      </div>
    </div>
  `).join('');
}

// --- Модал за нов пчелар ---
window.openBeekeeperModal = function() {
  document.getElementById('beekeeper-modal')?.classList.add('active');
};

window.closeBeekeeperModal = function() {
  document.getElementById('beekeeper-modal')?.classList.remove('active');
};

window.handleBeekeeperSubmit = async function() {
  const name = document.getElementById('bk-name')?.value.trim();
  const apiary = document.getElementById('bk-apiary')?.value.trim();
  const babh = document.getElementById('bk-babh')?.value.trim();
  const region = document.getElementById('bk-region')?.value;
  const city = document.getElementById('bk-city')?.value.trim();
  const phone = document.getElementById('bk-phone')?.value.trim();
  const offerHoney = document.getElementById('bk-offer-honey')?.checked ?? true;
  const offerPollen = document.getElementById('bk-offer-pollen')?.checked ?? false;
  const hives = parseInt(document.getElementById('bk-hives')?.value || '50', 10);
  const details = document.getElementById('bk-details')?.value.trim() || 'Чист български пчелен мед и прашец';

  if (!name || !apiary || !babh || !city || !phone) {
    showToast('⚠️ Моля, попълнете задължителните полета!');
    return;
  }

  try {
    const res = await apiFetch('/beekeepers/apply', {
      method: 'POST',
      body: JSON.stringify({
        fullName: name,
        apiaryName: apiary,
        registrationNumber: babh,
        region: region,
        cityOrVillage: city,
        phone: phone,
        offersHoney: offerHoney,
        offersPollen: offerPollen,
        productDetails: details,
        hiveCount: hives
      })
    });

    document.getElementById('beekeeper-form')?.reset();
    window.closeBeekeeperModal();
    showToast(`🎉 ${res.message}`);
  } catch (err) {
    showToast(`❌ ${err.message}`);
  }
};

// --- Количка ---
function handleAddToCart(productId) {
  const product = PRODUCTS_DATA.find(p => p.id === productId);
  if (!product) return;

  const select = document.getElementById(`size-select-${productId}`);
  const selectedSizeIndex = select ? parseInt(select.value) : 0;
  const sizeObj = product.sizes[selectedSizeIndex] || product.sizes[0];

  const cartItemId = `${product.id}_${sizeObj.variantId}`;
  const existing = cartState.find(item => item.cartItemId === cartItemId);

  if (existing) {
    existing.qty += 1;
  } else {
    cartState.push({
      cartItemId: cartItemId,
      id: product.id,
      name: product.name,
      producer: product.producer,
      image: product.image,
      sizeLabel: sizeObj.label,
      variantId: sizeObj.variantId,
      weight: sizeObj.weight,
      price: sizeObj.price,
      qty: 1
    });
  }

  saveCart();
  updateCartUI();
  showToast(`🍯 Добавено: ${product.name}`);
  openCartDrawer();
}

function saveCart() {
  localStorage.setItem('pchelari_bg_cart', JSON.stringify(cartState));
}

function updateCartUI() {
  const countBadge = document.getElementById('cart-badge');
  const drawerCount = document.getElementById('cart-drawer-count');
  const itemsContainer = document.getElementById('cart-items-container');
  const subtotalEl = document.getElementById('cart-subtotal');
  const discountEl = document.getElementById('cart-discount');
  const discountRow = document.getElementById('discount-row');
  const shippingEl = document.getElementById('cart-shipping');
  const totalEl = document.getElementById('cart-total');
  const shippingBanner = document.getElementById('cart-shipping-banner');

  const totalQty = cartState.reduce((sum, item) => sum + item.qty, 0);
  if (countBadge) countBadge.textContent = totalQty;
  if (drawerCount) drawerCount.textContent = totalQty;

  const subtotal = cartState.reduce((sum, item) => sum + (item.price * item.qty), 0);
  const discountVal = subtotal * appliedDiscount;
  const netTotal = subtotal - discountVal;

  // Сумите са в евро и съвпадат с OrderService.FreeShippingThreshold / ShippingCost на сървъра.
  const FREE_SHIPPING_THRESHOLD = 25.00;
  const SHIPPING_COST = 3.00;
  let shippingCost = netTotal >= FREE_SHIPPING_THRESHOLD || netTotal === 0 ? 0 : SHIPPING_COST;

  if (itemsContainer) {
    if (cartState.length === 0) {
      itemsContainer.innerHTML = `
        <div class="text-center" style="padding: 40px 0; color:var(--text-muted);">
          <div style="font-size:2.5rem; margin-bottom:8px;">🍯</div>
          <p>Количката ви е празна</p>
          <a href="#products" class="btn btn-outline" style="margin-top:12px;" onclick="closeCartDrawer()">Към Каталога</a>
        </div>
      `;
    } else {
      itemsContainer.innerHTML = cartState.map(item => `
        <div class="cart-item">
          <img src="${item.image}" alt="${item.name}" class="cart-item-img">
          <div class="cart-item-details">
            <div class="cart-item-title">${item.name}</div>
            <div style="font-size:0.75rem; color:#FCD34D;">${item.producer || ''}</div>
            <div class="cart-item-size">${item.sizeLabel} — ${item.price.toFixed(2)} €</div>
            <div class="cart-qty-controls">
              <button class="qty-btn" onclick="changeQty('${item.cartItemId}', -1)">-</button>
              <span><strong>${item.qty}</strong></span>
              <button class="qty-btn" onclick="changeQty('${item.cartItemId}', 1)">+</button>
              <button class="close-btn" style="font-size:1rem; margin-left:auto;" onclick="removeItem('${item.cartItemId}')">
                <i data-lucide="trash-2"></i>
              </button>
            </div>
          </div>
        </div>
      `).join('');
    }
  }

  if (shippingBanner) {
    if (netTotal >= FREE_SHIPPING_THRESHOLD || netTotal === 0) {
      shippingBanner.innerHTML = `✅ Поръчката ви е с <strong>БЕЗПЛАТНА доставка</strong> за България!`;
    } else {
      const remaining = FREE_SHIPPING_THRESHOLD - netTotal;
      shippingBanner.innerHTML = `🚚 Добавете още <strong>${remaining.toFixed(2)} €</strong> за БЕЗПЛАТНА доставка!`;
    }
  }

  if (subtotalEl) subtotalEl.textContent = `${subtotal.toFixed(2)} €`;
  
  if (discountRow) {
    if (appliedDiscount > 0) {
      discountRow.style.display = 'flex';
      discountEl.textContent = `-${discountVal.toFixed(2)} €`;
    } else {
      discountRow.style.display = 'none';
    }
  }

  if (shippingEl) shippingEl.textContent = shippingCost === 0 ? 'БЕЗПЛАТНА' : `${shippingCost.toFixed(2)} €`;
  const grandTotal = netTotal + shippingCost;
  if (totalEl) totalEl.textContent = `${grandTotal.toFixed(2)} €`;

  const ckTotal = document.getElementById('ck-summary-total');
  if (ckTotal) ckTotal.textContent = `${grandTotal.toFixed(2)} €`;

  if (window.lucide) lucide.createIcons();
}

function changeQty(cartItemId, delta) {
  const item = cartState.find(i => i.cartItemId === cartItemId);
  if (item) {
    item.qty += delta;
    if (item.qty <= 0) {
      removeItem(cartItemId);
      return;
    }
    saveCart();
    updateCartUI();
  }
}

function removeItem(cartItemId) {
  cartState = cartState.filter(i => i.cartItemId !== cartItemId);
  saveCart();
  updateCartUI();
  showToast('🗑️ Продуктът бе премахнат');
}

function openCartDrawer() {
  document.getElementById('cart-drawer-overlay')?.classList.add('active');
  document.getElementById('cart-drawer')?.classList.add('active');
}

function closeCartDrawer() {
  document.getElementById('cart-drawer-overlay')?.classList.remove('active');
  document.getElementById('cart-drawer')?.classList.remove('active');
}

function showToast(message) {
  const container = document.getElementById('toast-container');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = 'toast';
  toast.innerHTML = `<span>${message}</span>`;
  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    setTimeout(() => toast.remove(), 300);
  }, 3000);
}

// --- Финализиране на поръчката ---
window.handleCheckoutSubmit = async function() {
  const name = document.getElementById('ck-name')?.value;
  const phone = document.getElementById('ck-phone')?.value;
  const email = document.getElementById('ck-email')?.value || '';
  const city = document.getElementById('ck-city')?.value;
  const address = document.getElementById('ck-address')?.value;

  if (cartState.length === 0) {
    showToast('⚠️ Количката ви е празна!');
    return;
  }

  const submitBtn = document.querySelector('#checkout-form button[type="submit"]');
  if (submitBtn) submitBtn.disabled = true;

  try {
    // Сървърът сам изчислява цените, отстъпката, доставката и общата сума
    const result = await apiFetch('/orders', {
      method: 'POST',
      body: JSON.stringify({
        customerName: name,
        customerPhone: phone,
        customerEmail: email || null,
        deliveryCity: city,
        deliveryAddress: address,
        paymentMethod: selectedPaymentMethodName,
        promoCode: appliedPromoCode,
        items: cartState.map(i => ({ productVariantId: i.variantId, quantity: i.qty }))
      })
    });

    const receiptOrderId = document.getElementById('receipt-order-id');
    const receiptCity = document.getElementById('receipt-city');
    const receiptPayment = document.getElementById('receipt-payment');
    const receiptTotal = document.getElementById('receipt-total');

    if (receiptOrderId) receiptOrderId.textContent = `#${result.orderNumber}`;
    if (receiptCity) receiptCity.textContent = city;
    if (receiptPayment) receiptPayment.textContent = selectedPaymentMethodName;
    if (receiptTotal) receiptTotal.textContent = `${Number(result.total).toFixed(2)} €`;

    showToast(`🎉 Поръчка #${result.orderNumber} е регистрирана успешно!`);

    cartState = [];
    appliedDiscount = 0;
    appliedPromoCode = null;
    saveCart();
    updateCartUI();

    document.getElementById('checkout-modal')?.classList.remove('active');
    document.getElementById('success-modal')?.classList.add('active');
  } catch (err) {
    // Количката се запазва, за да може клиентът да опита отново
    showToast(`❌ Поръчката не беше приета: ${err.message}`);
  } finally {
    if (submitBtn) submitBtn.disabled = false;
  }
};

// --- Помощ при избор (Quiz) ---
function renderQuizStep() {
  const container = document.getElementById('quiz-body');
  const progressFill = document.getElementById('quiz-progress-fill');
  if (!container) return;

  if (currentQuizStep < QUIZ_STEPS.length) {
    const stepData = QUIZ_STEPS[currentQuizStep];
    const progressPercent = ((currentQuizStep + 1) / QUIZ_STEPS.length) * 100;
    if (progressFill) progressFill.style.width = `${progressPercent}%`;

    container.innerHTML = `
      <h3 class="text-center" style="font-size:1.3rem; margin-bottom:18px;">
        Стъпка ${stepData.step} от ${QUIZ_STEPS.length}: ${stepData.title}
      </h3>
      <div class="quiz-options-grid">
        ${stepData.options.map(opt => `
          <div class="quiz-option-card" onclick="selectQuizOption('${opt.val}')">
            <div class="quiz-option-icon">${opt.icon}</div>
            <strong>${opt.label}</strong>
          </div>
        `).join('')}
      </div>
    `;
  } else {
    if (progressFill) progressFill.style.width = `100%`;
    calculateQuizRecommendation();
  }
}

function selectQuizOption(value) {
  quizAnswers[`step_${currentQuizStep}`] = value;
  currentQuizStep++;
  renderQuizStep();
}

function calculateQuizRecommendation() {
  const container = document.getElementById('quiz-body');
  if (!container) return;

  const goal = quizAnswers['step_0'] || 'immunity';
  const taste = quizAnswers['step_1'] || 'herbal';

  let recommended = PRODUCTS_DATA.find(p => p.quizMatch && p.quizMatch.includes(goal) && p.quizMatch.includes(taste));
  if (!recommended) recommended = PRODUCTS_DATA[0];
  if (!recommended) {
    container.innerHTML = '<p class="text-center" style="color:var(--text-muted);">Продуктите все още се зареждат или сървърът не е достъпен. Опитайте отново след малко.</p>';
    return;
  }

  container.innerHTML = `
    <div class="quiz-recommendation-box">
      <img src="${recommended.image}" alt="${recommended.name}" class="quiz-rec-img">
      <div>
        <span class="badge-tag">Препоръчан продукт за вашите нужди</span>
        <h3 style="font-size:1.5rem; margin: 8px 0;">${recommended.name}</h3>
        <p style="color:var(--text-muted); margin-bottom:12px; font-size:0.92rem;">${recommended.description}</p>
        <p style="font-size:0.88rem;"><strong>Регион:</strong> ${recommended.origin}</p>
        <div style="margin-top:18px; display:flex; gap:12px; align-items:center;">
          <span style="font-size:1.5rem; font-weight:700; font-family:var(--font-serif);" class="text-gold">
            ${recommended.sizes[0].price.toFixed(2)} €
          </span>
          <button class="btn btn-primary" onclick="handleAddToCart('${recommended.id}')">
            <i data-lucide="shopping-bag"></i> В Количката
          </button>
          <button class="btn btn-outline" onclick="resetQuiz()">
            <i data-lucide="rotate-ccw"></i> Отново
          </button>
        </div>
      </div>
    </div>
  `;

  if (window.lucide) lucide.createIcons();
}

function resetQuiz() {
  currentQuizStep = 0;
  quizAnswers = {};
  renderQuizStep();
}

// --- Слушатели на събития ---
function setupEventListeners() {
  const navbar = document.getElementById('navbar');
  const navLinksAnchors = document.querySelectorAll('#nav-links a');
  const sections = document.querySelectorAll('section[id]');

  function onNavScroll() {
    if (navbar) {
      navbar.classList.toggle('scrolled', window.scrollY > 50);
    }

    let currentSection = '';
    sections.forEach(section => {
      if (window.scrollY >= section.offsetTop - 120) {
        currentSection = section.getAttribute('id');
      }
    });

    navLinksAnchors.forEach(link => {
      link.classList.toggle('active-link', link.getAttribute('href') === `#${currentSection}`);
    });
  }

  window.addEventListener('scroll', onNavScroll, { passive: true });
  onNavScroll();

  // Мобилно меню
  const mobileBtn = document.getElementById('mobile-menu-btn');
  const navLinks = document.getElementById('nav-links');

  mobileBtn?.addEventListener('click', () => {
    navLinks?.classList.toggle('active');
    document.body.style.overflow = navLinks?.classList.contains('active') ? 'hidden' : '';
  });

  navLinksAnchors.forEach(link => {
    link.addEventListener('click', () => {
      navLinks?.classList.remove('active');
      document.body.style.overflow = '';
    });
  });

  // Количка
  document.getElementById('open-cart-btn')?.addEventListener('click', openCartDrawer);
  document.getElementById('close-cart-btn')?.addEventListener('click', closeCartDrawer);
  document.getElementById('cart-drawer-overlay')?.addEventListener('click', closeCartDrawer);

  // Категории
  const filterBtns = document.querySelectorAll('.filter-btn');
  filterBtns.forEach(btn => {
    btn.addEventListener('click', (e) => {
      filterBtns.forEach(b => b.classList.remove('active'));
      e.target.classList.add('active');
      activeFilterCategory = e.target.getAttribute('data-filter');
      loadProducts();
    });
  });

  // Промокод – проверява се от сървъра
  document.getElementById('apply-promo-btn')?.addEventListener('click', async () => {
    const input = document.getElementById('promo-input');
    const msg = document.getElementById('promo-message');
    const code = input ? input.value.trim() : '';
    if (!code) return;
    try {
      const promo = await apiFetch(`/promocodes/validate?code=${encodeURIComponent(code)}`);
      appliedDiscount = promo.discountPercent / 100;
      appliedPromoCode = promo.code;
      if (msg) {
        msg.textContent = `✅ Промокодът е приложен! ${promo.discountPercent}% отстъпка.`;
        msg.style.color = '#34D399';
      }
    } catch (err) {
      appliedDiscount = 0;
      appliedPromoCode = null;
      if (msg) {
        msg.textContent = err.status === 404 ? '❌ Невалиден промо код' : `❌ ${err.message}`;
        msg.style.color = '#EF4444';
      }
    }
    updateCartUI();
  });

  // Модал поръчка
  document.getElementById('proceed-checkout-btn')?.addEventListener('click', () => {
    if (cartState.length === 0) {
      showToast('⚠️ Количката ви е празна!');
      return;
    }
    closeCartDrawer();
    document.getElementById('checkout-modal')?.classList.add('active');
  });

  document.getElementById('close-checkout-btn')?.addEventListener('click', () => {
    document.getElementById('checkout-modal')?.classList.remove('active');
  });
}

// --- Реални клиентски отзиви ---

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function initStarRatingSelector() {
  const container = document.getElementById('star-rating-select');
  const input = document.getElementById('review-rating-value');
  if (!container || !input) return;

  const starBtns = container.querySelectorAll('.star-btn');
  window.setStarRating = function(rating) {
    input.value = rating;
    starBtns.forEach(btn => {
      const r = parseInt(btn.dataset.rating, 10);
      btn.classList.toggle('active', r <= rating);
      btn.classList.toggle('inactive', r > rating);
    });
  };

  starBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      window.setStarRating(parseInt(btn.dataset.rating, 10));
    });
  });

  window.setStarRating(5);
}

function formatReviewDate(iso) {
  const d = new Date(iso);
  if (isNaN(d)) return 'Наскоро';
  return `${String(d.getDate()).padStart(2, '0')}.${String(d.getMonth() + 1).padStart(2, '0')}.${d.getFullYear()}`;
}

async function renderUserReviews() {
  const grid = document.getElementById('reviews-grid');
  if (!grid) return;

  let reviews = [];
  try {
    reviews = await apiFetch('/reviews');
  } catch (err) {
    showLoadError('reviews-grid', 'Отзивите не можаха да бъдат заредени. ' + err.message, 'renderUserReviews');
    return;
  }

  if (reviews.length === 0) {
    grid.innerHTML = `
      <div style="grid-column: 1 / -1; text-align: center; padding: 40px; color: var(--text-muted);">
        <p>Все още няма споделени отзиви. Напишете вашето мнение във формата по-горе!</p>
      </div>
    `;
    return;
  }

  grid.innerHTML = reviews.map(r => {
    const ratingNum = Math.max(1, Math.min(5, parseInt(r.rating || 5, 10)));
    const stars = '★'.repeat(ratingNum) + '☆'.repeat(5 - ratingNum);
    const initials = (r.authorName || 'Клиент').trim().split(/\s+/).filter(Boolean).slice(0, 2).map(w => w[0].toUpperCase()).join('') || 'К';
    const cityText = r.city ? `${escapeHtml(r.city)} • ` : '';

    return `
      <div class="review-card card-glass reveal is-visible">
        <div class="review-stars" style="color:var(--primary-gold); font-size:1.2rem; letter-spacing:3px; margin-bottom:10px;">${stars}</div>
        <p class="review-text" style="line-height:1.6; color:var(--text-soft); margin-bottom:16px; font-size:0.92rem;">"${escapeHtml(r.comment)}"</p>
        <div class="review-author" style="display:flex; align-items:center; gap:12px;">
          <div class="author-avatar" style="width:40px; height:40px; background:var(--primary-gold); color:#0b0503; font-weight:700; border-radius:50%; display:flex; align-items:center; justify-content:center; flex-shrink:0;">${initials}</div>
          <div>
            <strong style="color:#FAF4EB; display:block; font-size:0.95rem;">${escapeHtml(r.authorName)}</strong>
            <span style="font-size:0.8rem; color:var(--text-muted);">${cityText}Проверен клиент (${escapeHtml(formatReviewDate(r.createdAt))})</span>
          </div>
        </div>
      </div>
    `;
  }).join('');
}

window.handleUserReviewSubmit = async function() {
  const nameEl = document.getElementById('review-author-name');
  const cityEl = document.getElementById('review-author-city');
  const textEl = document.getElementById('review-comment-text');
  const ratingEl = document.getElementById('review-rating-value');

  const name = nameEl?.value.trim();
  const city = cityEl?.value.trim();
  const text = textEl?.value.trim();
  const rating = parseInt(ratingEl?.value || '5', 10);

  if (!name || !text) {
    showToast('⚠️ Моля, попълнете вашето име и отзив!');
    return;
  }

  try {
    const res = await apiFetch('/reviews', {
      method: 'POST',
      body: JSON.stringify({ authorName: name, city, rating, comment: text })
    });

    if (nameEl) nameEl.value = '';
    if (cityEl) cityEl.value = '';
    if (textEl) textEl.value = '';
    if (window.setStarRating) window.setStarRating(5);

    showToast(`🍯 ${res.message}`);
  } catch (err) {
    showToast(`❌ ${err.message}`);
  }
};
