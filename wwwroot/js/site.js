const savedTheme = localStorage.getItem("depi-theme");
if (savedTheme === "dark") {
    document.documentElement.dataset.theme = "dark";
}

function refreshIcons() {
    if (window.lucide) {
        window.lucide.createIcons();
    }
}

refreshIcons();

document.getElementById("themeToggle")?.addEventListener("click", () => {
    const isDark = document.documentElement.dataset.theme === "dark";
    document.documentElement.dataset.theme = isDark ? "light" : "dark";
    localStorage.setItem("depi-theme", isDark ? "light" : "dark");
    refreshIcons();
});

document.addEventListener("submit", (event) => {
    const button = event.target.querySelector("button[type='submit']");
    if (!button || button.dataset.noBusy === "true") {
        return;
    }

    button.dataset.originalText = button.innerHTML;
    button.setAttribute("aria-busy", "true");
});
