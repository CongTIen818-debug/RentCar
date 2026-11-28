(() => {
 document.addEventListener('submit', async event => {
  const form=event.target;
  if(!form.matches('.rental-booking-form') || event.submitter?.value!=='add' || event.defaultPrevented)return;
  event.preventDefault();const button=event.submitter;const label=button.textContent;button.disabled=true;button.textContent="Đang thêm…";form.setAttribute("aria-busy","true");
  let feedback=form.querySelector('[data-fleet-feedback]');
  if(!feedback){feedback=document.createElement('p');feedback.dataset.fleetFeedback='';feedback.setAttribute('role','status');feedback.className='contract-note';form.append(feedback);}
  try {
   const data=new FormData(form);data.set('intent','add');
   const response=await fetch(form.action,{method:'POST',body:data,headers:{Accept:'application/json'}});
   if(response.redirected){window.location.assign(response.url);return;}
   if(!response.ok)throw new Error('Chưa thêm được xe. Vui lòng kiểm tra thông tin và thử lại.');
   const result=await response.json();document.querySelectorAll('[data-rental-count]').forEach(el=>el.textContent=String(result.count));feedback.textContent=result.message;
  }catch(error){feedback.textContent=error.message;}finally{button.disabled=false;button.textContent=label;form.removeAttribute("aria-busy");}
 });
})();
