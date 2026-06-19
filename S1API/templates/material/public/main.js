const setupAffixActiveLinks = () => {
  const affix = document.querySelector('#affix');
  if (!affix || affix.dataset.s1ActiveTracking === 'true') {
    return Boolean(affix);
  }

  const affixLinks = [...affix.querySelectorAll('a[href^="#"]')];
  if (affixLinks.length === 0) {
    return false;
  }

  const headingById = new Map(
    [...document.querySelectorAll('article h2[id], article h3[id]')]
      .map((heading) => [heading.id, heading])
  );
  const trackedLinks = affixLinks
    .map((link) => {
      const id = decodeURIComponent(link.getAttribute('href').slice(1));
      return { link, heading: headingById.get(id) };
    })
    .filter((item) => item.heading);

  if (trackedLinks.length === 0) {
    return false;
  }

  const applyActiveLink = (current) => {
    for (const item of trackedLinks) {
      item.link.classList.toggle('active', item === current);
      if (item === current) {
        item.link.setAttribute('aria-current', 'true');
      } else {
        item.link.removeAttribute('aria-current');
      }
    }
  };

  const activateHashLink = () => {
    if (!window.location.hash) {
      return false;
    }

    const current = trackedLinks.find((item) => item.link.getAttribute('href') === window.location.hash);
    if (!current) {
      return false;
    }

    applyActiveLink(current);
    return true;
  };

  const updateActiveLink = () => {
    const offset = 120;
    let current = trackedLinks[0];

    for (const item of trackedLinks) {
      if (item.heading.getBoundingClientRect().top <= offset) {
        current = item;
      } else {
        break;
      }
    }

    applyActiveLink(current);
  };

  affix.dataset.s1ActiveTracking = 'true';
  if (!activateHashLink()) {
    updateActiveLink();
  }
  document.addEventListener('scroll', updateActiveLink, { passive: true });
  window.addEventListener('resize', updateActiveLink);
  window.addEventListener('hashchange', () => window.setTimeout(activateHashLink, 80));

  return true;
};

const scheduleAffixActiveLinks = () => {
  const delays = [80, 250, 600, 1200];
  for (const delay of delays) {
    window.setTimeout(setupAffixActiveLinks, delay);
  }
};

const preventTocGroupNavigation = () => {
  document.addEventListener('click', (event) => {
    const target = event.target instanceof Element
      ? event.target.closest('.toc li.expander > a[href="#"]')
      : null;

    if (target) {
      event.preventDefault();
    }
  }, true);
};

if (typeof window !== 'undefined') {
  preventTocGroupNavigation();

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', scheduleAffixActiveLinks, { once: true });
  } else {
    scheduleAffixActiveLinks();
  }
}

export default {
  defaultTheme: 'dark',
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/ifBars/S1API',
      title: 'GitHub'
    }
  ],
  anchors: {
    level: [2, 3]
  },
  start: scheduleAffixActiveLinks
};
