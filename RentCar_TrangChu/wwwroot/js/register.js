(() => {
    const password = document.getElementById('Password');
    const confirmation = document.getElementById('ConfirmPassword');
    const toggle = document.getElementById('register-toggle-password');
    if (!password || !confirmation || !toggle) return;
    toggle.hidden = false;
    toggle.addEventListener('click', () => {
        const show = password.type === 'password';
        password.type = confirmation.type = show ? 'text' : 'password';
        toggle.textContent = show ? 'Ẩn' : 'Hiện';
        toggle.setAttribute('aria-pressed', String(show));
    });
    const validateConfirmation = () => {
        confirmation.setCustomValidity(confirmation.value && password.value !== confirmation.value
            ? 'Mật khẩu nhập lại không khớp.' : '');
    };
    password.addEventListener('input', validateConfirmation);
    confirmation.addEventListener('input', validateConfirmation);
})();
