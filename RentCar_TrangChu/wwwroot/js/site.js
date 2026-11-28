(() => {
    const form = document.getElementById('rental-search');
    if (!form) return;

    const pad = number => String(number).padStart(2, '0');
    function parseDate(value) {
        const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value);
        if (!match) return null;
        const day = Number(match[1]);
        const month = Number(match[2]);
        const year = Number(match[3]);
        if (year < 1900 || year > 9999) return null;
        const date = new Date(year, month - 1, day);
        return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
            ? date : null;
    }

    function earliestDate(pickup) {
        const parts = new Intl.DateTimeFormat('en-GB', {
            timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit'
        }).formatToParts(new Date());
        const values = Object.fromEntries(parts.map(part => [part.type, part.value]));
        const tomorrow = new Date(Number(values.year), Number(values.month) - 1, Number(values.day) + 1);
        const chosenPickup = parseDate(document.getElementById('pickupDate').value);
        return !pickup && chosenPickup && chosenPickup > tomorrow ? chosenPickup : tomorrow;
    }
    const pickers = Array.from(document.querySelectorAll('[data-date-picker]')).map(picker => {
        const dateInput = picker.querySelector('input[type="text"]');
        const timeInput = picker.querySelectorAll('input[type="text"]')[1];
        const hiddenInput = picker.querySelector('input[type="hidden"]');
        const calendar = picker.querySelector('.rental-calendar');
        const isPickup = dateInput.id === 'pickupDate';
        let month = parseDate(dateInput.value) || earliestDate(isPickup);

        function close() {
            calendar.hidden = true;
            dateInput.setAttribute('aria-expanded', 'false');
            picker.querySelector('.calendar-toggle')?.setAttribute('aria-expanded', 'false');
        }

        function render() {
            calendar.replaceChildren();
            const heading = document.createElement('div');
            heading.className = 'calendar-heading';
            const title = document.createElement('span');
            title.setAttribute('aria-live', 'polite');
            title.textContent = 'Tháng ' + (month.getMonth() + 1) + ' / ' + month.getFullYear();
            for (const direction of [-1, 1]) {
                const button = document.createElement('button');
                button.type = 'button';
                button.textContent = direction === -1 ? '‹' : '›';
                button.setAttribute('aria-label', direction === -1 ? 'Tháng trước' : 'Tháng sau');
                button.addEventListener('click', () => {
                    month = new Date(month.getFullYear(), month.getMonth() + direction, 1);
                    render();
                    calendar.querySelector(direction === -1
                        ? '.calendar-heading button:first-child' : '.calendar-heading button:last-child').focus();
                });
                heading.append(button);
                if (direction === -1) heading.append(title);
            }
            calendar.append(heading);
            const grid = document.createElement('div');
            grid.className = 'calendar-days';
            for (const day of ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']) {
                const label = document.createElement('span');
                label.textContent = day;
                grid.append(label);
            }
            const first = new Date(month.getFullYear(), month.getMonth(), 1);
            const offset = (first.getDay() + 6) % 7;
            for (let index = 0; index < offset; index++) grid.append(document.createElement('span'));
            const total = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
            const selected = parseDate(dateInput.value);
            for (let day = 1; day <= total; day++) {
                const button = document.createElement('button');
                button.type = 'button';
                button.textContent = day;
                const date = new Date(month.getFullYear(), month.getMonth(), day);
                button.disabled = date < earliestDate(isPickup);
                button.setAttribute('aria-label', date.toLocaleDateString('vi-VN'));
                button.setAttribute('aria-pressed', String(selected?.getTime() === date.getTime()));
                button.addEventListener('click', () => {
                    dateInput.value = pad(day) + '/' + pad(month.getMonth() + 1) + '/' + month.getFullYear();
                    dateInput.dispatchEvent(new Event('input', { bubbles: true }));
                    close();
                    dateInput.focus();
                });
                grid.append(button);
            }
            calendar.append(grid);
        }

        function open() {
            const selectedDate = parseDate(dateInput.value);
            const minimum = earliestDate(isPickup);
            month = selectedDate && selectedDate >= minimum ? selectedDate : minimum;
            render();
            calendar.hidden = false;
            dateInput.setAttribute('aria-expanded', 'true');
            picker.querySelector('.calendar-toggle')?.setAttribute('aria-expanded', 'true');
        }
        dateInput.addEventListener('click', open);
        picker.querySelector('.calendar-toggle')?.addEventListener('click', () => calendar.hidden ? open() : close());
        dateInput.addEventListener('keydown', event => {
            if (event.altKey && event.key === 'ArrowDown') {
                event.preventDefault();
                open();
                calendar.querySelector('.calendar-days button[aria-pressed="true"]:not(:disabled), .calendar-days button:not(:disabled)').focus();
            }
        });
        dateInput.addEventListener('input', () => {
            dateInput.setCustomValidity('');
            const date = parseDate(dateInput.value);
            if (date && !calendar.hidden) {
                month = date;
                render();
            }
        });
        timeInput.addEventListener('input', () => timeInput.setCustomValidity(''));
        picker.addEventListener('keydown', event => {
            if (event.key === 'Escape') {
                close();
                dateInput.focus();
            }
        });
        document.addEventListener('click', event => {
            if (!picker.contains(event.target)) close();
        });
        picker.addEventListener('focusout', event => {
            if (!picker.contains(event.relatedTarget)) close();
        });

        function sync(required) {
            const date = parseDate(dateInput.value);
            const timeValid = /^([01]\d|2[0-3]):[0-5]\d$/.test(timeInput.value);
            dateInput.setCustomValidity(date || (!required && !dateInput.value)
                ? '' : 'Nhập ngày hợp lệ theo dd/mm/yyyy.');
            if (date && date < earliestDate(isPickup)) {
                dateInput.setCustomValidity(isPickup
                    ? 'Ngày nhận xe phải sau ngày hôm nay.'
                    : 'Ngày trả không được trước ngày nhận xe.');
            }
            timeInput.setCustomValidity(timeValid ? '' : 'Nhập giờ từ 00:00 đến 23:59 theo HH:mm.');
            hiddenInput.value = date && timeValid
                ? date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate()) + 'T' + timeInput.value
                : '';
            return dateInput.checkValidity() && timeInput.checkValidity();
        }
        return { sync, dateInput, timeInput, hiddenInput };
    });

    form.addEventListener('input', () => {
        for (const picker of pickers) {
            picker.dateInput.setCustomValidity('');
            picker.timeInput.setCustomValidity('');
        }
    });

    function validateSubmission(event) {
        const required = !event.submitter?.formNoValidate || pickers.some(picker => picker.dateInput.value);
        const valid = pickers.map(picker => picker.sync(required)).every(Boolean);
        if (!valid) {
            event.preventDefault();
            form.reportValidity();
            return;
        }
        const [pickup, dropoff] = pickers;
        if (pickup.hiddenInput.value && dropoff.hiddenInput.value &&
            dropoff.hiddenInput.value <= pickup.hiddenInput.value) {
            dropoff.dateInput.setCustomValidity('Ngày giờ trả phải sau ngày giờ nhận.');
            event.preventDefault();
            form.reportValidity();
        }
    }
    form.addEventListener('submit', validateSubmission);
    // A separate POST request form can validate/sync the compact date fields without navigating the GET quote form.
    form.addEventListener('rental:validate', validateSubmission);
})();
