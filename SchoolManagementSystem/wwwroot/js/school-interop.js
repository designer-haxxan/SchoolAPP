// School Management System - JS Interop Helpers

// Chart instances registry
window.chartInstances = {};

// Initialize or update a Chart.js chart
window.initChart = function (canvasId, type, labels, data, label, color, secondData, secondLabel) {
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
    }
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    const datasets = [{
        label: label,
        data: data,
        backgroundColor: color || 'rgba(26, 35, 126, 0.7)',
        borderColor: color || '#1a237e',
        borderWidth: 2,
        fill: type === 'line',
        tension: 0.4
    }];

    if (secondData && secondLabel) {
        datasets.push({
            label: secondLabel,
            data: secondData,
            backgroundColor: 'rgba(38,166,154,0.7)',
            borderColor: '#26a69a',
            borderWidth: 2,
            fill: false,
            tension: 0.4
        });
    }

    window.chartInstances[canvasId] = new Chart(ctx, {
        type: type,
        data: { labels: labels, datasets: datasets },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: datasets.length > 1,
                    position: 'bottom',
                    labels: { font: { size: 11 } }
                }
            },
            scales: type !== 'pie' && type !== 'doughnut' ? {
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(0,0,0,0.05)' },
                    ticks: { font: { size: 11 } }
                },
                x: {
                    grid: { display: false },
                    ticks: { font: { size: 11 } }
                }
            } : {}
        }
    });
};

// Initialize doughnut chart
window.initDoughnutChart = function (canvasId, labels, data, colors) {
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
    }
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    window.chartInstances[canvasId] = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: colors,
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                }
            },
            cutout: '65%'
        }
    });
};

// Print a specific element
window.printElement = function (elementId) {
    var content = document.getElementById(elementId);
    if (!content) { window.print(); return; }
    var original = document.body.innerHTML;
    document.body.innerHTML = content.innerHTML;
    window.print();
    document.body.innerHTML = original;
    window.location.reload();
};

// Download a file from base64
window.downloadFile = function (filename, contentType, base64Data) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = 'data:' + contentType + ';base64,' + base64Data;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Toggle sidebar on mobile
window.toggleSidebar = function () {
    var sidebar = document.getElementById('sidebar');
    var overlay = document.getElementById('sidebarOverlay');
    if (sidebar) sidebar.classList.toggle('open');
    if (overlay) overlay.classList.toggle('show');
};

window.closeSidebar = function () {
    var sidebar = document.getElementById('sidebar');
    var overlay = document.getElementById('sidebarOverlay');
    if (sidebar) sidebar.classList.remove('open');
    if (overlay) overlay.classList.remove('show');
};

// Scroll to top
window.scrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// Show a simple toast notification
window.showToast = function (message, type) {
    var toast = document.createElement('div');
    toast.style.cssText = `
        position: fixed; bottom: 20px; right: 20px; z-index: 9999;
        padding: 12px 20px; border-radius: 8px; color: #fff; font-size: 0.875rem;
        background: ${type === 'success' ? '#43a047' : type === 'error' ? '#e53935' : '#1a237e'};
        box-shadow: 0 4px 12px rgba(0,0,0,0.2); min-width: 200px;
        animation: slideInRight 0.3s ease;
    `;
    toast.textContent = message;
    document.body.appendChild(toast);
    setTimeout(() => { if (toast.parentNode) toast.parentNode.removeChild(toast); }, 3500);
};

// Format currency (PKR)
window.formatCurrency = function (amount) {
    return 'Rs. ' + parseFloat(amount).toLocaleString('en-PK', { minimumFractionDigits: 0 });
};

// ───────────────────────── Count-up numbers ─────────────────────────
// Usage: <span data-countup="12500" data-prefix="Rs. " data-suffix="%"></span>
// Keep the span empty in Blazor markup: this script owns its text, so Blazor's text
// nodes are never replaced. Re-animates whenever data-countup changes.
(function () {
    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var fmt = function (n) { return Math.round(n).toLocaleString('en-PK'); };

    function run(el) {
        var to = parseFloat(el.getAttribute('data-countup')) || 0;
        var prefix = el.getAttribute('data-prefix') || '';
        var suffix = el.getAttribute('data-suffix') || '';
        var from = typeof el._countVal === 'number' ? el._countVal : 0;
        el._countVal = to;

        if (el._countRaf) cancelAnimationFrame(el._countRaf);
        if (reduceMotion || from === to) {
            el.textContent = prefix + fmt(to) + suffix;
            return;
        }

        var start = null, duration = 900;
        var step = function (ts) {
            if (start === null) start = ts;
            var t = Math.min(1, (ts - start) / duration);
            var eased = 1 - Math.pow(1 - t, 3);
            el.textContent = prefix + fmt(from + (to - from) * eased) + suffix;
            if (t < 1) el._countRaf = requestAnimationFrame(step);
        };
        el._countRaf = requestAnimationFrame(step);
    }

    function scan(root) {
        if (!root || root.nodeType !== 1) return;
        if (root.hasAttribute && root.hasAttribute('data-countup')) run(root);
        if (root.querySelectorAll) root.querySelectorAll('[data-countup]').forEach(run);
    }

    document.addEventListener('DOMContentLoaded', function () { scan(document.body); });

    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (m) {
            if (m.type === 'attributes') { run(m.target); return; }
            m.addedNodes.forEach(scan);
        });
    });
    observer.observe(document.documentElement, {
        childList: true, subtree: true,
        attributes: true, attributeFilter: ['data-countup']
    });
})();
