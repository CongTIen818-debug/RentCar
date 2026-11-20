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

    const pickers = Array.from(document.querySelectorAll('[data-date-picker]')).map(picker => {
        const dateInput = picker.querySelector('input[type="text"]');
        const timeInput = picker.querySelectorAll('input[type="text"]')[1];
        const hiddenInput = picker.querySelector('input[type="hidden"]');
        const toggle = picker.querySelector('.calendar-toggle');
        const calendar = picker.querySelector('.rental-calendar');
        let month = parseDate(dateInput.value) || new Date();

        function close() {
            calendar.hidden = true;
            toggle.setAttribute('aria-expanded', 'false');
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
            month = parseDate(dateInput.value) || new Date();
            render();
            calendar.hidden = false;
            toggle.setAttribute('aria-expanded', 'true');
        }
        toggle.addEventListener('click', () => {
            if (calendar.hidden) {
                open();
                calendar.querySelector('.calendar-days button[aria-pressed="true"], .calendar-days button').focus();
            } else close();
        });
        dateInput.addEventListener('click', open);
        dateInput.addEventListener('keydown', event => {
            if (event.altKey && event.key === 'ArrowDown') {
                event.preventDefault();
                open();
                calendar.querySelector('.calendar-days button[aria-pressed="true"], .calendar-days button').focus();
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
                toggle.focus();
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

    form.addEventListener('submit', event => {
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
    });
})();
