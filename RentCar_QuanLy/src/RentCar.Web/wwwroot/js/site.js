// RentCar · tương tác nhỏ trên trình duyệt. Mọi kiểm tra nghiệp vụ vẫn nằm ở máy chủ (NFR-03).
(function () {
  'use strict';

  // Menu trên điện thoại.
  const toggle = document.querySelector('[data-nav-toggle]');
  const nav = document.getElementById('site-nav');
  if (toggle && nav) {
    toggle.addEventListener('click', function () {
      const open = nav.classList.toggle('is-open');
      toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
  }

  // Trả xe không được sớm hơn nhận xe (BR-02): giới hạn ngay trên ô chọn ngày.
  document.querySelectorAll('form').forEach(function (form) {
    const start = form.querySelector('input[name="startAt"]');
    const end = form.querySelector('input[name="endAt"]');
    if (!start || !end) return;
    const sync = function () { if (start.value) end.min = start.value; };
    start.addEventListener('change', sync);
    sync();
  });

  // Thông báo thành công tự ẩn sau 6 giây; thông báo lỗi giữ nguyên để người dùng kịp đọc.
  document.querySelectorAll('.flash:not(.flash--bad)').forEach(function (el) {
    setTimeout(function () { el.classList.add('is-leaving'); }, 6000);
  });

  // Hỏi xác nhận trước khi gửi form có data-confirm (ví dụ hủy yêu cầu thuê).
  document.querySelectorAll('form[data-confirm]').forEach(function (form) {
    form.addEventListener('submit', function (event) {
      if (!window.confirm(form.getAttribute('data-confirm'))) event.preventDefault();
    });
  });

  // Vùng kéo thả ảnh: hiện số ảnh đã chọn.
  document.querySelectorAll('[data-file-input]').forEach(function (input) {
    const zone = input.closest('.dropzone');
    const status = zone && zone.querySelector('[data-file-status]');
    if (!status) return;
    input.addEventListener('change', function () {
      const n = input.files ? input.files.length : 0;
      status.textContent = n === 0 ? 'Chưa chọn ảnh nào' : 'Đã chọn ' + n + ' ảnh';
    });
  });

  // In hợp đồng, biên nhận và biên bản (v8, #13).
  document.querySelectorAll('[data-print]').forEach(function (btn) {
    btn.addEventListener('click', function () { window.print(); });
  });

  // Sao chép mã đăng ký một lần (trang Mã đăng ký nhân viên).
  document.querySelectorAll('[data-copy]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      const source = document.getElementById(btn.getAttribute('data-copy'));
      if (!source || !navigator.clipboard) return;
      const original = btn.textContent;
      navigator.clipboard.writeText(source.textContent.trim()).then(function () {
        btn.textContent = 'Đã sao chép';
        setTimeout(function () { btn.textContent = original; }, 2000);
      }).catch(function () {
        btn.textContent = 'Không sao chép được';
      });
    });
  });
})();
