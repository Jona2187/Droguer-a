// Drogueria - Interacciones del Dashboard, Mini Sidebar y Modo Oscuro

document.addEventListener('DOMContentLoaded', function () {
    const appLayout = document.querySelector('.app-layout');
    const sidebar = document.getElementById('appSidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const mobileSidebarToggle = document.getElementById('mobileSidebarToggle');
    const backdrop = document.getElementById('sidebarBackdrop');
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const themeIcon = document.getElementById('themeIcon');

    // =========================================================================
    // 1. MODO OSCURO / MODO CLARO (Theme Switcher)
    // =========================================================================
    function applyTheme(theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        localStorage.setItem('app_theme', theme);

        if (themeIcon) {
            if (theme === 'dark') {
                themeIcon.classList.remove('bi-moon-stars');
                themeIcon.classList.add('bi-sun');
                if (themeToggleBtn) themeToggleBtn.title = 'Cambiar a modo claro';
            } else {
                themeIcon.classList.remove('bi-sun');
                themeIcon.classList.add('bi-moon-stars');
                if (themeToggleBtn) themeToggleBtn.title = 'Cambiar a modo oscuro';
            }
        }
    }

    // Inicializar icono según el tema actual guardado en localStorage o aplicado en <head>
    const savedTheme = localStorage.getItem('app_theme') || document.documentElement.getAttribute('data-bs-theme') || 'light';
    applyTheme(savedTheme);

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', function () {
            const activeTheme = document.documentElement.getAttribute('data-bs-theme') || 'light';
            const newTheme = activeTheme === 'dark' ? 'light' : 'dark';
            applyTheme(newTheme);
        });
    }

    // =========================================================================
    // 2. MINI SIDEBAR (Colapsar a solo iconos) Y MÓVIL
    // =========================================================================
    function updateToggleTitle() {
        if (!sidebarToggle) return;
        if (appLayout && appLayout.classList.contains('sidebar-collapsed')) {
            sidebarToggle.title = 'Expandir barra lateral';
        } else {
            sidebarToggle.title = 'Colapsar barra lateral a iconos';
        }
    }

    // Restaurar estado del sidebar en escritorio
    if (appLayout && window.innerWidth >= 992) {
        const isCollapsed = localStorage.getItem('sidebar_collapsed') === 'true';
        if (isCollapsed) {
            appLayout.classList.add('sidebar-collapsed');
        }
        updateToggleTitle();
    }

    // Función de alternancia para escritorio
    function toggleSidebar() {
        if (window.innerWidth >= 992) {
            if (appLayout) {
                appLayout.classList.toggle('sidebar-collapsed');
                const isCollapsed = appLayout.classList.contains('sidebar-collapsed');
                localStorage.setItem('sidebar_collapsed', isCollapsed);
                updateToggleTitle();
            }
        } else {
            // Modo Móvil
            if (sidebar && backdrop) {
                sidebar.classList.toggle('show');
                backdrop.classList.toggle('show');
            }
        }
    }

    // Botón flotante en el borde divisorio (Edge Toggle)
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.preventDefault();
            toggleSidebar();
        });
    }

    // Botón hamburguesa para móviles
    if (mobileSidebarToggle) {
        mobileSidebarToggle.addEventListener('click', function (e) {
            e.preventDefault();
            if (sidebar && backdrop) {
                sidebar.classList.add('show');
                backdrop.classList.add('show');
            }
        });
    }

    // Cerrar al hacer clic en el backdrop (móvil)
    if (backdrop) {
        backdrop.addEventListener('click', function () {
            if (sidebar) sidebar.classList.remove('show');
            backdrop.classList.remove('show');
        });
    }

    // =========================================================================
    // 3. SUBMENÚS Y DROPDOWNS
    // =========================================================================
    const navToggles = document.querySelectorAll('[data-nav-toggle]');
    navToggles.forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            // Si el sidebar está en modo mini y se hace clic en un grupo, primero expandir
            if (appLayout && appLayout.classList.contains('sidebar-collapsed') && window.innerWidth >= 992) {
                appLayout.classList.remove('sidebar-collapsed');
                localStorage.setItem('sidebar_collapsed', 'false');
                updateToggleTitle();
            }

            const group = this.closest('[data-nav-group]');
            if (group) {
                group.classList.toggle('is-open');
            }
        });
    });

    // Menú de Perfil
    const profileBtn = document.getElementById('profileDropdownBtn');
    const profileMenu = document.getElementById('profileDropdownMenu');

    if (profileBtn && profileMenu) {
        profileBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            profileMenu.classList.toggle('show');
        });

        document.addEventListener('click', function (e) {
            if (!profileMenu.contains(e.target) && !profileBtn.contains(e.target)) {
                profileMenu.classList.remove('show');
            }
        });
    }
});
