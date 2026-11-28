(() => {
 for(const form of document.querySelectorAll('.customer-account-form')) {
  const password=form.querySelector('[name="NewPassword"]');const confirm=form.querySelector('[name="ConfirmPassword"]');
  function compare(){if(confirm)confirm.setCustomValidity(confirm.value && password?.value!==confirm.value?'Mật khẩu xác nhận không khớp.':'');}
  password?.addEventListener('input',compare);confirm?.addEventListener('input',compare);
  form.addEventListener('invalid',event=>{const input=event.target;if(input.validity.patternMismatch&&input.title)input.setCustomValidity(input.title);},true);
  form.addEventListener('input',event=>{if(event.target!==confirm)event.target.setCustomValidity?.('');compare();});
 }
})();
