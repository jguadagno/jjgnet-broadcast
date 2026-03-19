document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('time[data-local-time]').forEach(function (el) {
        var datetime = el.getAttribute('datetime');
        if (datetime) {
            var d = new Date(datetime);
            if (!isNaN(d.getTime())) {
                var isDateOnly = el.getAttribute('data-local-time') === 'date';
                el.textContent = new Intl.DateTimeFormat(undefined, isDateOnly
                    ? { dateStyle: 'medium' }
                    : { dateStyle: 'medium', timeStyle: 'short' }
                ).format(d);
                // Provide a UTC tooltip so users can always reference the exact stored value.
                el.title = 'UTC: ' + d.toISOString().replace('T', ' ').replace(/\.\d+Z$/, ' Z');
            }
        }
    });
});
