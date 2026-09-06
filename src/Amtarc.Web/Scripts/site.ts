/**
 * Progressive enhancement for the public page.
 *
 * The previous site used React plus the `motion` library for these five effects. They are all
 * view-level and need no server round-trip, so here they are a single bundled script and the
 * page renders complete without it — the observers only ever *remove* a starting state.
 *
 * Reduced motion mirrors the old `<MotionConfig reducedMotion="user">`: cross-fades survive,
 * movement and counting do not.
 */

const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

/** Runs `onEnter` the first time each element crosses into view, then stops watching it. */
function observeOnce(
  elements: Iterable<Element>,
  threshold: number,
  onEnter: (element: Element) => void,
): void {
  const observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (!entry.isIntersecting) continue;
        observer.unobserve(entry.target);
        onEnter(entry.target);
      }
    },
    { threshold },
  );

  for (const element of elements) {
    observer.observe(element);
  }
}

/**
 * The signature: section headings arrive at `wdth` 85 and settle to 100.
 * The morph itself is the `font-variation-settings` transition in globals.css; this only
 * drops the compressed class at the right moment.
 */
function initHeadingWidthMorph(): void {
  const headings = document.querySelectorAll('.display-animated.display-compressed');

  if (prefersReducedMotion) {
    headings.forEach((heading) => heading.classList.remove('display-compressed'));
    return;
  }

  observeOnce(headings, 0.4, (heading) => heading.classList.remove('display-compressed'));
}

/** Blocks fade and rise into place once, in document order within a stagger container. */
function initScrollReveals(): void {
  const staggerContainers = document.querySelectorAll<HTMLElement>('[data-stagger]');

  for (const container of staggerContainers) {
    const step = Number(container.dataset.stagger) || 70;
    Array.from(container.children).forEach((child, index) => {
      if (child instanceof HTMLElement) {
        child.style.transitionDelay = `${index * step}ms`;
      }
    });
  }

  observeOnce(document.querySelectorAll('.reveal'), 0.2, (element) =>
    element.classList.add('reveal-in'),
  );
}

/** Counts a figure up to its target value when it scrolls into view. */
function initCountUp(): void {
  const counters = document.querySelectorAll<HTMLElement>('[data-count-to]');

  const settle = (counter: HTMLElement) => {
    counter.textContent = String(Number(counter.dataset.countTo) || 0);
  };

  if (prefersReducedMotion) {
    counters.forEach(settle);
    return;
  }

  observeOnce(counters, 0.4, (element) => {
    const counter = element as HTMLElement;
    const target = Number(counter.dataset.countTo) || 0;
    const durationMs = 1300;
    const start = performance.now();

    const tick = (now: number) => {
      const progress = Math.min((now - start) / durationMs, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      counter.textContent = String(Math.round(target * eased));
      if (progress < 1) requestAnimationFrame(tick);
    };

    requestAnimationFrame(tick);
  });
}

/** The fixed nav goes from transparent to a solid bar once the page has scrolled. */
function initNavOnScroll(): void {
  const nav = document.querySelector('.site-nav');
  if (!nav) return;

  let ticking = false;

  const update = () => {
    nav.classList.toggle('nav-scrolled', window.scrollY > 24);
    ticking = false;
  };

  const onScroll = () => {
    if (ticking) return;
    ticking = true;
    requestAnimationFrame(update);
  };

  update();
  window.addEventListener('scroll', onScroll, { passive: true });
}

/** Hamburger toggle for the small-screen nav panel. */
function initMobileMenu(): void {
  const toggle = document.querySelector<HTMLButtonElement>('[data-menu-toggle]');
  const panel = document.querySelector<HTMLElement>('[data-menu-panel]');
  if (!toggle || !panel) return;

  const setOpen = (open: boolean) => {
    toggle.setAttribute('aria-expanded', String(open));
    panel.classList.toggle('hidden', !open);
  };

  setOpen(false);

  toggle.addEventListener('click', () => {
    setOpen(toggle.getAttribute('aria-expanded') !== 'true');
  });

  // Following an in-page anchor should close the panel behind you.
  panel.addEventListener('click', (event) => {
    if ((event.target as HTMLElement).closest('a')) setOpen(false);
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') setOpen(false);
  });
}

function init(): void {
  initHeadingWidthMorph();
  initScrollReveals();
  initCountUp();
  initNavOnScroll();
  initMobileMenu();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', init, { once: true });
} else {
  init();
}
