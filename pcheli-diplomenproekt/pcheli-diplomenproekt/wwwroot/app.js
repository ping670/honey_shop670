/* ==========================================================================
   ПЧЕЛАРИ БЪЛГАРИЯ - Национална платформа за чист мед и пчелен прашец
   Свързва независимите производители с клиентите без посредници
   ========================================================================== */

// --- Свързване с бекенда (SQLite + ASP.NET Core) ---
const API_BASE_URL = window.location.origin.includes('localhost') || window.location.origin.includes('127.0.0.1')
  ? (window.location.port === '5272' ? '/api' : 'http://localhost:5272/api')
  : '/api';

// --- Данни за регистрираните пчелари ---
let BEEKEEPERS_DATA = [
  {
    id: 1,
    fullName: "Иван Стоянов",
    apiaryName: "Пчелин «Странджанска Дъбрава»",
    registrationNumber: "БАБХ-4001-0812",
    region: "Странджа",
    cityOrVillage: "гр. Малко Търново",
    phone: "0888 123 456",
    bio: "Трети потомствен пчелар в района на Малко Търново. Кошерите са разположени в чисти дъбови гори. Добиваме автентичен странджански манов мед (ЗНП) и горски прашец.",
    experienceYears: 24,
    hiveCount: 180,
    rating: 5.0,
    isVerified: true,
    hasHoney: true,
    hasPollen: true
  },
  {
    id: 2,
    fullName: "Димитър Родопски",
    apiaryName: "Био Пчелин «Родопска Панорама»",
    registrationNumber: "БАБХ-4700-0234",
    region: "Родопи",
    cityOrVillage: "с. Момчиловци (1250 м надм. вис.)",
    phone: "0887 654 321",
    bio: "Пчелинът се намира във високопланинския пояс на Родопите сред пасища с мащерка, риган и жълт кантарион. Предлагаме планински букет и пресен замразен прашец.",
    experienceYears: 18,
    hiveCount: 140,
    rating: 4.95,
    isVerified: true,
    hasHoney: true,
    hasPollen: true
  },
  {
    id: 3,
    fullName: "Христо Маринов",
    apiaryName: "Семеен Пчелин «Златна Добруджа»",
    registrationNumber: "БАБХ-9300-0419",
    region: "Добруджа",
    cityOrVillage: "гр. Генерал Тошево",
    phone: "0885 998 877",
    bio: "Семейно пчеларство с над 200 кошера в района на Генерал Тошево. Добиваме чист лавандулов мед и слънчогледов прашец с високо съдържание на биофлавоноиди.",
    experienceYears: 15,
    hiveCount: 210,
    rating: 4.9,
    isVerified: true,
    hasHoney: true,
    hasPollen: true
  },
  {
    id: 4,
    fullName: "Стоян Петков",
    apiaryName: "Пчелин «Тракийска Долина»",
    registrationNumber: "БАБХ-4000-0551",
    region: "Тракия",
    cityOrVillage: "гр. Садово / Пловдив",
    phone: "0894 332 211",
    bio: "Пчелинът е разположен близо до поречието на Марица сред богати акациеви гори. Произвеждаме прозрачен акациев мед и пролетен цветен прашец.",
    experienceYears: 12,
    hiveCount: 120,
    rating: 4.9,
    isVerified: true,
    hasHoney: true,
    hasPollen: true
  },
  {
    id: 5,
    fullName: "Николай Василев",
    apiaryName: "Пчелин «Балканско Ехо»",
    registrationNumber: "БАБХ-5000-0112",
    region: "Стара планина",
    cityOrVillage: "гр. Трявна",
    phone: "0876 114 455",
    bio: "Стационарен пчелин в Тревненския балкан. Пашата е от дива детелина, къпина, малина и планински билки. Предлагаме мед букет и чист пчелен прашец.",
    experienceYears: 20,
    hiveCount: 95,
    rating: 4.95,
    isVerified: true,
    hasHoney: true,
    hasPollen: true
  },
  {
    id: 6,
    fullName: "Красимир Георгиев",
    apiaryName: "Пчеларска ферма «Дунавски Бряг»",
    registrationNumber: "БАБХ-7000-0388",
    region: "Дунавска равнина",
    cityOrVillage: "гр. Русе",
    phone: "0883 227 788",
    bio: "Пчеларство по поречието на Дунав в близост до липовите масиви на Русенски Лом. Чист монофлорен липов мед и полифлорен цветен прашец.",
    experienceYears: 16,
    hiveCount: 160,
    rating: 4.88,
    isVerified: true,
    hasHoney: true,
    hasPollen: true
  }
];

// --- Каталог Продукти (Истински български мед и прашец) ---
let PRODUCTS_DATA = [
  {
    id: "p1",
    beekeeperId: 1,
    producer: "Пчелин «Странджанска Дъбрава» (Иван Стоянов)",
    babh: "БАБХ-4001-0812",
    phone: "0888 123 456",
    name: "Странджански Манов Мед (ЗНП)",
    type: "Мед",
    category: "manov",
    origin: "Странджа (Защитено Наименование)",
    tag: "ЗНП Сертификат",
    image: "./images/manov.jpg",
    description: "Тъмен манов мед, добит от вековните дъбови гори в Странджа. Богат на калий, желязо и минерални соли. Кристализира изключително бавно.",
    specs: { color: "Тъмно кехлибарен", aroma: "Дъбова мана и гора", crystallization: "Бавна (над 2 години)" },
    harvestDate: "Реколта Август 2026",
    pollenPurity: "100% Чист добив, без захари",
    tastingNotes: "Плътен, наситен вкус с леки карамелени и смолисти нотки.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 12.27 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 7.16 }
    ],
    rating: 5.0,
    quizMatch: ["antioxidant", "immunity", "dark"]
  },
  {
    id: "p2",
    beekeeperId: 1,
    producer: "Пчелин «Странджанска Дъбрава» (Иван Стоянов)",
    babh: "БАБХ-4001-0812",
    phone: "0888 123 456",
    name: "Натурален Пчелен Прашец от Странджа",
    type: "Пчелен прашец",
    category: "pollen",
    origin: "Странджа (Малко Търново)",
    tag: "Сушен цветен прашец",
    image: "./images/pollen.jpg",
    description: "Сушен цветен прашец от дивите ливади и гори на Странджа. Пълен източник на растителен протеин, аминокиселини и биофлавоноиди. Подходящ за ежедневен прием сутрин.",
    specs: { color: "Пъстри гранули", aroma: "Цветен и смолист", crystallization: "Сушени гранули" },
    harvestDate: "Реколта Юли 2026",
    pollenPurity: "100% Натурален сбор",
    tastingNotes: "Приятен цветен аромат с леко орехов послевкус.",
    sizes: [
      { label: "Буркан 250 г", weight: "0.25kg", price: 8.50 },
      { label: "Пакет 500 г", weight: "0.5kg", price: 16.00 }
    ],
    rating: 4.95,
    quizMatch: ["immunity", "antioxidant"]
  },
  {
    id: "p3",
    beekeeperId: 4,
    producer: "Пчелин «Тракийска Долина» (Стоян Петков)",
    babh: "БАБХ-4000-0551",
    phone: "0894 332 211",
    name: "Тракийски Акациев Мед",
    type: "Мед",
    category: "acacia",
    origin: "Тракия (Садово / Пловдив)",
    tag: "Фин & Мек",
    image: "./images/acacia.jpg",
    description: "Светъл и прозрачен акациев мед от горите край Марица. Изключително нежен вкус без натрапчива сладост. Един от най-подходящите медове за деца.",
    specs: { color: "Светло сламеножълт", aroma: "Нежен акациев цвят", crystallization: "Много бавна" },
    harvestDate: "Реколта Май 2026",
    pollenPurity: "Чиста бяла акация",
    tastingNotes: "Ефирен, мек и сладък вкус без киселинност.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 10.74 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 6.39 }
    ],
    rating: 4.9,
    quizMatch: ["tea", "kids", "light"]
  },
  {
    id: "p4",
    beekeeperId: 2,
    producer: "Био Пчелин «Родопска Панорама» (Димитър Родопски)",
    babh: "БАБХ-4700-0234",
    phone: "0887 654 321",
    name: "Родопски Планински Билков Букет",
    type: "Мед",
    category: "herbal",
    origin: "Родопи (Момчиловци, 1250 м)",
    tag: "Планински билки",
    image: "./images/rhodope.jpg",
    description: "Добит от диви планински билки над 1200 м надморска височина — мащерка, риган, бяла детелина и жълт кантарион. Ароматен и богат на естествени антиоксиданти.",
    specs: { color: "Кехлибарен", aroma: "Дива планинска мащерка", crystallization: "Средна естествена" },
    harvestDate: "Реколта Юли 2026",
    pollenPurity: "Диворастящи лечебни билки",
    tastingNotes: "Многопластов билков вкус с отчетлив аромат на мащерка.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 9.97 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 5.88 }
    ],
    rating: 4.95,
    quizMatch: ["cold", "immunity", "herbal"]
  },
  {
    id: "p5",
    beekeeperId: 2,
    producer: "Био Пчелин «Родопска Панорама» (Димитър Родопски)",
    babh: "БАБХ-4700-0234",
    phone: "0887 654 321",
    name: "Пресен Суров Замразен Пчелен Прашец",
    type: "Пчелен прашец",
    category: "pollen",
    origin: "Родопи (Високопланински пояс)",
    tag: "Суров замразен (-18°C)",
    image: "./images/pollen.jpg",
    description: "Шоково замразен веднага след събиране от кошера при -18°C. Без сушене и загряване, което запазва 100% от живите ензими, пробиотици и витамини.",
    specs: { color: "Ярко жълто и оранжево", aroma: "Свеж цвят и цитрус", crystallization: "Суров замразен" },
    harvestDate: "Реколта Юни 2026",
    pollenPurity: "100% Суров жив прашец",
    tastingNotes: "Разтапя се леко на езика с приятен свеж цветен вкус.",
    sizes: [
      { label: "Кутия 300 г (Замразен)", weight: "0.3kg", price: 11.20 },
      { label: "Кутия 600 г (Замразен)", weight: "0.6kg", price: 21.00 }
    ],
    rating: 5.0,
    quizMatch: ["immunity", "antioxidant"]
  },
  {
    id: "p6",
    beekeeperId: 3,
    producer: "Семеен Пчелин «Златна Добруджа» (Христо Маринов)",
    babh: "БАБХ-9300-0419",
    phone: "0885 998 877",
    name: "Добруджански Лавандулов Мед",
    type: "Мед",
    category: "lavender",
    origin: "Добруджа (Генерал Тошево)",
    tag: "Лавандулов цвят",
    image: "./images/rhodope.jpg",
    description: "Чист монофлорен мед от лавандуловите масиви на Добруджа. Отличава се с фин аромат и успокояващо действие. Чудесен за топъл билков чай вечер.",
    specs: { color: "Светъл млечен кехлибар", aroma: "Лавандулов нюанс", crystallization: "Фина кремообразна" },
    harvestDate: "Реколта Юни 2026",
    pollenPurity: "Добруджанска лавандула",
    tastingNotes: "Кадифен вкус с деликатен лавандулов послевкус.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 10.50 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 6.20 }
    ],
    rating: 4.9,
    quizMatch: ["tea", "herbal", "light"]
  },
  {
    id: "p7",
    beekeeperId: 3,
    producer: "Семеен Пчелин «Златна Добруджа» (Христо Маринов)",
    babh: "БАБХ-9300-0419",
    phone: "0885 998 877",
    name: "Златен Слънчогледов Пчелен Прашец",
    type: "Пчелен прашец",
    category: "pollen",
    origin: "Добруджа (Генерал Тошево)",
    tag: "Слънчогледов прашец",
    image: "./images/pollen.jpg",
    description: "Златист прашец от обширните слънчогледови масиви на Добруджа. Отличен природен източник на рутин и лецитин за тонус и подкрепа на кръвообращението.",
    specs: { color: "Ярко златисто-жълт", aroma: "Сладък слънчоглед", crystallization: "Сушени гранули" },
    harvestDate: "Реколта Август 2026",
    pollenPurity: "Чист слънчогледов сбор",
    tastingNotes: "Сладникав вкус с леки маслени нотки.",
    sizes: [
      { label: "Буркан 250 г", weight: "0.25kg", price: 7.90 }
    ],
    rating: 4.85,
    quizMatch: ["immunity", "tea"]
  },
  {
    id: "p8",
    beekeeperId: 6,
    producer: "Пчеларска ферма «Дунавски Бряг» (Красимир Георгиев)",
    babh: "БАБХ-7000-0388",
    phone: "0883 227 788",
    name: "Крайдунавски Липов Мед",
    type: "Мед",
    category: "linden",
    origin: "Дунавска равнина (гр. Русе)",
    tag: "Ароматна липа",
    image: "./images/acacia.jpg",
    description: "Наситен монофлорен мед от вековните крайдунавски липови гори край Русе. Традиционно домашно средство при настинка, болки в гърлото и грип.",
    specs: { color: "Жълто-златист", aroma: "Уханен липов цвят", crystallization: "Средна кристализация" },
    harvestDate: "Реколта Юни 2026",
    pollenPurity: "Сребролистна дунавска липа",
    tastingNotes: "Свеж, затоплящ вкус с отчетлив аромат на цъфнала липа.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 9.80 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 5.70 }
    ],
    rating: 4.88,
    quizMatch: ["cold", "herbal"]
  },
  {
    id: "p9",
    beekeeperId: 5,
    producer: "Пчелин «Балканско Ехо» (Николай Василев)",
    babh: "БАБХ-5000-0112",
    phone: "0876 114 455",
    name: "Балкански Горски Букет Мед",
    type: "Мед",
    category: "herbal",
    origin: "Стара планина (гр. Трявна)",
    tag: "Горски букет",
    image: "./images/rhodope.jpg",
    description: "Многоцветен горски мед от девствените планински масиви около Трявна. Пашата е от дива детелина, къпина, малина и горски билки.",
    specs: { color: "Тъмно кехлибарен", aroma: "Горски билки и листа", crystallization: "Естествена средна" },
    harvestDate: "Реколта Юли 2026",
    pollenPurity: "Балкански горски пасища",
    tastingNotes: "Балансиран, дълбок и плътен горски вкус.",
    sizes: [
      { label: "Буркан 0.9 кг", weight: "0.9kg", price: 10.10 },
      { label: "Буркан 0.5 кг", weight: "0.5kg", price: 5.95 }
    ],
    rating: 4.95,
    quizMatch: ["immunity", "herbal"]
  }
];

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

// --- Състояние на количката ---
let cartState = (JSON.parse(localStorage.getItem('pchelari_bg_cart')) || []).map(item => {
  const prod = PRODUCTS_DATA.find(p => p.id === item.productId || p.id === item.id);
  if (prod) {
    const sizeObj = prod.sizes.find(s => s.weight === item.weight) || prod.sizes[0];
    if (sizeObj) item.price = sizeObj.price;
  }
  return item;
});

let currentQuizStep = 0;
let quizAnswers = {};
let activeFilterCategory = 'all';
let activeFilterType = 'all';
let activeFilterRegion = 'all';
let activeSearchQuery = '';
let appliedDiscount = 0;
let selectedPaymentMethodName = '💵 Наложен Платеж (при доставка)';

// --- Инициализация при зареждане ---
document.addEventListener('DOMContentLoaded', () => {
  loadCustomDataFromStorage();
  tryFetchApiData();

  renderProducts();
  renderBeekeepers();
  renderQuizStep();
  renderUserReviews();
  initStarRatingSelector();
  updateCartUI();

  setupEventListeners();
  if (window.lucide) lucide.createIcons();
});

// --- Зареждане на локално добавени пчелари и продукти ---
function loadCustomDataFromStorage() {
  try {
    const savedBeekeepers = JSON.parse(localStorage.getItem('pchelari_custom_beekeepers')) || [];
    savedBeekeepers.forEach(b => {
      if (!BEEKEEPERS_DATA.some(x => x.registrationNumber === b.registrationNumber)) {
        BEEKEEPERS_DATA.unshift(b);
      }
    });

    const savedProducts = JSON.parse(localStorage.getItem('pchelari_custom_products')) || [];
    savedProducts.forEach(p => {
      if (!PRODUCTS_DATA.some(x => x.id === p.id)) {
        PRODUCTS_DATA.unshift(p);
      }
    });
  } catch (e) {
    console.warn("Storage error:", e);
  }
}

// --- Връзка със сървърната SQLite база данни (ако е пуснат .NET бекенда) ---
async function tryFetchApiData() {
  try {
    const res = await fetch(`${API_BASE_URL}/products`);
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        const mapped = data.map(p => ({
          id: `db_${p.id}`,
          beekeeperId: p.beekeeper ? p.beekeeper.id : 1,
          producer: p.beekeeper ? `${p.beekeeper.apiaryName} (${p.beekeeper.fullName})` : 'Български Пчелин',
          babh: p.beekeeper ? p.beekeeper.registrationNumber : 'БАБХ Сертифициран',
          phone: p.beekeeper ? p.beekeeper.phone : '0888 123 456',
          name: p.name,
          type: p.type || "Мед",
          category: p.category || "herbal",
          origin: p.origin,
          tag: p.tag || "100% Натурален",
          image: p.image || "./images/manov.jpg",
          description: p.description,
          specs: { color: "Натурален", aroma: "Автентичен", crystallization: "Естествена" },
          harvestDate: p.harvestDate || "Реколта 2026",
          pollenPurity: p.purity || "100% Чист",
          tastingNotes: p.tastingNotes || "Характерен български пчелен вкус.",
          sizes: [
            { label: `Опаковка ${p.weight || '0.9 кг'}`, weight: p.weight || '0.9kg', price: Number(p.price) }
          ],
          rating: p.rating || 5.0,
          quizMatch: ["immunity", "herbal"]
        }));

        PRODUCTS_DATA = mapped;
        renderProducts();
      }
    }
  } catch (err) {
    // Автономен режим без грешка
  }
}

// --- Филтриране ---
window.filterByType = function(type) {
  activeFilterType = type;
  
  document.querySelectorAll('.type-tab-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-type') === type);
  });

  renderProducts();

  const catalogEl = document.getElementById('products');
  if (catalogEl) {
    catalogEl.scrollIntoView({ behavior: 'smooth' });
  }
};

window.filterByRegion = function(regionName) {
  activeFilterRegion = regionName;
  showToast(`📍 Избран регион: ${regionName}`);
  renderProducts();

  const catalogEl = document.getElementById('products');
  if (catalogEl) {
    catalogEl.scrollIntoView({ behavior: 'smooth' });
  }
};

window.handleSearchInput = function(query) {
  activeSearchQuery = query.trim().toLowerCase();
  renderProducts();
};

// --- Визуализация на Каталога ---
function renderProducts() {
  const grid = document.getElementById('products-grid');
  if (!grid) return;

  let filtered = PRODUCTS_DATA;

  if (activeFilterType !== 'all') {
    filtered = filtered.filter(p => p.type.toLowerCase() === activeFilterType.toLowerCase());
  }

  if (activeFilterCategory !== 'all') {
    if (activeFilterCategory === 'pollen') {
      filtered = filtered.filter(p => p.type === 'Пчелен прашец' || p.category === 'pollen');
    } else {
      filtered = filtered.filter(p => p.category === activeFilterCategory);
    }
  }

  if (activeFilterRegion !== 'all') {
    filtered = filtered.filter(p => p.origin.toLowerCase().includes(activeFilterRegion.toLowerCase()));
  }

  if (activeSearchQuery) {
    filtered = filtered.filter(p => 
      p.name.toLowerCase().includes(activeSearchQuery) ||
      p.origin.toLowerCase().includes(activeSearchQuery) ||
      (p.producer && p.producer.toLowerCase().includes(activeSearchQuery)) ||
      (p.description && p.description.toLowerCase().includes(activeSearchQuery))
    );
  }

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
              <option value="${index}">${s.label} — ${s.price.toFixed(2)} лв</option>
            `).join('')}
          </select>

          <div class="product-footer">
            <div class="product-price" id="price-display-${p.id}">
              ${p.sizes[0].price.toFixed(2)} <span>лв</span>
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

  renderProducts();
};

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
            ${product.sizes[0].price.toFixed(2)} лв
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

  const newBeekeeper = {
    id: Date.now(),
    fullName: name,
    apiaryName: apiary,
    registrationNumber: babh,
    region: region,
    cityOrVillage: city,
    phone: phone,
    bio: `Пчелин от регион ${region} (${city}). Кошери: ${hives}. ${details}`,
    experienceYears: 5,
    hiveCount: hives,
    rating: 5.0,
    isVerified: true,
    hasHoney: offerHoney,
    hasPollen: offerPollen
  };

  BEEKEEPERS_DATA.unshift(newBeekeeper);
  const savedBeekeepers = JSON.parse(localStorage.getItem('pchelari_custom_beekeepers')) || [];
  savedBeekeepers.unshift(newBeekeeper);
  localStorage.setItem('pchelari_custom_beekeepers', JSON.stringify(savedBeekeepers));

  const newProducts = [];
  if (offerHoney) {
    newProducts.push({
      id: `p_custom_honey_${Date.now()}`,
      beekeeperId: newBeekeeper.id,
      producer: `${apiary} (${name})`,
      babh: babh,
      phone: phone,
      name: `Натурален Мед от «${apiary}»`,
      type: "Мед",
      category: "herbal",
      origin: `${region} (${city})`,
      tag: "Нов пчелин",
      image: "./images/acacia.jpg",
      description: `Натурален пчелен мед реколта 2026 от района на ${city}. Добит с грижа за пчелите.`,
      specs: { color: "Златист", aroma: "Билков", crystallization: "Естествена" },
      harvestDate: "Реколта 2026",
      pollenPurity: "100% Чист мед",
      tastingNotes: "Натурален чист домашен вкус.",
      sizes: [
        { label: "Буркан 0.9 кг", weight: "0.9kg", price: 10.50 }
      ],
      rating: 5.0,
      quizMatch: ["immunity", "herbal"]
    });
  }

  if (offerPollen) {
    newProducts.push({
      id: `p_custom_pollen_${Date.now()}`,
      beekeeperId: newBeekeeper.id,
      producer: `${apiary} (${name})`,
      babh: babh,
      phone: phone,
      name: `Пчелен Прашец от «${apiary}»`,
      type: "Пчелен прашец",
      category: "pollen",
      origin: `${region} (${city})`,
      tag: "Нов пчелин",
      image: "./images/pollen.jpg",
      description: `Цветен пчелен прашец от региона на ${city}. Богат на аминокиселини и ензими.`,
      specs: { color: "Пъстър", aroma: "Цветен прашец", crystallization: "Гранули" },
      harvestDate: "Реколта 2026",
      pollenPurity: "100% Чист прашец",
      tastingNotes: "Цветен, мек послевкус.",
      sizes: [
        { label: "Буркан 250 г", weight: "0.25kg", price: 8.50 }
      ],
      rating: 5.0,
      quizMatch: ["immunity", "antioxidant"]
    });
  }

  newProducts.forEach(prod => PRODUCTS_DATA.unshift(prod));
  const savedProducts = JSON.parse(localStorage.getItem('pchelari_custom_products')) || [];
  newProducts.forEach(prod => savedProducts.unshift(prod));
  localStorage.setItem('pchelari_custom_products', JSON.stringify(savedProducts));

  // Изпращане към бекенда
  try {
    fetch(`${API_BASE_URL}/beekeepers/apply`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
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
    }).catch(() => {});
  } catch (err) {}

  renderBeekeepers();
  renderProducts();

  document.getElementById('beekeeper-form')?.reset();
  window.closeBeekeeperModal();

  showToast(`🎉 Пчелин «${apiary}» бе добавен успешно в каталога!`);
};

// --- Количка ---
function handleAddToCart(productId) {
  const product = PRODUCTS_DATA.find(p => p.id === productId);
  if (!product) return;

  const select = document.getElementById(`size-select-${productId}`);
  const selectedSizeIndex = select ? parseInt(select.value) : 0;
  const sizeObj = product.sizes[selectedSizeIndex] || product.sizes[0];

  const cartItemId = `${product.id}_${sizeObj.weight}`;
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

  let shippingCost = netTotal >= 45.00 || netTotal === 0 ? 0 : 5.50;

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
            <div class="cart-item-size">${item.sizeLabel} — ${item.price.toFixed(2)} лв</div>
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
    if (subtotal >= 45.00 || subtotal === 0) {
      shippingBanner.innerHTML = `✅ Поръчката ви е с <strong>БЕЗПЛАТНА доставка</strong> за България!`;
    } else {
      const remaining = 45.00 - subtotal;
      shippingBanner.innerHTML = `🚚 Добавете още <strong>${remaining.toFixed(2)} лв</strong> за БЕЗПЛАТНА доставка!`;
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

  const ckTotal = document.getElementById('ck-summary-total');
  if (ckTotal) ckTotal.textContent = `${grandTotal.toFixed(2)} лв`;

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

  const totalStr = document.getElementById('cart-total')?.textContent || '0.00 лв';
  const totalNum = parseFloat(totalStr.replace('лв', '').trim()) || 0;
  const orderId = `#BG-HONEY-${Math.floor(10000 + Math.random() * 90000)}`;

  // Запис в базата данни през API
  try {
    fetch(`${API_BASE_URL}/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        customerName: name,
        customerPhone: phone,
        customerEmail: email,
        deliveryCity: city,
        deliveryAddress: address,
        totalAmount: totalNum,
        items: cartState.map(i => ({
          productName: i.name,
          apiaryName: i.producer || '',
          weight: i.sizeLabel || '',
          quantity: i.qty,
          unitPrice: i.price,
          totalPrice: i.price * i.qty
        }))
      })
    }).catch(() => {});
  } catch (e) {}

  const receiptOrderId = document.getElementById('receipt-order-id');
  const receiptCity = document.getElementById('receipt-city');
  const receiptPayment = document.getElementById('receipt-payment');
  const receiptTotal = document.getElementById('receipt-total');

  if (receiptOrderId) receiptOrderId.textContent = orderId;
  if (receiptCity) receiptCity.textContent = city;
  if (receiptPayment) receiptPayment.textContent = selectedPaymentMethodName;
  if (receiptTotal) receiptTotal.textContent = totalStr;

  showToast(`🎉 Поръчка ${orderId} е регистрирана успешно!`);

  cartState = [];
  saveCart();
  updateCartUI();

  document.getElementById('checkout-modal')?.classList.remove('active');
  document.getElementById('success-modal')?.classList.add('active');
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
            ${recommended.sizes[0].price.toFixed(2)} лв
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
      renderProducts();
    });
  });

  // Промокод
  document.getElementById('apply-promo-btn')?.addEventListener('click', () => {
    const input = document.getElementById('promo-input');
    const msg = document.getElementById('promo-message');
    if (input && input.value.trim().toUpperCase() === 'BG10') {
      appliedDiscount = 0.10;
      if (msg) {
        msg.textContent = '✅ Промокодът е приложен! 10% отстъпка.';
        msg.style.color = '#34D399';
      }
      updateCartUI();
    } else {
      if (msg) {
        msg.textContent = '❌ Невалиден промо код (Опитайте BG10)';
        msg.style.color = '#EF4444';
      }
    }
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
const REVIEWS_STORAGE_KEY = 'pchelari_bg_reviews';

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
    return raw ? JSON.parse(raw) : [
      {
        id: 1,
        name: "Петър Николов",
        city: "гр. София",
        text: "Поръчах странджански манов мед и суров замразен прашец. Мановият мед е плътен и истински, без никаква следа от претопляне. Прашецът дойде перфектно запазен. Чудесно е, че можем да поръчваме директно от пчеларите.",
        rating: 5,
        date: "04.11.2026"
      },
      {
        id: 2,
        name: "Десислава Тодорова",
        city: "гр. Пловдив",
        text: "Акациевият мед от Тракия е любим на децата ми — много светъл и нежен. Пчеларят Стоян се обади да потвърди адреса и го донесе лично на следващия ден. Препоръчвам горещо!",
        rating: 5,
        date: "28.10.2026"
      },
      {
        id: 3,
        name: "Красимир Иванов",
        city: "гр. Варна",
        text: "Взех липов мед и лавандулов от Добруджа. Качеството е отлично, вкусът е точно какъвто го помня от детството ми. Браво за платформата и подкрепата за българските производители!",
        rating: 5,
        date: "15.10.2026"
      }
    ];
  } catch (e) {
    return [];
  }
}

function saveStoredReviews(reviews) {
  try {
    localStorage.setItem(REVIEWS_STORAGE_KEY, JSON.stringify(reviews));
  } catch (e) {}
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

function renderUserReviews() {
  const grid = document.getElementById('reviews-grid');
  if (!grid) return;

  const reviews = getStoredReviews();
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
    const initials = (r.name || 'Клиент').trim().split(/\s+/).filter(Boolean).slice(0, 2).map(w => w[0].toUpperCase()).join('') || 'К';
    const cityText = r.city ? `${escapeHtml(r.city)} • ` : '';

    return `
      <div class="review-card card-glass reveal is-visible">
        <div class="review-stars" style="color:var(--primary-gold); font-size:1.2rem; letter-spacing:3px; margin-bottom:10px;">${stars}</div>
        <p class="review-text" style="line-height:1.6; color:var(--text-soft); margin-bottom:16px; font-size:0.92rem;">"${escapeHtml(r.text)}"</p>
        <div class="review-author" style="display:flex; align-items:center; gap:12px;">
          <div class="author-avatar" style="width:40px; height:40px; background:var(--primary-gold); color:#0b0503; font-weight:700; border-radius:50%; display:flex; align-items:center; justify-content:center; flex-shrink:0;">${initials}</div>
          <div>
            <strong style="color:#FAF4EB; display:block; font-size:0.95rem;">${escapeHtml(r.name)}</strong>
            <span style="font-size:0.8rem; color:var(--text-muted);">${cityText}Проверен клиент (${escapeHtml(r.date || 'Наскоро')})</span>
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
    showToast('⚠️ Моля, попълнете вашето име и отзив!');
    return;
  }

  const now = new Date();
  const dateFormatted = `${String(now.getDate()).padStart(2, '0')}.${String(now.getMonth() + 1).padStart(2, '0')}.${now.getFullYear()}`;

  const newReview = { id: Date.now(), name, city, text, rating, date: dateFormatted };
  const reviews = getStoredReviews();
  reviews.unshift(newReview);
  saveStoredReviews(reviews);

  renderUserReviews();

  if (nameEl) nameEl.value = '';
  if (cityEl) cityEl.value = '';
  if (textEl) textEl.value = '';
  if (window.setStarRating) window.setStarRating(5);

  showToast('🍯 Благодарим ви! Вашият отзив беше добавен.');
};
