document.addEventListener('DOMContentLoaded', function () {
    // Form loading/submitting state: disable the submit button and show "Saving..." on submit.
    // Re-enables if jQuery Unobtrusive Validation finds client-side errors.
    document.querySelectorAll('form').forEach(function (form) {
        var btn = form.querySelector('[type="submit"]');
        if (!btn) return;

        // Use click handler instead of submit to prevent race condition where multiple
        // clicks can queue multiple submits before the first submit disables the button
        btn.addEventListener('click', function (event) {
            if (btn.disabled) {
                event.preventDefault();
                return;
            }
            
            // Check client-side validation before disabling (if jQuery validation exists)
            if (typeof $ !== 'undefined' && $(form).valid && !$(form).valid()) {
                // Let validation run and show errors, don't disable button
                return;
            }
            
            // Disable immediately to prevent double-click
            btn.dataset.originalHtml = btn.innerHTML;
            btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Saving...';
            btn.disabled = true;
        });

        // jQuery Validate fires 'invalid-form.validate' when client-side validation fails.
        // This handles cases where validation happens after button click (async validation, etc.)
        if (typeof $ !== 'undefined') {
            $(form).on('invalid-form.validate', function () {
                if (btn.dataset.originalHtml) {
                    btn.innerHTML = btn.dataset.originalHtml;
                    delete btn.dataset.originalHtml;
                    btn.disabled = false;
                }
            });
        }
    });

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
