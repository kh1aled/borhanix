document.addEventListener('DOMContentLoaded', () => {
    if (window.lucide) lucide.createIcons();

    // ── Mobile menu ───────────────────────────────────────────────
    const mobileMenuButton = document.getElementById('mobileMenuButton');
    const mobileMenuPanel = document.getElementById('mobileMenuPanel');
    const menuIconOpen = document.getElementById('menuIconOpen');
    const menuIconClose = document.getElementById('menuIconClose');

    mobileMenuButton?.addEventListener('click', () => {
        const isOpen = !mobileMenuPanel.classList.contains('hidden');
        mobileMenuPanel.classList.toggle('hidden');
        menuIconOpen.classList.toggle('hidden', !isOpen);
        menuIconClose.classList.toggle('hidden', isOpen);
        mobileMenuButton.setAttribute('aria-expanded', String(!isOpen));
    });

    mobileMenuPanel?.querySelectorAll('a').forEach((link) => {
        link.addEventListener('click', () => {
            mobileMenuPanel.classList.add('hidden');
            menuIconOpen.classList.remove('hidden');
            menuIconClose.classList.add('hidden');
        });
    });

    // ── Theme toggle ──────────────────────────────────────────────
    const themeToggle = document.getElementById('themeToggle');
    const moonIcon = document.getElementById('themeIconMoon'); 
    const sunIcon = document.getElementById('themeIconSun');

    function getStoredTheme() {
        return localStorage.getItem('theme')
            || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    }

    function applyTheme(theme) {
        const isDark = theme === 'dark';

        localStorage.setItem('theme', theme);

        document.documentElement.classList.toggle('dark', isDark);

        moonIcon?.classList.toggle('hidden', isDark);
        sunIcon?.classList.toggle('hidden', !isDark);
    }

    const currentTheme = getStoredTheme();

    moonIcon?.classList.toggle('hidden', currentTheme === 'dark');
    sunIcon?.classList.toggle('hidden', currentTheme !== 'dark');

    themeToggle?.addEventListener('click', () => {
        const isDark = document.documentElement.classList.contains('dark');
        applyTheme(isDark ? 'light' : 'dark');
        lucide.createIcons();
    });

});