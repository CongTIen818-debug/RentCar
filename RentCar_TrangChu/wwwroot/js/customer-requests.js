(() => {
  'use strict';
  const noticeLink = document.querySelector('[data-customer-request-notices]');
  const page = document.querySelector('[data-requests-page]');
  let refreshing = false;
  let observer;
  const timers = new Map();
  const reading = new Set();
  const feedback = page?.querySelector('[data-request-feedback]');
  function notify(message, error = false) {
    if (!feedback) return;
    feedback.hidden = false;
    feedback.textContent = message;
    feedback.classList.toggle('is-error', error);
  }
  function badge(count) {
    document.querySelectorAll('[data-request-unread-dot]').forEach(dot => {
      dot.hidden = count === 0;
      dot.dataset.count = String(count);
      dot.setAttribute('aria-label', `${count} thông báo duyệt chưa đọc`);
    });
  }
  async function pollBadge() {
    if (!noticeLink?.dataset.unreadUrl || document.hidden) return;
    try {
      const response = await fetch(noticeLink.dataset.unreadUrl, { headers: { Accept: 'application/json' }, cache: 'no-store' });
      if (response.ok && !response.redirected && response.headers.get('content-type')?.includes('application/json')) badge((await response.json()).count);
    } catch { /* Giữ trạng thái trước đó nếu mạng tạm thời không khả dụng. */ }
  }
  async function post(form) {
    const response = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { Accept: 'application/json' }, cache: 'no-store' });
    if (response.redirected) { window.location.assign(response.url); return null; }
    const json = response.headers.get('content-type')?.includes('application/json') ? await response.json() : null;
    if (!response.ok) throw new Error(json?.message || 'Chưa thực hiện được thao tác. Vui lòng tải lại trang và thử lại.');
    return json;
  }
  async function read(form) {
    if (!form || !form.isConnected) return;
    const key = `${form.action}|${new FormData(form).get('version')}`;
    if (reading.has(key)) return;
    reading.add(key);
    try {
      const result = await post(form);
      if (!result) return;
      badge(result.count);
      form.replaceWith(Object.assign(document.createElement('span'), { className: 'request-read-done', textContent: 'Đã đọc thông báo' }));
    } catch { reading.delete(key); }
  }
  function observe() {
    observer?.disconnect();
    timers.forEach(clearTimeout); timers.clear();
    if (!('IntersectionObserver' in window)) return;
    observer = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        clearTimeout(timers.get(entry.target)); timers.delete(entry.target);
        if (entry.isIntersecting && !document.hidden) {
          timers.set(entry.target, setTimeout(() => {
            if (!document.hidden) read(entry.target.querySelector('[data-read-approval]'));
          }, 1200));
        }
      });
    }, { threshold: 0.25 });
    document.querySelectorAll('[data-approval-notice]').forEach(node => observer.observe(node));
  }
  function countdown() {
    document.querySelectorAll('[data-expirable="true"]').forEach(card => {
      const counter = card.querySelector('[data-request-countdown]');
      if (!counter) return;
      if (!card.dataset.clockOffset) card.dataset.clockOffset = String(Date.parse(card.dataset.serverNow) - Date.now());
      const seconds = Math.max(0, Math.ceil((Date.parse(card.dataset.deadline) - (Date.now() + Number(card.dataset.clockOffset))) / 1000));
      if (seconds === 0) {
        counter.textContent = '';
        card.querySelector('[data-deposit-message]').textContent = 'Đã quá thời hạn thanh toán cọc 24 tiếng, vui lòng gửi lại yêu cầu.';
        card.querySelector('[data-approval-notice]').classList.add('request-message-expired');
        card.querySelector('[data-request-payment]').hidden = true;
        card.querySelector('[data-request-resubmit]').hidden = false;
      } else {
        counter.textContent = ` · Còn ${Math.floor(seconds / 3600)} giờ ${Math.floor(seconds % 3600 / 60)} phút ${seconds % 60} giây`;
      }
    });
  }
  async function refresh(manual = false) {
    if (!page || refreshing || document.hidden || document.querySelector('[data-cancel-dialog]')?.open) return;
    refreshing = true;
    const button = page.querySelector('[data-refresh-requests]');
    if (button) button.disabled = true;
    try {
      const response = await fetch(page.dataset.listUrl, { headers: { Accept: 'text/html' }, cache: 'no-store' });
      if (response.redirected) { window.location.assign(response.url); return; }
      if (!response.ok || !response.headers.get('content-type')?.includes('text/html')) throw new Error();
      const html = await response.text();
      const parsed = new DOMParser().parseFromString(html, 'text/html');
      const fragment = parsed.querySelector('[data-requests-fragment]');
      if (!fragment) throw new Error();
      page.querySelector('[data-request-list]').replaceChildren(document.importNode(fragment, true));
      ['total', 'pending', 'approved', 'cancelled'].forEach(key => {
        const stat = page.querySelector(`[data-request-stat="${key}"]`);
        if (stat) stat.textContent = fragment.dataset[key];
      });
      badge(Number(fragment.dataset.unread));
      observe(); countdown();
      if (manual) notify('Đã cập nhật yêu cầu của bạn.');
    } catch { if (manual) notify('Chưa cập nhật được danh sách. Vui lòng thử lại.', true); }
    finally { refreshing = false; if (button) button.disabled = false; }
  }
  const dialog = document.querySelector('[data-cancel-dialog]');
  let cancelForm;
  async function cancel(form) {
    const button = form.querySelector('button');
    if (button) button.disabled = true;
    try { const result = await post(form); if (result) { await refresh(); notify(result.message); } }
    catch (error) { notify(error.message, true); await refresh(); }
    finally { if (button) button.disabled = false; }
  }
  document.addEventListener('submit', event => {
    const form = event.target;
    if (form.matches('[data-read-approval]')) { event.preventDefault(); read(form); }
    if (form.matches('[data-cancel-request]')) {
      event.preventDefault();
      if (dialog && typeof dialog.showModal === 'function') { cancelForm = form; dialog.showModal(); }
      else if (window.confirm('Bạn muốn hủy yêu cầu đang chờ duyệt?')) cancel(form);
    }
  });
  dialog?.querySelector('[data-close-cancel]')?.addEventListener('click', () => dialog.close());
  dialog?.querySelector('[data-confirm-cancel]')?.addEventListener('click', () => { dialog.close(); if (cancelForm) cancel(cancelForm); });
  page?.querySelector('[data-refresh-requests]')?.addEventListener('click', () => refresh(true));
  document.addEventListener('visibilitychange', () => { if (!document.hidden) { pollBadge(); refresh(); observe(); } else { timers.forEach(clearTimeout); timers.clear(); } });
  window.addEventListener('pageshow', pollBadge);
  if (noticeLink) setInterval(pollBadge, 20000);
  if (page) { observe(); countdown(); setInterval(countdown, 1000); setInterval(refresh, 20000); }
})();
