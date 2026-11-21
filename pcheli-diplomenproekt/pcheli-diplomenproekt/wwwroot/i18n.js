/* ==========================================================================
   Език (BG/EN) и тема (тъмна/светла) за всички страници.
   Българският текст остава в HTML-а; английският се подменя по точно съвпадение на текста.
   Имената и описанията на продуктите идват от API-то (NameEn/DescriptionEn).
   Дългите маркетингови абзаци на началната страница са само на български.
   ========================================================================== */
(function () {
  'use strict';

  const EN = {
    // --- меню и общи ---
    'Начало': 'Home', 'Карта': 'Map', 'Каталог': 'Catalog', 'Региони': 'Regions', 'Пчелен Прашец': 'Bee Pollen', 'Пчелари': 'Beekeepers',
    'Блог': 'Blog', 'Помощ при избор': 'Help me choose', 'Отзиви': 'Reviews', 'Проследи поръчка': 'Track order', 'За Пчелари': 'For Beekeepers',
    'ПЧЕЛАРИ БЪЛГАРИЯ': 'BEEKEEPERS BULGARIA', 'ОБЕДИНЕНИ БЪЛГАРСКИ ПЧЕЛИНИ': 'UNITED BULGARIAN APIARIES',
    '📞 0888 123 456 (Национална Линия)': '📞 0888 123 456 (National line)',
    'Магазин': 'Shop', 'Моят профил': 'My account',
    // --- начален екран и карта ---
    'Пчеларите на България,': 'The beekeepers of Bulgaria,', 'обединени': 'united', 'на едно място': 'in one place',
    '🍯 Разгледай мед и прашец': '🍯 Browse honey and pollen', '🐝 Добави своя пчелин': '🐝 Add your apiary', 'Научи повече': 'Learn more',
    'Пчелините в платформата': 'Apiaries on the platform', 'Карта на пчеларите': 'Map of the beekeepers',
    'Натиснете регион, за да видите неговия мед и прашец': 'Click a region to see its honey and pollen',
    'Странджа': 'Strandzha', 'Родопи': 'Rhodopes', 'Тракия': 'Thrace', 'Добруджа': 'Dobruja', 'Дунавска равнина': 'Danube Plain',
    'Стара планина': 'Balkan Mountains', 'Пирин и Рила': 'Pirin & Rila', 'региона': 'regions', 'кошера': 'hives', 'продукта': 'products',
    // --- каталог ---
    'Реколта 2026 • Директно от кошера': 'Harvest 2026 • Straight from the hive', 'Каталог: Пчелен Мед & Пчелен Прашец': 'Catalog: Bee Honey & Bee Pollen',
    'Всеки буркан мед и пакет прашец идва от сертифициран пчелин. Виждате точния регион, номера по БАБХ и телефон за директна връзка с пчеларя.':
      'Every jar of honey and bag of pollen comes from a certified apiary. You see the exact region, the BFSA registration number and a phone number to reach the beekeeper directly.',
    'Всички Продукти': 'All Products', '🍯 Натурален Пчелен Мед': '🍯 Natural Bee Honey', '🌼 Пчелен Прашец': '🌼 Bee Pollen', '🎁 Подаръчни Комплекти': '🎁 Gift Sets',
    'Всички Видове': 'All Varieties', 'Странджански Манов': 'Strandzha Honeydew', 'Тракийска Акация': 'Thracian Acacia', 'Родопски & Горски Букет': 'Rhodope & Forest Bouquet',
    'Добруджанска Лавандула': 'Dobrujan Lavender', 'Дунавска Липа': 'Danube Linden', 'Всички Прашци': 'All Pollens',
    'Търсете по вид мед, пчелин или регион (Странджа, Родопи, Тракия...)': 'Search by honey type, apiary or region (Strandzha, Rhodopes, Thrace...)',
    'Сортиране': 'Sort', 'Сортиране: по подразбиране': 'Sort: default', 'Цена: ниска към висока': 'Price: low to high', 'Цена: висока към ниска': 'Price: high to low',
    'Име (А–Я)': 'Name (A–Z)', 'Най-нови': 'Newest', 'Най-високо оценени': 'Top rated', 'Цена от': 'Price from', 'до': 'to', 'Само налични': 'In stock only', 'Само любими': 'Favorites only',
    'Намерени продукти': 'Products found', 'Няма намерени продукти по тези критерии': 'No products match these criteria',
    'Опитайте да изчистите филтрите или напишете друг регион.': 'Try clearing the filters or typing a different region.', 'Изчисти филтрите': 'Clear filters',
    'Профил на пчеларя': 'Beekeeper profile', 'Профил': 'Profile', 'Преглед': 'Quick view', 'Любими': 'Favorites', 'Добави в любими': 'Add to favorites',
    'Добавено в любими': 'Added to favorites', 'Премахнато от любими': 'Removed from favorites',
    'Обадете се на пчеларя': 'Call the beekeeper', 'Пчелар': 'Beekeeper', 'изчерпано': 'sold out', 'Изчерпано': 'Sold out', 'В Количката': 'Add to cart',
    'Остават само': 'Only', 'бр.': 'left', 'Предишна': 'Previous', 'Следваща': 'Next', 'Добави в Количката': 'Add to Cart',
    'Тази опаковка е изчерпана.': 'This size is sold out.', 'В наличност има само': 'Only in stock:', 'Добавено': 'Added',
    'Абонирай се': 'Subscribe', 'Партида': 'Batch', 'Реколта': 'Harvest', 'Състав': 'Contents', 'Сертификати': 'Certificates', 'Лабораторен анализ': 'Lab analysis',
    'Протокол': 'Report', 'Отвори протокола': 'Open the report', 'Произход и проследимост': 'Origin and traceability',
    // --- региони и ръководство ---
    'Българска географска палитра': 'Bulgaria’s geographic palette', 'Пчелините по Региони': 'Apiaries by Region', 'Родопите': 'The Rhodopes',
    'Тракийска Низина': 'Thracian Lowland', 'Чиста природа': 'Pure nature', 'Природна подкрепа за здравето': 'Natural support for your health',
    'Пчелен Прашец — Природна Енергия от Кошера': 'Bee Pollen — Natural Energy from the Hive', 'Пресен Суров Замразен Прашец': 'Fresh Raw Frozen Pollen',
    'Сушен Цветен Прашец': 'Dried Flower Pollen', '22 Незаменими Аминокиселини': '22 Essential Amino Acids',
    '🌼 Разгледай Предложенията за Пчелен Прашец': '🌼 See the bee pollen offers',
    'Не сте сигурни кой мед или прашец да изберете?': 'Not sure which honey or pollen to choose?',
    'Всеки пчелен продукт има различен вкус и действие. Отговорете на 2 въпроса и вижте нашата препоръка според вашия вкус и цели.':
      'Every bee product has a different taste and effect. Answer 2 questions and see our recommendation based on your taste and goals.',
    'Хората зад кошерите': 'The people behind the hives', 'Производители в Платформата': 'Producers on the Platform',
    '🐝 Вие сте пчелар? Добавете своя пчелин в платформата': '🐝 Are you a beekeeper? Add your apiary to the platform',
    // --- отзиви ---
    'Обратна връзка': 'Feedback', 'Отзиви от Нашите Клиенти': 'Reviews from Our Customers',
    'Споделете вашите впечатления от опитания мед или прашец и обслужването на пчеларите.': 'Share your impressions of the honey or pollen you tried and of the beekeepers’ service.',
    'Оставете Ваш Отзив': 'Leave Your Review', 'Споделете какво поръчахте и как оценявате качеството': 'Tell us what you ordered and how you rate the quality',
    'Вашето Име *': 'Your Name *', 'Град / Населено място': 'City / Town', 'Вашата Оценка:': 'Your Rating:', 'Вашият Коментар *': 'Your Comment *', 'Публикувай Отзива': 'Post Review',
    // --- количка и поръчка ---
    'Вашата Количка (': 'Your Cart (', 'Приложи': 'Apply', 'Мед и продукти:': 'Honey & products:', 'Отстъпка (10%):': 'Discount:', 'Отстъпка:': 'Discount:',
    'Доставка (България):': 'Shipping (Bulgaria):', 'Обща Сума:': 'Total:', 'Премини към Поръчка': 'Proceed to Checkout',
    'Количката ви е празна': 'Your cart is empty', 'Към Каталога': 'To the Catalog', 'Поръчката ви е с': 'Your order has', 'БЕЗПЛАТНА доставка': 'FREE shipping', 'за България!': 'within Bulgaria!',
    'Добавете още': 'Add another', 'за БЕЗПЛАТНА доставка!': 'for FREE shipping!', 'БЕЗПЛАТНА': 'FREE',
    'Завършване на Поръчката': 'Complete Your Order', '1. Данни за Получателя': '1. Recipient details', 'Запазени адреси': 'Saved addresses',
    'Име и Фамилия *': 'Full name *', 'Телефон за потвърждение *': 'Phone for confirmation *', 'Имейл адрес (за разписка)': 'Email (for the receipt)',
    '2. Доставка за Цяла България': '2. Delivery across Bulgaria', 'Град / Област *': 'City / Region *', 'гр. София': 'Sofia', 'гр. Пловдив': 'Plovdiv', 'гр. Варна': 'Varna',
    'гр. Бургас': 'Burgas', 'гр. Русе': 'Ruse', 'гр. Стара Загора': 'Stara Zagora', 'гр. Плевен': 'Pleven', 'гр. Добрич': 'Dobrich', 'гр. Смолян': 'Smolyan',
    'гр. Благоевград': 'Blagoevgrad', 'гр. Велико Търново': 'Veliko Tarnovo', 'Друго населено място в България': 'Another place in Bulgaria',
    'Адрес за доставка или Офис на Еконт / Спиди *': 'Delivery address or Econt / Speedy office *',
    'напр. бул. България 45 или Офис Еконт Център': 'e.g. 45 Bulgaria Blvd. or Econt office downtown',
    'Куриер': 'Courier', 'Доставка до': 'Deliver to', 'Офис на куриера': 'Courier office', 'Личен адрес': 'Home address', '🎁 Това е подарък': '🎁 This is a gift',
    'Лично съобщение към подаръка (по желание)': 'Personal gift message (optional)', 'Начин на Плащане *': 'Payment method *', 'Без скрити такси': 'No hidden fees',
    '💵 Наложен платеж (в брой или с карта при доставка)': '💵 Cash on delivery (cash or card on delivery)', '💳 Банкова Карта / Revolut Pay': '💳 Bank card / Revolut Pay',
    'в момента не е налично': 'currently unavailable', 'Избран метод:': 'Selected method:', 'Наложен платеж': 'Cash on delivery', 'Банкова карта (онлайн)': 'Bank card (online)',
    'Крайна Сума за плащане:': 'Total to pay:', 'Потвърди & Изпрати Поръчката': 'Confirm & Place Order', '— изберете запазен адрес —': '— choose a saved address —',
    'офис': 'office', 'адрес': 'address', 'до адрес': 'to address', 'до офис': 'to office', 'Артикули': 'Items', 'Отстъпка': 'Discount', 'Доставка': 'Shipping', 'Изчислява се...': 'Calculating...',
    'Пренасочваме ви към плащане...': 'Redirecting you to payment...', 'Поръчката е записана, но плащането не стартира': 'Your order is saved, but the payment could not start',
    'Можете да платите при доставка – свържете се с нас.': 'You can pay on delivery – please contact us.', 'Поръчка': 'Order', 'е регистрирана успешно!': 'was placed successfully!',
    'Поръчката не беше приета': 'The order was not accepted',
    'Поръчката ви е приета успешно!': 'Your order was placed successfully!', 'Благодарим ви, че подкрепяте независимите български пчелари!': 'Thank you for supporting independent Bulgarian beekeepers!',
    'Номер на поръчка:': 'Order number:', 'Град за доставка:': 'Delivery city:', 'Метод на плащане:': 'Payment method:',
    'Данните са записани в системата. Пчеларят ще се свърже с вас на посочения телефон за потвърждение на доставката.': 'Your details are saved. The beekeeper will contact you on the phone you provided to confirm delivery.',
    'Детайли за Продукта': 'Product Details',
    // --- заявка на пчелар ---
    'Добави Своя Пчелин в «Пчелари България»': 'Add Your Apiary to “Beekeepers Bulgaria”', 'Предложете своя мед или прашец директно на крайните потребители': 'Offer your honey or pollen directly to customers',
    'Име и Фамилия на пчеларя *': 'Beekeeper’s full name *', 'Име на Пчелина / Стопанството *': 'Apiary / farm name *', 'Регистрационен № по БАБХ *': 'BFSA registration no. *',
    'Регион на кошерите *': 'Hive region *', 'Стара планина / Балкан': 'Balkan Mountains', 'Пирин & Рила': 'Pirin & Rila', 'Населено място (село / град) *': 'Village / town *',
    'Телефон за връзка с клиенти *': 'Phone for customers *', 'Какви продукти предлагате? *': 'Which products do you offer? *', '🍯 Пчелен Мед': '🍯 Bee Honey', 'Брой кошери': 'Number of hives',
    'Видове мед/прашец, цена, реколта': 'Honey/pollen types, price, harvest',
    '💡 Ако сте влезли в акаунта си, след одобрение заявката ще бъде свързана с него и ще получите достъп до панела на пчеларя.': '💡 If you are signed in, your application will be linked to your account after approval and you will get access to the beekeeper panel.',
    'Изпрати Заявката за Одобрение': 'Submit Application for Approval',
    // --- футър ---
    'НАЦИОНАЛНА ПЛАТФОРМА': 'NATIONAL PLATFORM',
    'Платформа за директна връзка между българските пчелари и потребителите, които търсят чист мед и пчелен прашец от производител.': 'A platform connecting Bulgarian beekeepers directly with customers looking for pure honey and bee pollen from the producer.',
    '🛡️ Всички пчелини са с регистрация по БАБХ съгласно Закона за пчеларството.': '🛡️ All apiaries are registered with the BFSA in accordance with the Beekeeping Act.',
    '🌲 Странджа (Манов мед)': '🌲 Strandzha (Honeydew)', '🏔️ Родопите (Билков букет)': '🏔️ Rhodopes (Herbal bouquet)', '🌾 Тракия (Акациев мед)': '🌾 Thrace (Acacia honey)',
    '🌻 Добруджа (Лавандула)': '🌻 Dobruja (Lavender)', '🌊 Дунав (Липов мед)': '🌊 Danube (Linden honey)', 'Продукти': 'Products',
    '🍯 100% Натурален Мед': '🍯 100% Natural Honey', '🌼 Пресен Суров Прашец': '🌼 Fresh Raw Pollen', '☀️ Сушен Цветен Прашец': '☀️ Dried Flower Pollen',
    '🧬 Ползи от Прашеца': '🧬 Benefits of Pollen', '❓ Помощ при избор': '❓ Help me choose', '📦 Публикувай мед или прашец': '📦 Publish honey or pollen',
    '📞 Въпроси и контакти': '📞 Questions & contacts', '📜 Регистър на пчеларите': '📜 Beekeeper register',
    '© 2026 ПЧЕЛАРИ БЪЛГАРИЯ — Национална платформа за чист мед и пчелен прашец. Дипломен проект.': '© 2026 BEEKEEPERS BULGARIA — National platform for pure honey and bee pollen. Diploma project.',
    // --- акаунт и вход ---
    'Вход': 'Sign in', 'Регистрация': 'Register', 'Затвори': 'Close', 'Име и фамилия': 'Full name', 'Имейл': 'Email', 'Парола': 'Password', 'Създай акаунт': 'Create account',
    'Минимум 8 символа: главна и малка буква, цифра и специален знак.': 'At least 8 characters: upper- and lowercase letter, a digit and a special character.',
    'Не е нужен акаунт, за да поръчате – той ви дава история на поръчките, любими, запазени адреси и абонаменти.': 'You don’t need an account to order – it gives you order history, favorites, saved addresses and subscriptions.',
    'Попълнете имейл и парола.': 'Enter your email and password.', 'Въведете име.': 'Enter your name.', 'Добре дошли': 'Welcome', 'Излязохте от акаунта.': 'You have signed out.',
    'Админ панел': 'Admin panel', 'Панел на пчеларя': 'Beekeeper panel', 'Моите поръчки': 'My orders', 'Абонаменти': 'Subscriptions', 'Профил и адреси': 'Profile & addresses', 'Изход': 'Sign out',
    'Отговор': 'Answer', 'Нов въпрос': 'New question', 'Неуспешно зареждане': 'Failed to load', 'Грешка': 'Error',
    'Няма връзка със сървъра. Опитайте отново.': 'Cannot reach the server. Please try again.', 'Твърде много опити. Изчакайте малко и опитайте отново.': 'Too many attempts. Wait a moment and try again.',
    'Колко е': 'What is',
    // --- проследяване ---
    'Проследяване на поръчка': 'Order tracking', 'Номер на поръчка': 'Order number', 'Телефон, с който сте поръчали': 'Phone used for the order', 'Провери': 'Check',
    'Плащане': 'Payment', 'Общо': 'Total', 'Не намерихме поръчка с тези данни. Проверете номера и телефона.': 'We could not find an order with these details. Check the number and phone.',
    'Благодарим! Плащането за поръчка': 'Thank you! The payment for order', 'се обработва.': 'is being processed.',
    'Плащането беше прекъснато. Поръчката е записана – платете при доставка или се свържете с нас.': 'The payment was interrupted. Your order is saved – pay on delivery or contact us.',
    'Приета': 'Received', 'Обработва се': 'Processing', 'Изпратена': 'Shipped', 'Доставена': 'Delivered', 'Отказана': 'Cancelled',
    'Чака плащане': 'Awaiting payment', 'Платена': 'Paid', 'Неуспешно плащане': 'Payment failed',
    // --- абонамент ---
    'Влезте в акаунта си, за да се абонирате.': 'Sign in to subscribe.', 'Абонамент за': 'Subscription for', 'Получавайте продукта редовно с отстъпка': 'Get this product regularly with a discount of',
    'Плащане при доставка. Можете да паузирате или прекратите по всяко време от профила си.': 'Cash on delivery. You can pause or cancel any time from your account.',
    'Опаковка': 'Size', 'Количество': 'Quantity', 'Доставка на всеки': 'Deliver every', 'месец': 'month', 'месеца': 'months', 'Първа доставка': 'First delivery', 'Име': 'Name',
    'Телефон': 'Phone', 'Град': 'City', 'Адрес или офис на куриер': 'Address or courier office', 'Абонаментът е създаден. Управлявайте го от профила си.': 'Subscription created. Manage it from your account.',
    'Активен': 'Active', 'На пауза': 'Paused', 'Прекратен': 'Cancelled',
    // --- профил ---
    'Зареждане...': 'Loading...', 'Все още нямате поръчки.': 'You have no orders yet.', 'Към каталога': 'To the catalog', 'Проследи': 'Track', 'Поръчай отново': 'Order again',
    'Артикулите от тази поръчка вече не са налични.': 'The items from this order are no longer available.', 'Поръчки': 'Orders',
    'Нямате любими продукти. Натиснете ♡ върху продукт в каталога.': 'You have no favorites. Tap ♡ on a product in the catalog.', 'от': 'from', 'Премахни': 'Remove',
    'Нямате абонаменти. Отворете продукт в каталога и изберете „Абонирай се“.': 'You have no subscriptions. Open a product in the catalog and choose “Subscribe”.',
    'На всеки': 'Every', 'мес.': 'mo.', 'следваща доставка': 'next delivery', 'Създадени поръчки': 'Orders created', 'Поднови': 'Resume', 'Пауза': 'Pause', 'Прекрати': 'Cancel',
    'Сигурни ли сте, че искате да прекратите абонамента?': 'Are you sure you want to cancel the subscription?', 'Лични данни': 'Personal details', 'Запази': 'Save',
    'Смяна на парола': 'Change password', 'Текуща парола': 'Current password', 'Нова парола': 'New password', 'Смени паролата': 'Change password', 'Получател': 'Recipient',
    'Адрес': 'Address', 'основен': 'default', 'Нямате запазени адреси.': 'You have no saved addresses.', 'Нов адрес': 'New address', 'Име на адреса': 'Address name',
    'Основен адрес': 'Default address', 'Запази адреса': 'Save address', 'Запазено.': 'Saved.', 'Паролата е сменена.': 'Password changed.', 'Редакция на адрес': 'Edit address',
    'Да изтрием ли този адрес?': 'Delete this address?',
    // --- страница на пчелар ---
    'Липсва пчелар.': 'No beekeeper specified.', 'Към списъка': 'To the list', 'Пчеларят не е намерен.': 'Beekeeper not found.', 'БАБХ проверен': 'BFSA verified', 'Чака проверка': 'Pending verification',
    'г. опит': 'yrs experience', 'отзива': 'reviews', 'За пчеларя': 'About the beekeeper', 'Пчелинът': 'The apiary', 'Виж в каталога': 'View in catalog',
    'Отзиви за продуктите на пчеларя': 'Reviews of the beekeeper’s products', 'Все още няма публикувани продукти.': 'No products published yet.', 'Все още няма отзиви.': 'No reviews yet.',
    // --- панел ---
    'Този акаунт няма достъп до панела на пчеларя. Кандидатствайте от началната страница („За Пчелари“), като сте влезли в акаунта си, или помолете администратор да свърже акаунта ви с вашия пчелин.':
      'This account has no access to the beekeeper panel. Apply from the home page (“For Beekeepers”) while signed in, or ask an administrator to link your account to your apiary.',
    'Към магазина': 'To the shop', 'Вход за пчелари': 'Beekeeper sign-in', 'Опаковки': 'Sizes', 'Малка наличност': 'Low stock', 'Продукт': 'Product', 'Наличност': 'Stock', 'Малко': 'Low',
    'Нов продукт': 'New product', 'Вид': 'Type', 'Нямате продукти.': 'You have no products.', 'Да изтрием ли този продукт? Действието е необратимо.': 'Delete this product? This cannot be undone.',
    'Етикет': 'Label', 'Тегло': 'Weight', 'Цена €': 'Price €', 'Редакция на продукт': 'Edit product', 'Категория': 'Category', 'Описание': 'Description', 'Снимка': 'Photo', 'Качи': 'Upload',
    'Попълвайте само верни данни – те се показват на клиентите като гаранция за произхода.': 'Enter only accurate data – it is shown to customers as a guarantee of origin.',
    'Допълнителни характеристики и английски текст': 'Additional properties and English text', 'Отказ': 'Cancel', 'Не е избран файл.': 'No file selected.',
    'Снимката е твърде голяма (максимум 4 MB).': 'The image is too large (4 MB max).', 'Подарък': 'Gift', 'Все още няма поръчки за вашите продукти.': 'No orders for your products yet.',
    'Публичен профил': 'Public profile', 'Виж страницата': 'View page', 'Кратко представяне': 'Short bio', 'Вашата история': 'Your story', 'Разделяйте абзаците с празен ред.': 'Separate paragraphs with a blank line.',
    'Ваша снимка': 'Your photo', 'Снимки от пчелина': 'Apiary photos', 'Добави снимка': 'Add photo', 'Запази профила': 'Save profile', 'Достигнахте максимума от 10 снимки.': 'You have reached the 10 photo maximum.',
    'Наличности': 'Inventory',
    // --- забравена парола, потвърждение, поверителност, отказ на поръчка ---
    'Забравена парола?': 'Forgot your password?', 'Забравена парола': 'Forgot password', 'Нова парола': 'New password',
    'Въведете имейла на акаунта си и ще Ви изпратим връзка за нова парола.': 'Enter your account email and we will send you a link to set a new password.',
    'Изпрати връзка': 'Send link', 'Обратно към входа': 'Back to sign in', 'Повторете паролата': 'Repeat password', 'Запази паролата': 'Save password',
    'Паролите не съвпадат.': 'The passwords do not match.', 'Към профила': 'To my account',
    'Имейлът Ви още не е потвърден. Потвърдете го, за да получавате известия за поръчките.': 'Your email is not confirmed yet. Confirm it to receive order notifications.',
    'Изпрати писмо': 'Send email', 'Разписка': 'Receipt', 'Откажи': 'Cancel order', 'Откажи поръчката': 'Cancel order',
    'Сигурни ли сте, че искате да откажете поръчка': 'Are you sure you want to cancel order', 'Поръчката е отказана.': 'The order has been cancelled.',
    'Сигурни ли сте, че искате да откажете поръчката?': 'Are you sure you want to cancel this order?',
    'С регистрацията приемате': 'By registering you accept', 'Общите условия': 'the Terms and Conditions', 'и': 'and', 'Политиката за поверителност': 'the Privacy Policy',
    'Поверителност и лични данни': 'Privacy and personal data', 'Политика за поверителност': 'Privacy policy', 'Изтегли моите данни': 'Download my data',
    'Можете да изтеглите всички данни, които пазим за Вас, или да изтриете акаунта си. Историята на поръчките се пази анонимно за счетоводството.':
      'You can download all the data we keep about you or delete your account. Order history is retained anonymously for accounting purposes.',
    'За изтриване на акаунта въведете паролата си': 'To delete your account, enter your password', 'Изтрий акаунта': 'Delete account',
    'Въведете паролата си.': 'Enter your password.', 'Акаунтът и личните Ви данни ще бъдат изтрити окончателно. Продължавате ли?': 'Your account and personal data will be permanently deleted. Continue?',
    'Акаунтът е изтрит. Благодарим, че бяхте с нас!': 'Your account has been deleted. Thank you for being with us!',
    // --- контакти, бисквитки, бюлетин, наличност ---
    'Контакти': 'Contact', 'Доставка и плащане': 'Delivery and payment', 'Връщане и рекламации': 'Returns and complaints', 'Общи условия': 'Terms and conditions',
    'Поверителност': 'Privacy', 'Бисквитки': 'Cookies', 'Към съдържанието': 'Skip to content', 'Известие за бисквитки': 'Cookie notice', 'Разбрах': 'Got it',
    'Повече информация': 'More information',
    'Сайтът използва само необходими бисквитки и локално хранилище (вход, количка, любими, тема и език). Не проследяваме посетителите и не показваме реклами.':
      'This site only uses necessary cookies and local storage (sign-in, cart, favourites, theme and language). We do not track visitors or show ads.',
    'Бюлетин': 'Newsletter', 'Запиши ме': 'Subscribe', 'Вашият имейл': 'Your email', 'Имейл за бюлетина': 'Newsletter email', 'Благодарим!': 'Thank you!',
    'Пишете ни': 'Write to us', 'Пишете на пчеларя': 'Write to the beekeeper', 'Уведоми ме, когато е наличен': 'Notify me when available', 'Страница на продукта': 'Product page',
    'Изпратете съобщение': 'Send a message', 'Съобщение до пчелин': 'Message to apiary', 'Въпрос към': 'Question for',
    'Съобщението ще бъде препратено на пчелина и на екипа на платформата.': 'Your message will be forwarded to the apiary and the platform team.',
    'Попълнете всички полета (съобщението – поне 10 символа).': 'Fill in all fields (message – at least 10 characters).', 'Съобщението е изпратено.': 'Your message has been sent.',
    'Куриер': 'Courier', 'До офис': 'To office', 'До адрес': 'To address', 'Безплатна доставка при поръчка над': 'Free delivery on orders over'
  };

  const LANG_KEY = 'pchelari_lang', THEME_KEY = 'pchelari_theme';
  const norm = (s) => String(s).replace(/\s+/g, ' ').trim();
  const store = {
    get(k) { try { return localStorage.getItem(k); } catch (e) { return null; } },
    set(k, v) { try { localStorage.setItem(k, v); } catch (e) { /* частен режим */ } }
  };

  const I18N = (window.I18N = {
    lang: store.get(LANG_KEY) === 'en' ? 'en' : 'bg',
    t(text) {
      if (I18N.lang !== 'en' || text == null) return text;
      const v = EN[norm(text)];
      return v === undefined ? text : v;
    }
  });

  // ---------- Превод на DOM (по текстови възли и атрибути) ----------
  const originals = new WeakMap();
  const ATTRS = ['placeholder', 'title', 'aria-label', 'alt'];

  function translateText(node) {
    if (!originals.has(node)) {
      const key = norm(node.nodeValue);
      if (!key || EN[key] === undefined) return;
      originals.set(node, node.nodeValue);
    }
    const raw = originals.get(node);
    if (!raw) return;
    if (I18N.lang === 'en') {
      const tr = EN[norm(raw)];
      if (tr !== undefined) node.nodeValue = raw.match(/^\s*/)[0] + tr + raw.match(/\s*$/)[0];
    } else {
      node.nodeValue = raw;
    }
  }

  function translateAttrs(el) {
    for (const a of ATTRS) {
      if (!el.hasAttribute || !el.hasAttribute(a)) continue;
      const store_key = 'bg' + a.replace('-', '');
      if (el.dataset[store_key] === undefined) {
        if (EN[norm(el.getAttribute(a))] === undefined) continue;
        el.dataset[store_key] = el.getAttribute(a);
      }
      const raw = el.dataset[store_key];
      el.setAttribute(a, I18N.lang === 'en' ? EN[norm(raw)] ?? raw : raw);
    }
  }

  function walk(root) {
    if (!root) return;
    if (root.nodeType === 3) return translateText(root);
    if (root.nodeType !== 1) return;
    const skip = { SCRIPT: 1, STYLE: 1, TEXTAREA: 1, NOSCRIPT: 1 };
    if (skip[root.tagName]) return;
    translateAttrs(root);
    const tw = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT | NodeFilter.SHOW_TEXT, {
      acceptNode: (n) => (n.nodeType === 1 && skip[n.tagName] ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT)
    });
    let n;
    while ((n = tw.nextNode())) n.nodeType === 3 ? translateText(n) : translateAttrs(n);
  }

  let observer = null;
  function startObserver() {
    if (observer || !window.MutationObserver) return;
    observer = new MutationObserver((muts) => {
      if (I18N.lang !== 'en') return;
      for (const m of muts) m.addedNodes.forEach((n) => walk(n));
    });
    observer.observe(document.body, { childList: true, subtree: true });
  }

  const origTitle = {};
  function translateTitle() {
    if (origTitle.v === undefined) origTitle.v = document.title;
    document.title = I18N.lang === 'en' ? (EN[norm(origTitle.v)] || origTitle.v) : origTitle.v;
  }

  I18N.apply = function () {
    document.documentElement.lang = I18N.lang;
    walk(document.body);
    translateTitle();
    refreshButtons();
  };

  I18N.setLang = function (lang) {
    I18N.lang = lang === 'en' ? 'en' : 'bg';
    store.set(LANG_KEY, I18N.lang);
    I18N.apply();
    window.dispatchEvent(new Event('langchange'));
  };

  // ---------- Тема ----------
  // Сайтът е тъмен по подразбиране; светлата тема е по избор на потребителя
  I18N.theme = document.documentElement.dataset.theme || store.get(THEME_KEY) || 'dark';
  I18N.setTheme = function (theme) {
    I18N.theme = theme === 'light' ? 'light' : 'dark';
    document.documentElement.dataset.theme = I18N.theme;
    store.set(THEME_KEY, I18N.theme);
    const meta = document.querySelector('meta[name="theme-color"]');
    if (meta) meta.setAttribute('content', I18N.theme === 'light' ? '#FBF6EC' : '#140a05');
    refreshButtons();
  };

  // ---------- Бутони за език и тема ----------
  function refreshButtons() {
    document.querySelectorAll('[data-tool="theme"]').forEach((b) => {
      b.textContent = I18N.theme === 'light' ? '🌙' : '☀️';
      const label = I18N.lang === 'en' ? (I18N.theme === 'light' ? 'Dark theme' : 'Light theme') : (I18N.theme === 'light' ? 'Тъмна тема' : 'Светла тема');
      b.title = label; b.setAttribute('aria-label', label);
    });
    document.querySelectorAll('[data-tool="lang"]').forEach((b) => {
      b.textContent = I18N.lang === 'en' ? 'BG' : 'EN';
      const label = I18N.lang === 'en' ? 'Превключи на български' : 'Switch to English';
      b.title = label; b.setAttribute('aria-label', label);
    });
  }

  I18N.mountTools = function (host) {
    if (!host || host.dataset.mounted) return;
    host.dataset.mounted = '1';
    const only = host.dataset.tools; // "theme" – само тема (напр. в блога, който е на български)
    host.innerHTML = '';
    const mk = (tool, onClick) => {
      const b = document.createElement('button');
      b.type = 'button'; b.className = 'icon-btn'; b.dataset.tool = tool;
      b.addEventListener('click', onClick);
      host.appendChild(b);
    };
    if (!only || only === 'theme') mk('theme', () => I18N.setTheme(I18N.theme === 'light' ? 'dark' : 'light'));
    if (!only) mk('lang', () => I18N.setLang(I18N.lang === 'en' ? 'bg' : 'en'));
    host.style.display = 'inline-flex'; host.style.gap = '6px';
    refreshButtons();
  };

  document.addEventListener('DOMContentLoaded', () => {
    document.documentElement.dataset.theme = I18N.theme;
    document.querySelectorAll('#site-tools').forEach(I18N.mountTools);
    if (I18N.lang === 'en') I18N.apply(); else document.documentElement.lang = 'bg';
    startObserver();
  });
})();
