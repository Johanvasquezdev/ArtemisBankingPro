(() => {
    const storageKey = 'artemis-theme';

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
        });
    };

    const initializeTheme = () => {
        const savedTheme = localStorage.getItem(storageKey);
        applyTheme(savedTheme === 'dark' ? 'dark' : 'light');

        document.querySelectorAll('[data-theme-toggle]').forEach((toggle) => {
            toggle.addEventListener('click', () => {
                const nextTheme = document.body.dataset.theme === 'dark' ? 'light' : 'dark';
                localStorage.setItem(storageKey, nextTheme);
                applyTheme(nextTheme);
            });
        });
    };

    const initializePageMotion = () => {
        document.body.classList.add('page-motion-ready');

        document.addEventListener('click', (event) => {
            const link = event.target.closest('a[href]');
            if (!link || event.defaultPrevented || link.target === '_blank' || link.hasAttribute('download')) return;
            if (link.origin !== window.location.origin || link.getAttribute('href')?.startsWith('#')) return;

            document.body.classList.add('page-motion-leaving');
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

            form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach((button) => {
                button.disabled = true;
                button.classList.add('is-loading');
                button.setAttribute('aria-busy', 'true');
            });
        }, { capture: true });
    };

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
