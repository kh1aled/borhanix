document.addEventListener('DOMContentLoaded', () => {
    if (typeof lucide !== 'undefined') lucide.createIcons();

    document.querySelectorAll('[data-toast]').forEach((toast, i) => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(20px)';
        toast.style.transition = 'all 0.3s ease';

        setTimeout(() => {
            toast.style.opacity = '1';
            toast.style.transform = 'translateX(0)';
        }, i * 100);

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(20px)';
            setTimeout(() => toast.remove(), 300);
        }, 4000 + i * 100);
    });

    function showToast(type, message) {
        const container = document.getElementById('toast-container');
        const colors = {
            success: { border: 'border-emerald-200', icon: 'check-circle', text: 'text-emerald-500' },
            error: { border: 'border-red-200', icon: 'x-circle', text: 'text-red-500' },
            warning: { border: 'border-amber-200', icon: 'alert-triangle', text: 'text-amber-500' },
            info: { border: 'border-blue-200', icon: 'info', text: 'text-primary' },
        };
        const c = colors[type] || colors.info;

        const toast = document.createElement('div');
        toast.className = `toast-item flex items-start gap-3 rounded-2xl px-4 py-3 shadow-lg border bg-white dark:bg-slate-900 dark:border-slate-800 ${c.border}`;
        toast.innerHTML = `
        <span class="mt-0.5 shrink-0 ${c.text}"><i data-lucide="${c.icon}" class="w-5 h-5"></i></span>
        <p class="text-sm text-slate-700 dark:text-slate-200 flex-1">${message}</p>
        <button onclick="this.closest('.toast-item').remove()" class="text-slate-400 hover:text-slate-600">
            <i data-lucide="x" class="w-4 h-4"></i>
        </button>`;

        container.appendChild(toast);
        lucide.createIcons();

        setTimeout(() => toast.remove(), 4000);
    }
});