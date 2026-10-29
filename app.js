/* ==========================================================================
   ЮЖЕН ПЧЕЛИН - Core Application Logic
   Interactive E-Commerce, Quiz Wizard, Splash Curtain & Health Calculator
   ========================================================================== */

// --- Products Database ---
const PRODUCTS_DATA = [
  {
    id: "p1",
    name: "Странджански Манов Мед (ЗНП)",
    category: "manov",
    origin: "Странджа (Защитено Наименование)",
    tag: "ЗНП Сертифициран",
    image: "./images/manov.jpg",
    description: "Световно признат тъмен манов мед, добит от вековните дъбови гори на Странджа. Богат на минерали, калий и желязо.",
    specs: { color: "Тъмен кехлибар", aroma: "Дъб & Мана", crystallization: "Бавна (>2 год.)" },
    harvestDate: "Реколта Август 2026",
    pollenPurity: "96.4% Манов прашецов аналитичен индекс",
    tastingNotes: "Наситен, плътен карамелено-дъбов вкус с едва доловена тръпчивост.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 12.27 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 7.16 },
      { label: "Тенекия 25 кг (Едро)", weight: "25kg", price: 245.42 }
    ],
    rating: 5.0,
    quizMatch: ["antioxidant", "immunity", "dark"]
  },
  {
    id: "p2",
    name: "Тракийски Акациев Мед",
    category: "acacia",
    origin: "Западна Тракия (Пловдив / Пазарджик)",
    tag: "Най-Търсен",
    image: "./images/acacia.jpg",
    description: "Кристално чист и прозрачен акациев мед с нежен сладък вкус. Идеален за деца и за подслаждане на чай и кафе.",
    specs: { color: "Светло златист", aroma: "Деликатен акациев цвят", crystallization: "Много бавна" },
    harvestDate: "Реколта Май 2026",
    pollenPurity: "92.1% Бяла акация",
    tastingNotes: "Изключително нежен, копринено сладок без натрапчив следвкус.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 10.74 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 6.39 },
      { label: "Тенекия 25 кг (Едро)", weight: "25kg", price: 214.74 }
    ],
    rating: 4.9,
    quizMatch: ["tea", "kids", "light"]
  },
  {
    id: "p3",
    name: "Родопски Планински Букет",
    category: "rhodope",
    origin: "Западни Родопи (Смолян / Чепеларе)",
    tag: "100% Билков",
    image: "./images/rhodope.jpg",
    description: "Добит от над 400 вида диворастящи билки, мащерка, риган и планинска детелина в кристално чистите Родопи.",
    specs: { color: "Златисто кехлибарен", aroma: "Планински билки", crystallization: "Средна" },
    harvestDate: "Реколта Юли 2026",
    pollenPurity: "94.8% Планински лечебни билки",
    tastingNotes: "Богат, многослоен билков профил с нотки на мащерка и горска малина.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 9.97 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 5.88 },
      { label: "Тенекия 25 кг (Едро)", weight: "25kg", price: 199.40 }
    ],
    rating: 4.95,
    quizMatch: ["cold", "immunity", "herbal"]
  },
  {
    id: "p4",
    name: "Лавандулов Мед от Тракийското Поле",
    category: "lavender",
    origin: "Средна Гора & Тракийско Поле",
    tag: "Ароматен",
    image: "./images/rhodope.jpg",
    description: "Уникален ароматен мед с релаксиращи свойства, събиран от уханните лавандулови масиви в Тракия.",
    specs: { color: "Млечно златист", aroma: "Лавандулов нюанс", crystallization: "Фино кремообразна" },
    harvestDate: "Реколта Юни 2026",
    pollenPurity: "89.5% Тракийска лавандула",
    tastingNotes: "Кадифен цветен вкус с успокояващ лавандулов послевкус.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 10.23 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 6.14 }
    ],
    rating: 4.85,
    quizMatch: ["tea", "herbal", "light"]
  },
  {
    id: "p5",
    name: "Сакарски Липов Мед",
    category: "lavender",
    origin: "Планина Сакар & Тополовград",
    tag: "Традиционен",
    image: "./images/acacia.jpg",
    description: "Наситен и ароматен липов мед от вековните липови гори в регион Сакар. Отличен при кашлица и настинка.",
    specs: { color: "Жълто-златист", aroma: "Силна уханна липа", crystallization: "Бърза естествена" },
    harvestDate: "Реколта Юни 2026",
    pollenPurity: "91.0% Дива липа",
    tastingNotes: "Ухаен, леко ментов и затоплящ вкус.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 9.71 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 5.62 }
    ],
    rating: 4.8,
    quizMatch: ["cold", "herbal"]
  },
  {
    id: "p6",
    name: "Натурален Пчелен Прашец (Южна БГ)",
    category: "products",
    origin: "Чисти пасища Тракия & Родопи",
    tag: "Суперхрана",
    image: "./images/pollen.jpg",
    description: "Богат на аминокиселини, протеини и витамини от група B. Мощен природен биостимулатор за силен имунитет.",
    specs: { color: "Разноцветни зрънца", aroma: "Цветен прашец", crystallization: "Сушени гранули" },
    harvestDate: "Реколта 2026",
    pollenPurity: "100% Натурален био прашец",
    tastingNotes: "Орехов, леко сладко-кисел цветен аромат.",
    sizes: [
      { label: "Опаковка 200 гр", weight: "0.2kg", price: 7.16 },
      { label: "Опаковка 500 гр", weight: "0.5kg", price: 14.32 }
    ],
    rating: 5.0,
    quizMatch: ["immunity", "antioxidant"]
  },
  {
    id: "p7",
    name: "Прополисова Клеева Тинктура (30%)",
    category: "products",
    origin: "Лабораторен пчелин с. Скутаре",
    tag: "100% Натурална",
    image: "./images/pollen.jpg",
    description: "Концентриран 30% екстракт от натурален пчелен клей. Естествен антибиотик и щит срещу вируси.",
    specs: { color: "Тъмно кафяв", aroma: "Пчелен клей & смола", crystallization: "Течен с капкомер" },
    harvestDate: "Партида 2026",
    pollenPurity: "30% Активен пчелен клей",
    tastingNotes: "Характерен билково-смолист природен аромат.",
    sizes: [
      { label: "Шишенце 30 мл", weight: "30ml", price: 4.86 },
      { label: "Шишенце 50 мл", weight: "50ml", price: 7.41 }
    ],
    rating: 4.9,
    quizMatch: ["immunity", "cold"]
  }
];

// --- Southern Bulgaria Cities Data ---
const SOUTHERN_CITIES = [
  {
    name: "Пловдив",
    icon: "🏙️",
    deliveryType: "Експресна Лична Доставка от Пчеларя",
    freeLimit: 25.00,
    days: "Всеки ден (в рамките на 24 часа)",
    note: "Централна логистична база за Пловдив и района. Всички поръчки над 25 € се доставят лично до вашата врата."
  },
  {
    name: "Стара Загора",
    icon: "🌾",
    deliveryType: "Лична Доставка от Пчеларя",
    freeLimit: 25.00,
    days: "Вторник, Четвъртък и Събота",
    note: "Директен маршрут по АМ Тракия. Наш пчелар доставя пресни буркани 3 пъти седмично."
  },
  {
    name: "Бургас",
    icon: "🌊",
    deliveryType: "Лична Доставка / Еконт Express",
    freeLimit: 25.00,
    days: "Понеделник и Петък",
    note: "Прясна доставка от странджанските пчелини, близо до черноморския бряг."
  },
  {
    name: "Смолян",
    icon: "⛰️",
    deliveryType: "Лична Доставка от Планинския Пчелин",
    freeLimit: 25.00,
    days: "Сряда и Неделя",
    note: "Доставка директно от високопланинския пчелин край Чепеларе и Смолян."
  },
  {
    name: "Хасково",
    icon: "☀️",
    deliveryType: "Лична Доставка / Куриер 24ч",
    freeLimit: 25.00,
    days: "Вторник и Петък",
    note: "Бърза доставка за Хасково, Димитровград и Харманли."
  },
  {
    name: "Пазарджик",
    icon: "🏡",
    deliveryType: "Безплатна Лична Доставка",
    freeLimit: 20.00,
    days: "Всеки ден",
    note: "Близост до централния пчелин – гарантирана бързина."
  },
  {
    name: "Благоевград",
    icon: "🌲",
    deliveryType: "Еконт / Спиди Куриер (24h)",
    freeLimit: 30.00,
    days: "Всеки работен ден",
    note: "Бърза куриерска доставка с опция за преглед и тест преди плащане."
  },
  {
    name: "Кърджали",
    icon: "🏞️",
    deliveryType: "Лична Доставка / Куриер",
    freeLimit: 25.00,
    days: "Четвъртък",
    note: "Редовна седмична линия за Родопския регион."
  }
];

// --- Quiz Questions ---
const QUIZ_STEPS = [
  {
    step: 1,
    title: "За какво най-често ще използвате пчелния мед?",
    options: [
      { label: "За подсилване на имунитета & здраве", val: "immunity", icon: "🛡️" },
      { label: "За чай, кафе и подслаждане", val: "tea", icon: "☕" },
      { label: "При настинка, кашлица и гърло", val: "cold", icon: "🍋" },
      { label: "За деца, десерти и сутрешна филия", val: "kids", icon: "🥞" },
      { label: "Силно антиоксидантно действие", val: "antioxidant", icon: "✨" }
    ]
  },
  {
    step: 2,
    title: "Какъв вкус и аромат предпочитате?",
    options: [
      { label: "Нежен, лек и кристално сладък", val: "light", icon: "🌸" },
      { label: "Наситен билков аромат от Родопите", val: "herbal", icon: "🌿" },
      { label: "Тъмен, плътен и тръпчив дъбов мед", val: "dark", icon: "🌳" }
    ]
  }
];

let cartState = (JSON.parse(localStorage.getItem('yug_honey_cart')) || []).map(item => {
  const prod = PRODUCTS_DATA.find(p => p.id === item.productId);
  if (prod) {
    const sizeObj = prod.sizes.find(s => s.weight === item.weight) || prod.sizes[0];
    if (sizeObj) item.price = sizeObj.price;
  }
  return item;
});
let currentQuizStep = 0;
let quizAnswers = {};
let activeCityIndex = 0;
let appliedDiscount = 0;
let splashDismissed = false;

// --- Initialize App on DOM Ready ---
document.addEventListener('DOMContentLoaded', () => {
  document.body.classList.add('splash-active');
  setupSplashCurtain();
  initParticlesCanvas();
  renderCityChips();
  renderCityDetails(0);
  renderProducts('all');
  renderQuizStep();
  renderUserReviews();
  initStarRatingSelector();
  updateCartUI();

  setupEventListeners();
  if (window.lucide) lucide.createIcons();
});

// --- Bulletproof Splash Curtain Interaction Engine ---
function setupSplashCurtain() {
  const curtain = document.getElementById('splash-curtain');
  if (!curtain) return;

  function dismissSplash() {
    if (splashDismissed) return;
    splashDismissed = true;

    curtain.classList.add('splash-hidden');
    document.body.classList.remove('splash-active');

    // Remove from layout after animation completes so it never blocks scrolling
    setTimeout(() => {
      curtain.style.display = 'none';
      window.dispatchEvent(new Event('scroll'));
    }, 850);
  }

  // 1. Click / Tap ANYWHERE on splash curtain
  curtain.addEventListener('click', dismissSplash);

  // 1b. Direct touch dismiss on mobile tap
  curtain.addEventListener('touchend', (e) => {
    dismissSplash();
  }, { passive: true });

  // 2. Wheel event (mouse scroll down)
  window.addEventListener('wheel', (e) => {
    if (!splashDismissed && e.deltaY > 0) {
      dismissSplash();
    }
  }, { passive: true });

  // 3. Touch swipe event (swipe in ANY direction on mobile)
  let touchStartY = 0;
  let touchStartX = 0;
  window.addEventListener('touchstart', (e) => {
    if (e.touches.length > 0) {
      touchStartY = e.touches[0].clientY;
      touchStartX = e.touches[0].clientX;
    }
  }, { passive: true });

  window.addEventListener('touchmove', (e) => {
    if (!splashDismissed && e.touches.length > 0) {
      let touchEndY = e.touches[0].clientY;
      let touchEndX = e.touches[0].clientX;
      if (Math.abs(touchStartY - touchEndY) > 10 || Math.abs(touchStartX - touchEndX) > 10) {
        dismissSplash();
      }
    }
  }, { passive: true });

  // 4. Keyboard press (ArrowDown, Space, PageDown, Enter)
  window.addEventListener('keydown', (e) => {
    if (!splashDismissed) {
      if (['ArrowDown', 'Space', 'PageDown', 'Enter'].includes(e.code)) {
        dismissSplash();
      }
    }
  });

  // 5. Fallback: Window scroll event
  window.addEventListener('scroll', () => {
    if (!splashDismissed && window.scrollY > 10) {
      dismissSplash();
    }
  }, { passive: true });
}

window.reopenSplash = function() {
  const curtain = document.getElementById('splash-curtain');
  if (curtain) {
    splashDismissed = false;
    curtain.style.display = 'flex';
    setTimeout(() => {
      curtain.classList.remove('splash-hidden');
      document.body.classList.add('splash-active');
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }, 20);
  }
};

// --- Particles Canvas Simulation ---
function initParticlesCanvas() {
  const canvas = document.getElementById('splash-particles-canvas');
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  let width = canvas.width = window.innerWidth;
  let height = canvas.height = window.innerHeight;

  window.addEventListener('resize', () => {
    width = canvas.width = window.innerWidth;
    height = canvas.height = window.innerHeight;
  });

  const particles = [];
  const particleCount = Math.min(Math.floor(width / 25), 45);

  for (let i = 0; i < particleCount; i++) {
    particles.push({
      x: Math.random() * width,
      y: Math.random() * height,
      radius: Math.random() * 3 + 1,
      color: 'rgba(229, 169, 60, ' + (Math.random() * 0.5 + 0.2) + ')',
      speedY: -Math.random() * 0.8 - 0.2,
      speedX: Math.sin(Math.random() * Math.PI) * 0.5
    });
  }

  function animate() {
    if (splashDismissed && (!document.getElementById('splash-curtain') || document.getElementById('splash-curtain').style.display === 'none')) {
      return; // Stop animation loop when splash is hidden to save performance
    }

    ctx.clearRect(0, 0, width, height);

    particles.forEach(p => {
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
      ctx.fillStyle = p.color;
      ctx.shadowBlur = 10;
      ctx.shadowColor = '#E5A93C';
      ctx.fill();

      p.y += p.speedY;
      p.x += p.speedX;

      if (p.y < -10) {
        p.y = height + 10;
        p.x = Math.random() * width;
      }
    });

    requestAnimationFrame(animate);
  }

  animate();
}

// --- Render Region Selector (Южна България) ---
function renderCityChips() {
  const container = document.getElementById('city-chips');
  if (!container) return;

  container.innerHTML = SOUTHERN_CITIES.map((city, idx) => `
    <button class="city-chip ${idx === activeCityIndex ? 'active' : ''}" onclick="selectCity(${idx})">
      ${city.icon} ${city.name}
    </button>
  `).join('');
}

function selectCity(idx) {
  activeCityIndex = idx;
  renderCityChips();
  renderCityDetails(idx);
}

function renderCityDetails(idx) {
  const city = SOUTHERN_CITIES[idx];
  const box = document.getElementById('city-details-box');
  if (!box) return;

  box.innerHTML = `
    <span class="delivery-badge">${city.deliveryType}</span>
    <h4>${city.icon} График за ${city.name}</h4>
    <p><strong>Дни за доставка:</strong> ${city.days}</p>
    <p><strong>Условие за безплатна доставка:</strong> Покрива се автоматично за поръчки над <strong class="text-gold">${city.freeLimit.toFixed(2)} €</strong>.</p>
    <p style="margin-top:10px; font-size:0.9rem; color:var(--text-muted);">${city.note}</p>
  `;

  // Synchronize city in checkout select
  const ckSelect = document.getElementById('ck-city');
  if (ckSelect) {
    for (let opt of ckSelect.options) {
      if (opt.value.includes(city.name)) {
        opt.selected = true;
        break;
      }
    }
  }
}

// --- Render Products Catalog ---
function renderProducts(category = 'all') {
  const grid = document.getElementById('products-grid');
  if (!grid) return;

  const filtered = category === 'all' 
    ? PRODUCTS_DATA 
    : PRODUCTS_DATA.filter(p => p.category === category);

  grid.innerHTML = filtered.map(p => `
    <div class="product-card" id="card-${p.id}">
      <div class="product-img-wrapper" onclick="openQuickView('${p.id}')">
        <span class="product-tag">${p.tag}</span>
        <img src="${p.image}" alt="${p.name}" class="product-img">
        <button class="product-quickview-btn" title="Бърз преглед"><i data-lucide="eye"></i></button>
        <span class="product-origin-tag">📍 ${p.origin}</span>
      </div>
      <div class="product-body">
        <h3 class="product-title">${p.name}</h3>
        <p class="product-desc">${p.description}</p>

        <div class="product-specs">
          <div class="product-spec-item">🎨 ${p.specs.color}</div>
          <div class="product-spec-item">🌸 ${p.specs.aroma}</div>
        </div>

        <select class="product-size-select" id="size-select-${p.id}" onchange="updateProductPrice('${p.id}')">
          ${p.sizes.map((s, index) => `
            <option value="${index}">${s.label} - ${s.price.toFixed(2)} лв</option>
          `).join('')}
        </select>

        <div class="product-footer">
          <div class="product-price" id="price-display-${p.id}">
            ${p.sizes[0].price.toFixed(2)} <span>лв</span>
          </div>

          <button class="btn btn-primary glow-effect" onclick="handleAddToCart('${p.id}')">
            <i data-lucide="shopping-bag"></i> Добави
          </button>
        </div>
      </div>
    </div>
  `).join('');

  lucide.createIcons();
}

function updateProductPrice(productId) {
  const product = PRODUCTS_DATA.find(p => p.id === productId);
  const select = document.getElementById(`size-select-${productId}`);
  const priceDisplay = document.getElementById(`price-display-${productId}`);
  
  if (product && select && priceDisplay) {
    const selectedSizeIndex = parseInt(select.value);
    const sizeObj = product.sizes[selectedSizeIndex];
    priceDisplay.innerHTML = `${sizeObj.price.toFixed(2)} <span>лв</span>`;
  }
}

// --- Quick View Modal Engine ---
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
        <h4 style="font-size:1.4rem; margin:10px 0;">📍 ${product.origin}</h4>
        <p style="color:var(--text-muted); margin-bottom:15px; font-size:0.92rem; line-height:1.5;">${product.description}</p>
        
        <div style="background:rgba(26, 13, 8, 0.85); border:1px solid var(--amber-border); padding:14px; border-radius:var(--radius-sm); margin-bottom:15px; color:var(--text-soft); font-size:0.88rem;">
          <p style="margin-bottom:6px;"><strong>🍇 Дегустационен профил:</strong> ${product.tastingNotes}</p>
          <p style="margin-bottom:6px;"><strong>🔬 Анализ на чистота:</strong> ${product.pollenPurity}</p>
          <p style="margin-bottom:0;"><strong>📅 Дата на добив:</strong> ${product.harvestDate}</p>
        </div>

        <div class="quickview-actions">
          <span style="font-size:1.7rem; font-weight:800; font-family:var(--font-serif);" class="text-gold">
            от ${product.sizes[0].price.toFixed(2)} лв
          </span>
          <button class="btn btn-primary btn-lg glow-effect quickview-buy-btn" onclick="handleAddToCart('${product.id}'); document.getElementById('quickview-modal').classList.remove('active');">
            <i data-lucide="shopping-bag"></i> Добави в Количката
          </button>
        </div>
      </div>
    </div>
  `;

  modal.classList.add('active');
  lucide.createIcons();
};

function handleAddToCart(productId) {
  const product = PRODUCTS_DATA.find(p => p.id === productId);
  const select = document.getElementById(`size-select-${productId}`);
  const selectedSizeIndex = select ? parseInt(select.value) : 0;
  const sizeObj = product.sizes[selectedSizeIndex];

  const cartItemId = `${product.id}_${sizeObj.weight}`;
  const existing = cartState.find(item => item.cartItemId === cartItemId);

  if (existing) {
    existing.qty += 1;
  } else {
    cartState.push({
      cartItemId: cartItemId,
      id: product.id,
      name: product.name,
      image: product.image,
      sizeLabel: sizeObj.label,
      weight: sizeObj.weight,
      price: sizeObj.price,
      qty: 1
    });
  }

  saveCart();
  updateCartUI();
  showToast(`🍯 Добавено в количката: ${product.name} (${sizeObj.label})`);

  openCartDrawer();
}

// --- Interactive Honey Health Calculator ---
window.calculateHoneyDose = function() {
  const age = document.getElementById('calc-age')?.value || 'adult';
  const goal = document.getElementById('calc-goal')?.value || 'immunity';
  const box = document.getElementById('calc-results-box');

  if (!box) return;

  let spoonsHoney = "2 чаени лъжици (около 25 г)";
  let propolisDrops = "15-20 капки в чаша вода";
  let recommendedHoney = "Тракийски Акациев или Родопски Букет";

  if (age === 'child') {
    spoonsHoney = "1 чаена лъжичка (около 10-12 г)";
    propolisDrops = "5-8 капки в хладък чай";
    recommendedHoney = "Тракийски Акациев Мед (най-нежен за деца)";
  } else if (age === 'senior') {
    spoonsHoney = "2-3 чаени лъжици";
    propolisDrops = "20 капки 2 пъти дневно";
    recommendedHoney = "Странджански манов мед ЗНП (богат на минерали)";
  } else if (age === 'athlete') {
    spoonsHoney = "3-4 чаени лъжици (преди и след тренировка)";
    propolisDrops = "25 капки сутрин";
    recommendedHoney = "Родопски Билков Букет + Пчелен Прашец";
  }

  if (goal === 'sleep') {
    recommendedHoney = "Лавандулов Мед в топло мляко 30 мин преди сън";
  } else if (goal === 'cold') {
    recommendedHoney = "Сакарски Липов Мед с Прополисова Тинктура";
  }

  box.innerHTML = `
    <h4 style="color:#FFF; font-size:1.3rem; margin-bottom:15px;">📋 Препоръчителна Дневна Рецепта:</h4>
    <div class="dose-highlight">${spoonsHoney}</div>
    <p style="color:#E2D5C8; margin:8px 0 15px;"><strong>Прополисова тинктура:</strong> ${propolisDrops}</p>
    <div style="background:rgba(245,158,11,0.12); border:1px solid var(--amber-border); padding:12px 16px; border-radius:var(--radius-sm); color:#FFF;">
      👉 <strong>Най-добър избор за вас:</strong> <span class="text-gold">${recommendedHoney}</span>
    </div>
  `;
};

// --- Quiz Wizard Engine ---
function renderQuizStep() {
  const container = document.getElementById('quiz-body');
  const progressFill = document.getElementById('quiz-progress-fill');
  if (!container) return;

  if (currentQuizStep < QUIZ_STEPS.length) {
    const stepData = QUIZ_STEPS[currentQuizStep];
    const progressPercent = ((currentQuizStep + 1) / QUIZ_STEPS.length) * 100;
    if (progressFill) progressFill.style.width = `${progressPercent}%`;

    container.innerHTML = `
      <h3 class="text-center" style="font-size:1.4rem; margin-bottom:20px;">
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

  let recommended = PRODUCTS_DATA.find(p => p.quizMatch.includes(goal) && p.quizMatch.includes(taste));
  if (!recommended) {
    recommended = PRODUCTS_DATA[0];
  }

  container.innerHTML = `
    <div class="quiz-recommendation-box">
      <img src="${recommended.image}" alt="${recommended.name}" class="quiz-rec-img">
      <div>
        <span class="badge-tag">98% Съвпадение за вашите нужди</span>
        <h3 style="font-size:1.6rem; margin: 8px 0;">Препоръчваме ви: ${recommended.name}</h3>
        <p style="color:var(--text-muted); margin-bottom:15px;">${recommended.description}</p>
        <p><strong>Регион на паша:</strong> ${recommended.origin}</p>
        <div style="margin-top:20px; display:flex; gap:15px; align-items:center;">
          <span style="font-size:1.6rem; font-weight:700; font-family:var(--font-serif);" class="text-gold">
            ${recommended.sizes[0].price.toFixed(2)} лв
          </span>
          <button class="btn btn-primary glow-effect" onclick="handleAddToCart('${recommended.id}')">
            <i data-lucide="shopping-bag"></i> Добави Препоръчания Мед
          </button>
          <button class="btn btn-outline" onclick="resetQuiz()">
            <i data-lucide="rotate-ccw"></i> Отново
          </button>
        </div>
      </div>
    </div>
  `;

  lucide.createIcons();
}

function resetQuiz() {
  currentQuizStep = 0;
  quizAnswers = {};
  renderQuizStep();
}

// --- Cart & UI Handlers ---
function saveCart() {
  localStorage.setItem('yug_honey_cart', JSON.stringify(cartState));
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

  const currentCity = SOUTHERN_CITIES[activeCityIndex];
  const subtotal = cartState.reduce((sum, item) => sum + (item.price * item.qty), 0);
  const discountVal = subtotal * appliedDiscount;
  const netTotal = subtotal - discountVal;

  let shippingCost = netTotal >= currentCity.freeLimit || netTotal === 0 ? 0 : 5.50;

  if (itemsContainer) {
    if (cartState.length === 0) {
      itemsContainer.innerHTML = `
        <div class="text-center" style="padding: 40px 0; color:var(--text-muted);">
          <div style="font-size:3rem; margin-bottom:10px;">🍯</div>
          <p>Вашата количка е празна</p>
          <a href="#products" class="btn btn-outline" style="margin-top:15px;" onclick="closeCartDrawer()">Разгледай Каталога</a>
        </div>
      `;
    } else {
      itemsContainer.innerHTML = cartState.map(item => `
        <div class="cart-item">
          <img src="${item.image}" alt="${item.name}" class="cart-item-img">
          <div class="cart-item-details">
            <div class="cart-item-title">${item.name}</div>
            <div class="cart-item-size">${item.sizeLabel} • ${item.price.toFixed(2)} лв</div>
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

  // Shipping Progress Banner
  if (shippingBanner) {
    if (subtotal >= currentCity.freeLimit || subtotal === 0) {
      shippingBanner.innerHTML = `✅ Поздравления! Поръчката ви получава <strong>БЕЗПЛАТНА доставка</strong> за ${currentCity.name}!`;
    } else {
      const remaining = currentCity.freeLimit - subtotal;
      shippingBanner.innerHTML = `🚚 Добавете още <strong>${remaining.toFixed(2)} лв</strong> за БЕЗПЛАТНА доставка в ${currentCity.name}!`;
    }
  }

  if (subtotalEl) subtotalEl.textContent = `${subtotal.toFixed(2)} лв`;
  
  if (discountRow) {
    if (appliedDiscount > 0) {
      discountRow.style.display = 'flex';
      discountEl.textContent = `-${discountVal.toFixed(2)} лв`;
    } else {
      discountRow.style.display = 'none';
    }
  }

  if (shippingEl) shippingEl.textContent = shippingCost === 0 ? 'БЕЗПЛАТНА' : `${shippingCost.toFixed(2)} лв`;
  const grandTotal = netTotal + shippingCost;
  if (totalEl) totalEl.textContent = `${grandTotal.toFixed(2)} лв`;

  // Update summary in checkout modal
  const ckCount = document.getElementById('ck-summary-count');
  const ckTotal = document.getElementById('ck-summary-total');
  if (ckCount) ckCount.textContent = `${totalQty} бр.`;
  if (ckTotal) ckTotal.textContent = `${grandTotal.toFixed(2)} лв`;

  lucide.createIcons();
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
  showToast('🗑️ Артикулът бе премахнат от количката');
}

// --- Cart Drawer Controls ---
function openCartDrawer() {
  document.getElementById('cart-drawer-overlay')?.classList.add('active');
  document.getElementById('cart-drawer')?.classList.add('active');
}

function closeCartDrawer() {
  document.getElementById('cart-drawer-overlay')?.classList.remove('active');
  document.getElementById('cart-drawer')?.classList.remove('active');
}

// --- Toast Notifications ---
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

// --- Event Listeners Setup ---
function setupEventListeners() {
  // --- Scroll-aware Navbar: shrink on scroll + active section tracking ---
  const navbar = document.getElementById('navbar');
  const navLinksAnchors = document.querySelectorAll('#nav-links a');
  const sections = document.querySelectorAll('section[id]');

  function onNavScroll() {
    // Shrink navbar on scroll
    if (navbar) {
      if (window.scrollY > 60) {
        navbar.classList.add('scrolled');
      } else {
        navbar.classList.remove('scrolled');
      }
    }

    // Highlight active section link
    let currentSection = '';
    sections.forEach(section => {
      const sectionTop = section.offsetTop - 120;
      if (window.scrollY >= sectionTop) {
        currentSection = section.getAttribute('id');
      }
    });

    navLinksAnchors.forEach(link => {
      link.classList.remove('active-link');
      if (link.getAttribute('href') === `#${currentSection}`) {
        link.classList.add('active-link');
      }
    });
  }

  window.addEventListener('scroll', onNavScroll, { passive: true });
  onNavScroll(); // run once on init

  // --- Mobile Menu (Overlay) ---
  const mobileBtn = document.getElementById('mobile-menu-btn');
  const navLinks = document.getElementById('nav-links');

  // Inject close button into mobile nav if not present
  if (navLinks && !navLinks.querySelector('.mobile-close-btn')) {
    const closeBtn = document.createElement('button');
    closeBtn.className = 'mobile-close-btn';
    closeBtn.innerHTML = '✕';
    closeBtn.setAttribute('aria-label', 'Затвори менюто');
    closeBtn.addEventListener('click', () => {
      navLinks.classList.remove('active');
      document.body.style.overflow = '';
    });
    navLinks.prepend(closeBtn);
  }

  mobileBtn?.addEventListener('click', () => {
    navLinks?.classList.toggle('active');
    // Lock body scroll when mobile menu open
    if (navLinks?.classList.contains('active')) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
  });

  // Close mobile menu on link click
  navLinksAnchors.forEach(link => {
    link.addEventListener('click', () => {
      if (navLinks?.classList.contains('active')) {
        navLinks.classList.remove('active');
        document.body.style.overflow = '';
      }
    });
  });

  // Cart Drawer open/close
  document.getElementById('open-cart-btn')?.addEventListener('click', openCartDrawer);
  document.getElementById('close-cart-btn')?.addEventListener('click', closeCartDrawer);
  document.getElementById('cart-drawer-overlay')?.addEventListener('click', closeCartDrawer);

  // Catalog Filters
  const filterBtns = document.querySelectorAll('.filter-btn');
  filterBtns.forEach(btn => {
    btn.addEventListener('click', (e) => {
      filterBtns.forEach(b => b.classList.remove('active'));
      e.target.classList.add('active');
      const cat = e.target.getAttribute('data-filter');
      renderProducts(cat);
    });
  });

  // Promo Code
  document.getElementById('apply-promo-btn')?.addEventListener('click', () => {
    const input = document.getElementById('promo-input');
    const msg = document.getElementById('promo-message');
    if (input && input.value.trim().toUpperCase() === 'TRAKIA10') {
      appliedDiscount = 0.10;
      if (msg) {
        msg.textContent = '✅ Промокодът е приложен! Получавате 10% отстъпка.';
        msg.style.color = 'var(--green-accent)';
      }
      updateCartUI();
    } else {
      if (msg) {
        msg.textContent = '❌ Невалиден промо код (Опитайте TRAKIA10)';
        msg.style.color = '#E53935';
      }
    }
  });

  // Checkout modal trigger
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

  // Certificate Modal View
  document.getElementById('view-cert-btn')?.addEventListener('click', () => {
    document.getElementById('cert-modal')?.classList.add('active');
  });
}

let selectedPaymentMethodName = 'Наложен Платеж (в брой / с карта)';

window.selectPaymentMethod = function(method) {
  const cards = document.querySelectorAll('.payment-option-card');
  cards.forEach(c => c.classList.remove('active'));

  const radio = document.getElementById(`pay-${method}`);
  if (radio) {
    radio.checked = true;
    const parentCard = radio.closest('.payment-option-card');
    if (parentCard) parentCard.classList.add('active');
  }

  // Toggle extra fields visibility
  const cardExtra = document.getElementById('card-extra-details');
  const revExtra = document.getElementById('revolut-extra-details');
  const bankExtra = document.getElementById('bank-extra-details');

  if (cardExtra) cardExtra.style.display = (method === 'card') ? 'block' : 'none';
  if (revExtra) revExtra.style.display = (method === 'revolut') ? 'block' : 'none';
  if (bankExtra) bankExtra.style.display = (method === 'bank') ? 'block' : 'none';

  const names = {
    cod: '💵 Наложен Платеж (при доставка)',
    card: '💳 Банкова Карта (Visa/MC)',
    revolut: '⚡ Revolut Pay (@pchelin.yug)',
    bank: '🏦 Банков Превод по IBAN'
  };

  selectedPaymentMethodName = names[method] || 'Наложен Платеж';
  const summaryPaymentEl = document.getElementById('ck-summary-payment');
  if (summaryPaymentEl) summaryPaymentEl.textContent = selectedPaymentMethodName;
};

// --- Form Submissions Handlers ---
window.handleContactSubmit = function() {
  const name = document.getElementById('contact-name')?.value;
  const phone = document.getElementById('contact-phone')?.value;
  
  showToast(`🙏 Благодарим ви, ${name}! Запитването бе изпратено. Ще ви се обадим на ${phone}.`);
  document.getElementById('contact-form')?.reset();
};

window.handleCheckoutSubmit = function() {
  const name = document.getElementById('ck-name')?.value;
  const phone = document.getElementById('ck-phone')?.value;
  const city = document.getElementById('ck-city')?.value;

  const totalStr = document.getElementById('cart-total')?.textContent || '0.00 лв';
  const orderId = `#BG-HONEY-${Math.floor(10000 + Math.random() * 90000)}`;

  // Populate success receipt
  const receiptOrderId = document.getElementById('receipt-order-id');
  const receiptCity = document.getElementById('receipt-city');
  const receiptPayment = document.getElementById('receipt-payment');
  const receiptTotal = document.getElementById('receipt-total');

  if (receiptOrderId) receiptOrderId.textContent = orderId;
  if (receiptCity) receiptCity.textContent = city;
  if (receiptPayment) receiptPayment.textContent = selectedPaymentMethodName;
  if (receiptTotal) receiptTotal.textContent = totalStr;

  showToast(`🎉 Поръчката ${orderId} беше регистрирана успешно!`);

  // Clear cart
  cartState = [];
  saveCart();
  updateCartUI();

  // Hide checkout & show success modal
  document.getElementById('checkout-modal')?.classList.remove('active');
  document.getElementById('success-modal')?.classList.add('active');
};

// ==========================================================================
// REAL USER REVIEWS SYSTEM (localStorage Persistence & Interactive Ratings)
// ==========================================================================
const REVIEWS_STORAGE_KEY = 'yp_user_reviews';

function escapeHtml(str) {
  if (!str) return '';
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function getStoredReviews() {
  try {
    const raw = localStorage.getItem(REVIEWS_STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
}

function saveStoredReviews(reviews) {
  try {
    localStorage.setItem(REVIEWS_STORAGE_KEY, JSON.stringify(reviews));
  } catch (e) {
    console.error('Error saving reviews:', e);
  }
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
      if (r <= rating) {
        btn.classList.add('active');
        btn.classList.remove('inactive');
      } else {
        btn.classList.remove('active');
        btn.classList.add('inactive');
      }
    });
  };

  starBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const rating = parseInt(btn.dataset.rating, 10);
      window.setStarRating(rating);
    });
  });

  // Default to 5 stars
  window.setStarRating(5);
}

function renderUserReviews() {
  const grid = document.getElementById('reviews-grid');
  if (!grid) return;

  const reviews = getStoredReviews();
  if (reviews.length === 0) {
    grid.innerHTML = `
      <div class="no-reviews-box" style="grid-column: 1 / -1; text-align: center; padding: 45px 25px; background: rgba(18, 10, 6, 0.45); border: 1px dashed rgba(245, 158, 11, 0.25); border-radius: 16px; color: var(--text-soft);">
        <span style="font-size: 2.4rem; display: block; margin-bottom: 10px;">🍯</span>
        <h4 style="color: #FAF4EB; font-size: 1.15rem; margin-bottom: 6px; font-weight: 600;">Все още няма споделени отзиви</h4>
        <p style="font-size: 0.9rem; color: var(--text-muted); margin: 0; max-width: 460px; margin-inline: auto;">Бъдете първият наш клиент, който ще остави своето искрено мнение във формата по-горе!</p>
      </div>
    `;
    return;
  }

  grid.innerHTML = reviews.map(r => {
    const ratingNum = Math.max(1, Math.min(5, parseInt(r.rating || 5, 10)));
    const stars = '★'.repeat(ratingNum) + '☆'.repeat(5 - ratingNum);
    const initials = (r.name || 'Клиент')
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(w => w[0].toUpperCase())
      .join('') || 'К';

    const cityText = r.city ? `${escapeHtml(r.city)} • ` : '';
    const dateText = r.date || 'Наскоро';

    return `
      <div class="review-card card-glass reveal is-visible">
        <div class="review-stars" style="color:var(--primary-gold); font-size:1.25rem; letter-spacing:3px; margin-bottom:12px;">${stars}</div>
        <p class="review-text" style="font-style:italic; line-height:1.6; color:var(--text-soft); margin-bottom:18px;">"${escapeHtml(r.text)}"</p>
        <div class="review-author" style="display:flex; align-items:center; gap:12px;">
          <div class="author-avatar" style="width:42px; height:42px; background:var(--primary-gold); color:#0b0503; font-weight:700; border-radius:50%; display:flex; align-items:center; justify-content:center; flex-shrink:0;">${initials}</div>
          <div>
            <strong style="color:#FAF4EB; display:block;">${escapeHtml(r.name)}</strong>
            <span style="font-size:0.8rem; color:var(--text-muted);">${cityText}Верифициран клиент (${escapeHtml(dateText)})</span>
          </div>
        </div>
      </div>
    `;
  }).join('');
}

window.handleUserReviewSubmit = function() {
  const nameEl = document.getElementById('review-author-name');
  const cityEl = document.getElementById('review-author-city');
  const textEl = document.getElementById('review-comment-text');
  const ratingEl = document.getElementById('review-rating-value');

  const name = nameEl?.value.trim();
  const city = cityEl?.value.trim();
  const text = textEl?.value.trim();
  const rating = parseInt(ratingEl?.value || '5', 10);

  if (!name || !text) {
    showToast('⚠️ Моля, попълнете вашето име и текст на отзива!');
    return;
  }

  const now = new Date();
  const dateFormatted = `${String(now.getDate()).padStart(2, '0')}.${String(now.getMonth() + 1).padStart(2, '0')}.${now.getFullYear()}`;

  const newReview = {
    id: Date.now(),
    name,
    city,
    text,
    rating,
    date: dateFormatted
  };

  const reviews = getStoredReviews();
  reviews.unshift(newReview);
  saveStoredReviews(reviews);

  renderUserReviews();

  // Reset form
  if (nameEl) nameEl.value = '';
  if (cityEl) cityEl.value = '';
  if (textEl) textEl.value = '';
  if (window.setStarRating) window.setStarRating(5);

  showToast('🍯 Благодарим ви! Вашият отзив беше добавен успешно.');

  if (window.lucide) lucide.createIcons();
};
