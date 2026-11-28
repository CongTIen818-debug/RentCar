// Keep the member's estimate fields; get authoritative weekly/promotional pricing from the server.
(() => {
    const toggle = document.querySelector('[data-driver-toggle]');
    const dailyTotal = document.querySelector('[data-driver-total]');
    const summary = document.querySelector('[data-rental-summary]');
    if (!toggle || !dailyTotal || !summary) return;
    const daysEl = summary.querySelector('[data-rental-days]');
    const rentalTotal = summary.querySelector('[data-rental-total]');
    const warning = document.getElementById('booking-warning');
    const format = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' });
    let timer, pending;
    function dateValue(prefix) {
        const date = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(document.getElementById(prefix + 'Date').value);
        const time = document.getElementById(prefix + 'Time').value;
        if (!date || !/^([01]\d|2[0-3]):[0-5]\d$/.test(time)) return null;
        const value = `${date[3]}-${date[2]}-${date[1]}T${time}`;
        const check = new Date(value + '+07:00');
        const wall = new Date(Date.UTC(Number(date[3]), Number(date[2]) - 1, Number(date[1])));
        return Number.isNaN(check.getTime()) || wall.getUTCMonth() !== Number(date[2]) - 1 || wall.getUTCDate() !== Number(date[1]) ? null : value;
    }
    async function update() {
        pending?.abort(); pending = null;
        const daily = Number(dailyTotal.dataset.carRate) + (toggle.checked ? 1000000 : 0);
        dailyTotal.textContent = format.format(daily);
        const pickupAt = dateValue('pickup'), returnAt = dateValue('return');
        if (!pickupAt || !returnAt) {
            daysEl.textContent = '1'; rentalTotal.textContent = format.format(daily); return;
        }
        const controller = new AbortController(); pending = controller;
        const parameters = new URLSearchParams({ id: summary.dataset.carId, pickupAt, returnAt, withDriver: String(toggle.checked) });
        rentalTotal.textContent = '…';
        try {
            const response = await fetch(summary.dataset.quoteUrl + '?' + parameters, { signal: controller.signal, headers: { Accept: 'application/json' } });
            const result = await response.json();
            if (!response.ok) throw new Error(result.message || 'Chưa tính được giá thuê. Vui lòng thử lại.');
            if (controller.signal.aborted) return;
            daysEl.textContent = String(result.days); dailyTotal.textContent = format.format(result.dailyTotal); rentalTotal.textContent = format.format(result.total);
            if (warning?.dataset.quoteError) { warning.hidden = true; delete warning.dataset.quoteError; }
        } catch (error) {
            if (error.name === 'AbortError') return;
            rentalTotal.textContent = '—';
            if (warning) { warning.textContent = error.message; warning.hidden = false; warning.dataset.quoteError = 'true'; }
        }
    }
    function schedule() { pending?.abort(); clearTimeout(timer); timer = setTimeout(update, 180); }
    toggle.addEventListener('change', schedule);
    for (const id of ['pickupDate', 'pickupTime', 'returnDate', 'returnTime']) {
        const field = document.getElementById(id);
        field?.addEventListener('input', schedule); field?.addEventListener('change', schedule);
    }
    update();
})();
