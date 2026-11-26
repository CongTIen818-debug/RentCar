(() => {
    const toggle = document.querySelector('[data-driver-toggle]');
    const total = document.querySelector('[data-driver-total]');
    if (!toggle || !total) return;
    const format = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' });
    function update() {
        total.textContent = format.format(Number(total.dataset.carRate) + (toggle.checked ? 1000000 : 0));
    }
    toggle.addEventListener('change', update);
    update();
})();
