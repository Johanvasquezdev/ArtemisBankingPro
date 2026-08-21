document.addEventListener('DOMContentLoaded', () => {
    const transitionOut = async (main) => {
        if (!main) return;

        const motion = window.ArtemisMotion;
        const anime = motion?.getAnime?.();
        if (!anime) {
            main.style.opacity = '0';
            main.style.transform = 'translateY(-10px)';
            await new Promise((resolve) => setTimeout(resolve, motion?.prefersReducedMotion?.() ? 0 : 150));
            return;
        }

        await anime({
            targets: main,
            opacity: [1, 0],
            translateY: [0, -8],
            duration: 150,
            easing: 'easeInQuad'
        }).finished;
    };

    const transitionIn = () => {
        window.ArtemisMotion?.animatePageEnter?.();
    };

    // Enable SPA-like navigation for sidebar links
    document.addEventListener('click', async (e) => {
        const link = e.target instanceof Element ? e.target.closest('a') : null;
        
        // Only intercept links within the sidebar or topbar that point to the same origin
        if (!link || 
            (!link.closest('.abp-sidebar-nav') && !link.closest('.abp-topbar-nav')) || 
            link.target === '_blank' || 
            link.origin !== location.origin ||
            link.getAttribute('href') === '#' ||
            link.getAttribute('asp-action') === 'Logout' ||
            link.href.includes('/Logout')) {
            return;
        }

        e.preventDefault();
        const url = link.href;

        // Update active class immediately for UI feedback
        document.querySelectorAll('.abp-sidebar-nav a, .abp-topbar-nav a').forEach(el => el.classList.remove('active'));
        link.classList.add('active');

        const currentMain = document.querySelector('.abp-main');
        try {
            // Start loading while the current view transitions out.
            const responsePromise = fetch(url);
            await transitionOut(currentMain);
            const response = await responsePromise;
            if (!response.ok) {
                window.location.href = url;
                return;
            }
            
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const newMain = doc.querySelector('.abp-main');
            
            if (newMain && currentMain) {
                currentMain.replaceChildren(...Array.from(newMain.childNodes, node => document.importNode(node, true)));
                document.title = doc.title;
                window.history.pushState({ path: url }, '', url);
                transitionIn();
            } else {
                window.location.href = url;
            }
        } catch (error) {
            console.error('SPA navigation failed:', error);
            window.location.href = url;
        }
    });

    // Handle browser back/forward buttons
    window.addEventListener('popstate', async () => {
        const url = location.href;
        try {
            const response = await fetch(url);
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            
            const newMain = doc.querySelector('.abp-main');
            const currentMain = document.querySelector('.abp-main');
            
            if (newMain && currentMain) {
                currentMain.replaceChildren(...Array.from(newMain.childNodes, node => document.importNode(node, true)));
                document.title = doc.title;
                transitionIn();

                // Restore active state in sidebar
                document.querySelectorAll('.abp-sidebar-nav a, .abp-topbar-nav a').forEach(link => {
                    if (link.href === url) {
                        link.classList.add('active');
                    } else {
                        link.classList.remove('active');
                    }
                });
            } else {
                window.location.reload();
            }
        } catch (error) {
            window.location.reload();
        }
    });
});
