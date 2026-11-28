// sua: kiem tra du thong tin (ke ca 4h-30 ngay) truoc khi gui yeu cau thue truc tiep
(() => {
    const mainImage = document.getElementById('vehicle-main-image');
    const thumbnails = document.querySelectorAll('[data-gallery-image]');
    for (const thumbnail of thumbnails) {
        thumbnail.addEventListener('click', () => {
            mainImage.src = thumbnail.dataset.galleryImage;
            for (const button of thumbnails)
                button.setAttribute('aria-pressed', String(button === thumbnail));
        });
    }

    // sua kiem tra du thong tin truoc khi gui yeu cau thue
    const bookNowBtn = document.getElementById('btn-book-now');
    const warningEl = document.getElementById('booking-warning');
    if (bookNowBtn) {
        const showWarning = (msg, focusId) => {
            if (warningEl) {
                warningEl.textContent = msg;
                warningEl.hidden = false;
            }
            if (focusId) document.getElementById(focusId)?.focus();
        };
        const hideWarning = () => {
            if (warningEl) warningEl.hidden = true;
        };

        for (const fieldId of ['pickupDate', 'pickupTime', 'returnDate', 'returnTime', 'pickupLocationId']) {
            const el = document.getElementById(fieldId);
            if (el) {
                el.addEventListener('input', hideWarning);
                el.addEventListener('change', hideWarning);
            }
        }

        bookNowBtn.addEventListener('click', event => {
            // Sync/validate the visible member date inputs before reading hidden POST values.
            const validation = new Event('rental:validate', { cancelable: true });
            document.getElementById('rental-search').dispatchEvent(validation);
            if (validation.defaultPrevented) { event.preventDefault(); return; }
            const pickupAt = document.getElementById('pickupAt')?.value;
            const returnAt = document.getElementById('returnAt')?.value;
            const pickupLoc = document.getElementById('pickupLocationId')?.value;

            if (!pickupAt) {
                event.preventDefault();
                showWarning('Vui lòng nhập đầy đủ ngày giờ nhận xe.', 'pickupDate');
                return;
            }
            if (!returnAt) {
                event.preventDefault();
                showWarning('Vui lòng nhập đầy đủ ngày giờ trả xe.', 'returnDate');
                return;
            }
            if (!pickupLoc) {
                event.preventDefault();
                showWarning('Vui lòng chọn địa điểm nhận xe.', 'pickupLocationId');
                return;
            }

            const start = new Date(pickupAt + '+07:00');
            const end = new Date(returnAt + '+07:00');
            const parts = new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date());
            const fields = Object.fromEntries(parts.map(p => [p.type, p.value]));
            const today = `${fields.year}-${fields.month}-${fields.day}`;
            if (pickupAt.slice(0, 10) <= today) {
                event.preventDefault();
                showWarning('Ngày nhận xe phải sau ngày hôm nay.', 'pickupDate');
                return;
            }
            if (end <= start) {
                event.preventDefault();
                showWarning('Ngày giờ trả phải sau ngày giờ nhận xe.', 'returnDate');
                return;
            }
            const hours = (end - start) / (1000 * 60 * 60);
            if (hours < 4 || hours > 30 * 24) {
                event.preventDefault();
                showWarning('Thời gian thuê cần từ 4 giờ đến 30 ngày.', 'returnDate');
                return;
            }

            hideWarning();
        });
    }
    //
})();

