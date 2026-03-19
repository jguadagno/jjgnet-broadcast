// Enable Bootstrap tooltips for truncated template preview
document.addEventListener('DOMContentLoaded', function () {
    var tooltipEls = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipEls.forEach(function (el) { new bootstrap.Tooltip(el); });
});
