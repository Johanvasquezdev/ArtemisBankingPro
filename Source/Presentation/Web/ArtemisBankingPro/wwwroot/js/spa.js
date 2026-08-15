document.addEventListener('DOMContentLoaded', () => {
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
        if (currentMain) {
            currentMain.style.transition = 'opacity 0.2s ease-out, transform 0.2s ease-out';
            currentMain.style.opacity = '0';
            currentMain.style.transform = 'translateY(-10px)';
        }

        try {
            // Fetch the new page
            const response = await fetch(url);
            if (!response.ok) {
                window.location.href = url;
                return;
            }
            
            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            const newMain = doc.querySelector('.abp-main');
            
            if (newMain && currentMain) {
                // Wait for fade out to finish
                setTimeout(() => {
                    currentMain.replaceChildren(...Array.from(newMain.childNodes, node => document.importNode(node, true)));
                    document.title = doc.title;
                    window.history.pushState({ path: url }, '', url);
                    
                    // Fade in
                    currentMain.style.opacity = '1';
                    currentMain.style.transform = 'translateY(0)';
                }, 200);
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
