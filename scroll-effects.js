/* ==========================================================================
   SCROLL TRANSITIONS ENGINE — Южен Пчелин
   IntersectionObserver-based reveal, parallax, ripple, cursor glow, progress
   ========================================================================== */

(function() {
  'use strict';

  /* ──────────────────────────────────────────────
     1. SCROLL PROGRESS BAR
  ────────────────────────────────────────────── */
  function initScrollProgress() {
    const bar = document.createElement('div');
    bar.id = 'scroll-progress-bar';
    document.body.prepend(bar);

    window.addEventListener('scroll', () => {
      const scrollTop = window.scrollY;
      const docHeight = document.documentElement.scrollHeight - window.innerHeight;
      const pct = docHeight > 0 ? (scrollTop / docHeight) * 100 : 0;
      bar.style.width = pct + '%';
    }, { passive: true });
  }

  /* ──────────────────────────────────────────────
     2. CURSOR GLOW TRAIL
  ────────────────────────────────────────────── */
  function initCursorGlow() {
    const glow = document.createElement('div');
    glow.id = 'cursor-glow';
    document.body.appendChild(glow);

    let cx = 0, cy = 0;
    let tx = 0, ty = 0;

    document.addEventListener('mousemove', (e) => {
      tx = e.clientX;
      ty = e.clientY;
    });

    function raf() {
      cx += (tx - cx) * 0.1;
      cy += (ty - cy) * 0.1;
      glow.style.left = cx + 'px';
      glow.style.top  = cy + 'px';
      requestAnimationFrame(raf);
    }
    requestAnimationFrame(raf);
  }

  /* ──────────────────────────────────────────────
     3. INTERSECTION OBSERVER — REVEAL ON SCROLL
  ────────────────────────────────────────────── */
  function initScrollReveal() {
    // Auto-tag sections and key elements
    const autoTag = [
      { selector: '.section-header',      cls: 'reveal--up' },
      { selector: '.product-card',        cls: 'reveal--up' },
      { selector: '.review-card',         cls: 'reveal--up' },
      { selector: '.metric-card',         cls: 'reveal--scale' },
      { selector: '.story-item',          cls: 'reveal--left' },
      { selector: '.quiz-card',           cls: 'reveal--up' },
      { selector: '.spotlight-float-card', cls: 'reveal--up' },
      { selector: '.craft-arch-card',     cls: 'reveal--scale' },
      { selector: '.spotlight-step-badge', cls: 'reveal--right' },
      { selector: '.hero-arch-column',    cls: 'reveal--up' },
      { selector: '.hero-center-column',  cls: 'reveal--scale' },
      { selector: '.editorial-headline',  cls: 'reveal--up' },
      { selector: '.floating-honey-candle', cls: 'reveal--left' },
      { selector: '.spotlight-editorial-title', cls: 'reveal--left' },
      { selector: '.glass-glow-cube',     cls: 'reveal--scale' },
      { selector: '.region-card',         cls: 'reveal--up' },
    ];

    autoTag.forEach(({ selector, cls }) => {
      document.querySelectorAll(selector).forEach((el, i) => {
        el.classList.add('reveal');
        if (cls) el.classList.add(cls);
        // Add stagger for grids
        if (['product-card', 'review-card', 'metric-card'].some(c => el.classList.contains(c))) {
          el.style.transitionDelay = `${i * 80}ms`;
        }
      });
    });

    // Mark product-cards parent as reveal-group for stagger
    const grids = document.querySelectorAll('.products-grid, .reviews-grid, .lab-metrics-grid');
    grids.forEach(g => g.classList.add('reveal-group'));

    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('is-visible');
          // Optional: stop observing after first reveal
          // observer.unobserve(entry.target);
        } else {
          // Re-hide when scrolling back up (uncomment to disable reverse)
          // entry.target.classList.remove('is-visible');
        }
      });
    }, {
      threshold: 0.05,
      rootMargin: '0px 0px -20px 0px'
    });

    document.querySelectorAll('.reveal, .reviews-section').forEach(el => observer.observe(el));

    // Re-observe dynamically added product cards
    const productGrid = document.getElementById('products-grid');
    if (productGrid) {
      const mutObs = new MutationObserver(() => {
        productGrid.querySelectorAll('.product-card:not(.reveal)').forEach((el, i) => {
          el.classList.add('reveal', 'reveal--up');
          el.style.transitionDelay = `${i * 80}ms`;
          observer.observe(el);
        });
      });
      mutObs.observe(productGrid, { childList: true });
    }
  }

  /* ──────────────────────────────────────────────
     4. SECTION GLOW SEPARATORS
  ────────────────────────────────────────────── */
  function initGlowSeparators() {
    const sections = document.querySelectorAll(
      '#atmosphere, #products, #quiz, .reviews-section'
    );

    sections.forEach(section => {
      const sep = document.createElement('div');
      sep.className = 'glow-separator';
      section.insertAdjacentElement('beforebegin', sep);
    });
  }

  /* ──────────────────────────────────────────────
     5. SECTION DIVIDER DECORATORS
  ────────────────────────────────────────────── */
  function initSectionDividers() {
    document.querySelectorAll('.section-header').forEach(header => {
      const sub = header.querySelector('.sub-heading');
      if (sub && !header.querySelector('.section-divider')) {
        const div = document.createElement('div');
        div.className = 'section-divider';
        div.innerHTML = '<span>🍯</span>';
        header.insertAdjacentElement('afterend', div);
      }
    });
  }

  /* ──────────────────────────────────────────────
     6. SCROLL-LINKED PARALLAX
  ────────────────────────────────────────────── */
  function initParallax() {
    const layers = [
      { selector: '.glow-center',      speed: 0.18 },
      { selector: '.glow-bottom-left', speed: 0.12 },
      { selector: '.glow-bottom-right', speed: 0.15 },
      { selector: '.spotlight-ambient-glow.glow-top', speed: 0.1 },
      { selector: '.luxury-bg-glow',   speed: 0.08 },
    ];

    const elements = [];
    layers.forEach(({ selector, speed }) => {
      document.querySelectorAll(selector).forEach(el => {
        elements.push({ el, speed });
      });
    });

    if (elements.length === 0) return;

    let ticking = false;

    function onScroll() {
      if (!ticking) {
        requestAnimationFrame(() => {
          const scrollY = window.scrollY;
          elements.forEach(({ el, speed }) => {
            const parent = el.closest('section') || el.parentElement;
            if (!parent) return;
            const rect = parent.getBoundingClientRect();
            const relY = -(rect.top) * speed;
            el.style.transform = `translateY(${relY}px)`;
          });
          ticking = false;
        });
        ticking = true;
      }
    }

    window.addEventListener('scroll', onScroll, { passive: true });
  }

  /* ──────────────────────────────────────────────
     7. RIPPLE EFFECT ON ALL BUTTONS
  ────────────────────────────────────────────── */
  function initRippleEffect() {
    function addRipple(e) {
      const btn = e.currentTarget;
      const existing = btn.querySelector('.ripple-dot');
      if (existing) existing.remove();

      const rect = btn.getBoundingClientRect();
      const dot = document.createElement('span');
      dot.className = 'ripple-dot';
      dot.style.left = (e.clientX - rect.left) + 'px';
      dot.style.top  = (e.clientY - rect.top) + 'px';
      btn.appendChild(dot);
      dot.addEventListener('animationend', () => dot.remove());
    }

    document.querySelectorAll('.btn').forEach(btn => {
      btn.classList.add('btn-ripple');
      btn.addEventListener('click', addRipple);
    });

    // Also apply to dynamically added buttons
    const observer = new MutationObserver(() => {
      document.querySelectorAll('.btn:not(.btn-ripple)').forEach(btn => {
        btn.classList.add('btn-ripple');
        btn.addEventListener('click', addRipple);
      });
    });
    observer.observe(document.body, { childList: true, subtree: true });
  }

  /* ──────────────────────────────────────────────
     8. SECTION BACKGROUND COLOR SHIFTS
  ────────────────────────────────────────────── */
  function initSectionColorShift() {
    const sections = [
      { el: document.getElementById('hero'),        bg: '#0E0905' },
      { el: document.getElementById('atmosphere'),  bg: '#0d0604' },
      { el: document.getElementById('products'),    bg: '#090403' },
      { el: document.getElementById('regions'),     bg: '#0a0503' },
      { el: document.getElementById('quiz'),        bg: '#0c0504' },
      { el: document.getElementById('reviews'),     bg: '#000000' },
    ];

    const valid = sections.filter(s => s.el);
    if (valid.length === 0) return;

    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          const found = valid.find(s => s.el === entry.target);
          if (found) {
            document.body.style.transition = 'background-color 0.8s ease';
            document.body.style.backgroundColor = found.bg;
          }
        }
      });
    }, { threshold: 0.3 });

    valid.forEach(({ el }) => observer.observe(el));
  }

  /* ──────────────────────────────────────────────
     9. SCROLL-LINKED SECTION ENTRY GLOW PULSE
  ────────────────────────────────────────────── */
  function initSectionGlowPulse() {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('section-glow-enter');
          setTimeout(() => {
            entry.target.classList.remove('section-glow-enter');
          }, 1600);
        }
      });
    }, { threshold: 0.2 });

    document.querySelectorAll('section').forEach(s => observer.observe(s));
  }

  /* ──────────────────────────────────────────────
     10. SHIMMER ON PRODUCT & REVIEW CARDS
  ────────────────────────────────────────────── */
  function initShimmer() {
    function applyShimmer() {
      document.querySelectorAll('.product-card, .review-card, .card-glass:not(.shimmer-card)').forEach(el => {
        el.classList.add('shimmer-card');
      });
    }
    applyShimmer();

    // Re-apply when products re-render
    new MutationObserver(applyShimmer).observe(
      document.getElementById('products-grid') || document.body,
      { childList: true }
    );
  }

  /* ──────────────────────────────────────────────
     11. UPDATE IMAGES TO NEW AI-GENERATED ONES
  ────────────────────────────────────────────── */
  function updateProductImages() {
    // Update manov images
    document.querySelectorAll('img[src*="manov.jpg"]').forEach(img => {
      img.src = './images/manov_new.jpg';
    });
    // Update acacia images
    document.querySelectorAll('img[src*="acacia.jpg"]').forEach(img => {
      img.src = './images/acacia_new.jpg';
    });
  }

  /* ──────────────────────────────────────────────
     12. SMOOTH SCROLL WITH EASING for nav links
  ────────────────────────────────────────────── */
  function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(link => {
      link.addEventListener('click', (e) => {
        const target = document.querySelector(link.getAttribute('href'));
        if (!target) return;
        e.preventDefault();

        const navH = document.getElementById('navbar')?.offsetHeight || 70;
        const top  = target.getBoundingClientRect().top + window.scrollY - navH - 20;

        window.scrollTo({ top, behavior: 'smooth' });
      });
    });
  }

  /* ──────────────────────────────────────────────
     13. WAVE SECTIONS BETWEEN SECTIONS
  ────────────────────────────────────────────── */
  function insertWaveDividers() {
    const wavePairs = [
      { after: '#hero',        fill1: '#0d0604', fill2: '#0b0503' },
      { after: '#atmosphere',  fill1: '#090403', fill2: '#0d0604' },
      { after: '#products',    fill1: '#000000', fill2: '#000000' },
    ];

    wavePairs.forEach(({ after, fill1, fill2 }) => {
      const section = document.querySelector(after);
      if (!section) return;

      const wrapper = document.createElement('div');
      wrapper.className = 'section-wave';
      wrapper.innerHTML = `
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1440 60" preserveAspectRatio="none" height="60">
          <path fill="${fill1}" d="M0,32 C360,60 1080,0 1440,32 L1440,0 L0,0 Z"/>
          <path fill="${fill2}" opacity="0.5" d="M0,48 C480,20 960,60 1440,28 L1440,60 L0,60 Z"/>
        </svg>
      `;
      section.insertAdjacentElement('afterend', wrapper);
    });
  }

  /* ──────────────────────────────────────────────
     14. HONEY DRIPS FROM HERO INTO OTHER SECTIONS
  ────────────────────────────────────────────── */
  function initHoneyDrips() {
    // Sections that receive the drips (all except hero)
    const targetSelectors = [
      '#atmosphere',
      '#quiz',
      '#products',
      '#reviews',
    ];

    // Drip config — spread across the full width naturally
    // We define "anchor" x-positions as percentages and randomize slightly each time
    const dripConfig = [
      { xPct: 4,   dur: 3.1, delay: 0.0,  maxH: 70  },
      { xPct: 10,  dur: 2.5, delay: 0.6,  maxH: 110 },
      { xPct: 18,  dur: 3.4, delay: 1.1,  maxH: 85  },
      { xPct: 27,  dur: 2.8, delay: 0.3,  maxH: 100 },
      { xPct: 36,  dur: 3.7, delay: 1.5,  maxH: 60  },
      { xPct: 45,  dur: 2.6, delay: 0.8,  maxH: 115 },
      { xPct: 55,  dur: 3.2, delay: 0.1,  maxH: 80  },
      { xPct: 63,  dur: 2.9, delay: 1.3,  maxH: 105 },
      { xPct: 72,  dur: 3.5, delay: 0.5,  maxH: 70  },
      { xPct: 80,  dur: 2.7, delay: 1.0,  maxH: 95  },
      { xPct: 88,  dur: 3.0, delay: 0.4,  maxH: 88  },
      { xPct: 95,  dur: 3.3, delay: 1.7,  maxH: 65  },
    ];

    targetSelectors.forEach(sel => {
      const section = document.querySelector(sel);
      if (!section) return;

      // Ensure section has relative positioning for absolute children
      const pos = getComputedStyle(section).position;
      if (pos === 'static') section.style.position = 'relative';

      // Build the drip bar
      const bar = document.createElement('div');
      bar.className = 'honey-drip-bar';

      dripConfig.forEach(cfg => {
        // Add slight random jitter so each section looks different
        const jitter = (Math.random() - 0.5) * 3; // ±1.5%
        const xLeft  = Math.max(1, Math.min(98, cfg.xPct + jitter));
        const maxH   = cfg.maxH + Math.round((Math.random() - 0.5) * 20);
        const dur    = (cfg.dur + (Math.random() - 0.5) * 0.4).toFixed(2);
        const delay  = cfg.delay.toFixed(2);

        const drip = document.createElement('div');
        drip.className = 'honey-drip';
        drip.style.left = xLeft + '%';
        drip.style.setProperty('--drip-dur',   dur + 's');
        drip.style.setProperty('--drip-delay', delay + 's');
        drip.style.setProperty('--drip-max',   maxH + 'px');

        drip.innerHTML = `
          <div class="honey-drip__stream"></div>
          <div class="honey-drip__blob"></div>
        `;

        bar.appendChild(drip);
      });

      // Insert as first child so it sits behind section content
      section.insertBefore(bar, section.firstChild);
    });
  }

  /* ──────────────────────────────────────────────
     INIT ALL
  ────────────────────────────────────────────── */
  function init() {
    initScrollProgress();
    initCursorGlow();
    initGlowSeparators();
    insertWaveDividers();
    initSectionDividers();
    initScrollReveal();
    initParallax();
    initRippleEffect();
    initSectionColorShift();
    initSectionGlowPulse();
    initShimmer();
    updateProductImages();
    initSmoothScroll();
    initHoneyDrips();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

})();
