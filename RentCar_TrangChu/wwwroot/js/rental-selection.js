(() => {
    const requestForm = document.querySelector('[data-rental-request-form]');
    const scheduleForm = document.getElementById('rental-search');
    requestForm?.addEventListener('submit', event => {
        if (!scheduleForm) return;
        const valid = scheduleForm.dispatchEvent(new Event('rental:validate', { cancelable: true }));
        if (!valid || !scheduleForm.reportValidity()) { event.preventDefault(); return; }
        for (const name of ['pickupAt', 'returnAt', 'pickupLocationId']) {
            requestForm.elements.namedItem(name).value = scheduleForm.elements.namedItem(name).value;
        }
    });
    for (const form of document.querySelectorAll('.selection-driver-form')) {
        const toggle = form.querySelector('input[type="checkbox"]');
        if (!toggle || toggle.disabled) continue;
        const button = form.querySelector('button[type="submit"]');
        if (button) button.hidden = true;
        toggle.addEventListener('change', () => form.requestSubmit());
    }
})();
