/* ==========================================================================
   Карта на градовете с мед (Leaflet)
   Данните идват от honey-locations.json — за нов град се добавя само запис
   в JSON файла, без промяна в кода.
   ========================================================================== */
(function () {
  'use strict';

  // Къде са данните
  const DATA_URL = 'honey-locations.json';

  // Граници на България (с малък запас) — картата не може да излиза извън тях
  const BULGARIA_BOUNDS = L.latLngBounds([40.9, 22.0], [44.5, 29.0]);
  const BULGARIA_CENTER = [42.75, 25.5];

  // Цвят на маркера според типа
  const TYPE_COLORS = {
    produced: '#F5C518', // жълт - производство
    sold: '#22A559',     // зелен - продажба
    both: '#F08A1C'      // оранжев - и двете
  };

  // Екранира текст, за да не се вмъква HTML от данните
  function esc(text) {
    const d = document.createElement('div');
    d.textContent = String(text);
    return d.innerHTML;
  }

  // Иконка с капка мед, оцветена според типа
  function createIcon(type) {
    const color = TYPE_COLORS[type] || TYPE_COLORS.both;
    const svg =
      '<svg viewBox="0 0 32 42" width="32" height="42" xmlns="http://www.w3.org/2000/svg">' +
      '<path d="M16 2 C16 2 4 17 4 26 a12 12 0 0 0 24 0 C28 17 16 2 16 2 Z" ' +
      'fill="' + color + '" stroke="#3A2106" stroke-width="2"/>' +
      '<ellipse cx="11.5" cy="24" rx="3" ry="5" fill="#fff" opacity="0.45" transform="rotate(20 11.5 24)"/>' +
      '</svg>';
    return L.divIcon({
      className: 'honey-marker',
      html: svg,
      iconSize: [32, 42],
      iconAnchor: [16, 40],
      popupAnchor: [0, -36]
    });
  }

  // Списък с елементи; връща празен низ, ако няма данни
  function listBlock(title, items) {
    if (!Array.isArray(items) || !items.length) return '';
    return '<div class="hm-block"><strong>' + title + '</strong><ul>' +
      items.map(i => '<li>' + i + '</li>').join('') + '</ul></div>';
  }

  // Съдържание на попъпа — липсващите или празни полета не се показват
  function buildPopup(loc) {
    const producers = (loc.producers || []).filter(Boolean).map(esc);
    const shops = (loc.shops || []).filter(s => s && s.name).map(s =>
      '<b>' + esc(s.name) + '</b>' + (s.address ? ', ' + esc(s.address) : ''));
    const honey = (loc.honeyTypes || []).filter(Boolean).map(esc);

    return '<div class="hm-popup"><h4>' + esc(loc.city) + '</h4>' +
      listBlock('Производители', producers) +
      listBlock('Магазини', shops) +
      listBlock('Видове мед', honey) +
      '</div>';
  }

  // Дали маркерът трябва да се вижда при текущите отметки
  function isVisible(type, showProduced, showSold) {
    if (type === 'both') return showProduced || showSold;
    if (type === 'produced') return showProduced;
    if (type === 'sold') return showSold;
    return true;
  }

  // Легенда + филтри като контрола в ъгъла на картата
  function addControlPanel(map, onChange) {
    const panel = L.control({ position: 'topright' });
    panel.onAdd = function () {
      const div = L.DomUtil.create('div', 'hm-panel');
      div.innerHTML =
        '<label class="hm-filter"><input type="checkbox" id="hm-produced" checked> Производство</label>' +
        '<label class="hm-filter"><input type="checkbox" id="hm-sold" checked> Продажба</label>' +
        '<div class="hm-legend">' +
        '<span><i style="background:' + TYPE_COLORS.produced + '"></i>Производство</span>' +
        '<span><i style="background:' + TYPE_COLORS.sold + '"></i>Продажба</span>' +
        '<span><i style="background:' + TYPE_COLORS.both + '"></i>Производство и продажба</span>' +
        '</div>';
      // Кликовете в панела да не местят картата
      L.DomEvent.disableClickPropagation(div);
      L.DomEvent.disableScrollPropagation(div);
      div.addEventListener('change', onChange);
      return div;
    };
    panel.addTo(map);
  }

  async function init() {
    const el = document.getElementById('honey-map');
    if (!el || typeof L === 'undefined') return;

    const map = L.map(el, {
      center: BULGARIA_CENTER,
      zoom: 7,
      minZoom: 6,
      maxBounds: BULGARIA_BOUNDS,
      maxBoundsViscosity: 1.0,
      // Свободно управление: колелце, влачене, двоен клик, щипване с два пръста, клавиатура
      scrollWheelZoom: true,
      dragging: true,
      doubleClickZoom: true,
      touchZoom: true,
      boxZoom: true,
      keyboard: true,
      zoomSnap: 0.5,          // по-плавно приближаване (половин нива)
      zoomDelta: 0.5,
      wheelPxPerZoomLevel: 90
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 18,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    // Зареждане на данните
    let locations = [];
    try {
      const res = await fetch(DATA_URL);
      if (!res.ok) throw new Error('HTTP ' + res.status);
      locations = await res.json();
    } catch (err) {
      console.error('Градовете с мед не можаха да се заредят:', err);
    }

    // По един маркер на град
    const markers = locations
      .filter(l => typeof l.lat === 'number' && typeof l.lng === 'number')
      .map(loc => ({
        type: loc.type,
        marker: L.marker([loc.lat, loc.lng], { icon: createIcon(loc.type), title: loc.city })
          .bindPopup(buildPopup(loc))
          .addTo(map)
      }));

    // Показва/скрива маркерите според отметките
    function applyFilter() {
      const showProduced = document.getElementById('hm-produced').checked;
      const showSold = document.getElementById('hm-sold').checked;
      markers.forEach(({ type, marker }) => {
        const visible = isVisible(type, showProduced, showSold);
        if (visible && !map.hasLayer(marker)) marker.addTo(map);
        if (!visible && map.hasLayer(marker)) map.removeLayer(marker);
      });
    }

    addControlPanel(map, applyFilter);

    // Скрита при зареждане секция може да даде грешен размер — преизчисляваме
    window.addEventListener('load', () => map.invalidateSize());
    window.addEventListener('resize', () => map.invalidateSize());
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
