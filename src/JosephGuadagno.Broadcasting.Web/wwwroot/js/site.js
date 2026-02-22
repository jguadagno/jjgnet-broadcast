document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('time[data-local-time]').forEach(function (el) {
        var datetime = el.getAttribute('datetime');
        if (datetime) {
            var d = new Date(datetime);
            if (!isNaN(d.getTime())) {
                el.textContent = el.getAttribute('data-local-time') === 'date'
                    ? d.toLocaleDateString()
                    : d.toLocaleString();
            }
        }
    });
});
