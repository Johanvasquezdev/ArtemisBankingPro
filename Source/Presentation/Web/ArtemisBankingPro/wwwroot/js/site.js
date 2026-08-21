(() => {
    const storageKey = 'artemis-theme';
    const motionDuration = 300;

    const prefersReducedMotion = () =>
        window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;

    const getAnime = () => {
        if (prefersReducedMotion() || typeof window.anime !== 'function') return null;
        return window.anime;
    };

    const clearMotionStyles = (elements) => {
        elements.forEach((element) => {
            element.style.removeProperty('opacity');
            element.style.removeProperty('transform');
        });
    };

    const getMotionTargets = (root = document) => {
        const main = root.querySelector('.abp-main') || root.querySelector('main');
        if (!main) return { main: null, panels: [], alerts: [] };

        return {
            main,
            panels: [...main.querySelectorAll(
                '.abp-panel, .abp-kpi-card, .abp-quick-action, .access-denied-card'
            )],
            alerts: [...main.querySelectorAll('[role="alert"], .abp-alert, .abp-alert-danger, .abp-alert-warning')]
        };
    };

    const animatePageEnter = (root = document) => {
        const { main, panels, alerts } = getMotionTargets(root);
        if (!main) return;

        const anime = getAnime();
        if (!anime) {
            clearMotionStyles([main, ...panels, ...alerts]);
            document.body.classList.add('page-motion-ready');
            return;
        }

        document.body.classList.add('anime-motion-ready');
        document.body.classList.remove('page-motion-ready', 'page-motion-leaving');
        anime.remove([main, ...panels, ...alerts]);

        anime({
            targets: main,
            opacity: [0, 1],
            translateY: [10, 0],
            duration: motionDuration,
            easing: 'easeOutCubic'
        });

        if (panels.length) {
            anime({
                targets: panels,
                opacity: [0, 1],
                translateY: [14, 0],
                duration: 360,
                delay: anime.stagger(45, { start: 70 }),
                easing: 'easeOutCubic'
            });
        }

        if (alerts.length) {
            anime({
                targets: alerts,
                opacity: [0, 1],
                translateY: [-8, 0],
                duration: 260,
                delay: 80,
                easing: 'easeOutQuad'
            });
        }
    };

    const animatePageLeave = () => {
        if (!getAnime()) {
            document.body.classList.add('page-motion-leaving');
        }
    };

    const animateThemeToggle = (toggle) => {
        const anime = getAnime();
        if (!anime) return;

        anime({
            targets: toggle,
            scale: [0.88, 1],
            rotate: [prefersReducedMotion() ? 0 : -12, 0],
            duration: 260,
            easing: 'easeOutBack'
        });
    };

    const applyTheme = (theme) => {
        const isDark = theme === 'dark';
        document.documentElement.dataset.theme = isDark ? 'dark' : 'light';
        document.body.dataset.theme = isDark ? 'dark' : 'light';

        document.querySelectorAll('[data-access-denied]').forEach((page) => {
            page.dataset.theme = isDark ? 'dark' : 'light';
        });

        document.querySelectorAll('[data-theme-toggle]').forEach((toggle) => {
            toggle.setAttribute('aria-pressed', String(isDark));
            toggle.setAttribute('aria-label', isDark ? 'Activar modo claro' : 'Activar modo oscuro');
            toggle.querySelector('[data-theme-icon="light"]')?.classList.toggle('hidden', isDark);
            toggle.querySelector('[data-theme-icon="dark"]')?.classList.toggle('hidden', !isDark);
            animateThemeToggle(toggle);
        });
    };

    const initializeTheme = () => {
        const savedTheme = localStorage.getItem(storageKey);
        applyTheme(savedTheme === 'light' ? 'light' : 'dark');

        document.querySelectorAll('[data-theme-toggle]').forEach((toggle) => {
            toggle.addEventListener('click', () => {
                const nextTheme = document.body.dataset.theme === 'dark' ? 'light' : 'dark';
                localStorage.setItem(storageKey, nextTheme);
                applyTheme(nextTheme);
            });
        });
    };

    const initializePageMotion = () => {
        animatePageEnter();

        document.addEventListener('click', (event) => {
            const link = event.target.closest('a[href]');
            if (!link || event.defaultPrevented || link.target === '_blank' || link.hasAttribute('download')) return;
            if (link.origin !== window.location.origin || link.getAttribute('href')?.startsWith('#')) return;
            if (link.closest('.abp-sidebar-nav, .abp-topbar-nav')) return;

            animatePageLeave();
        }, { capture: true });
    };

    const initializeFormLoading = () => {
        document.addEventListener('submit', (event) => {
            const form = event.target;
            if (!(form instanceof HTMLFormElement) ||
                (!form.matches('form[data-loading-form]') && !form.matches('form[method="post"]'))) {
                return;
            }

            if (form.dataset.submitting === 'true') {
                event.preventDefault();
                return;
            }

            form.dataset.submitting = 'true';
            form.setAttribute('aria-busy', 'true');
            form.classList.add('is-loading');

            const loadingText = form.dataset.loadingText || 'Procesando...';
            form.querySelectorAll('[data-submit-text]').forEach((label) => {
                label.textContent = loadingText;
            });

            const buttons = [...form.querySelectorAll('button[type="submit"], input[type="submit"]')];
            buttons.forEach((button) => {
                button.disabled = true;
                button.classList.add('is-loading');
                button.setAttribute('aria-busy', 'true');
            });

            const anime = getAnime();
            if (anime && buttons.length) {
                anime({
                    targets: buttons,
                    opacity: [1, 0.72, 1],
                    duration: 900,
                    easing: 'easeInOutSine',
                    loop: true
                });
            }
        }, { capture: true });
    };

    window.ArtemisMotion = Object.freeze({
        animatePageEnter,
        animatePageLeave,
        getAnime,
        prefersReducedMotion
    });

    const initialize = () => {
        initializeTheme();
        initializePageMotion();
        initializeFormLoading();
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize, { once: true });
    } else {
        initialize();
    }
})();
