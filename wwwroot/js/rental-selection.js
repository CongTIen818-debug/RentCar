(() => {
    for (const form of document.querySelectorAll('.selection-driver-form')) {
        const toggle = form.querySelector('input[type="checkbox"]');
        if (!toggle || toggle.disabled) continue;
        const button = form.querySelector('button[type="submit"]');
        if (button) button.hidden = true;
        toggle.addEventListener('change', () => form.requestSubmit());
    }
})();
