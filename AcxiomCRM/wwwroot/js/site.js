'use strict';
document.querySelectorAll('nav a').forEach(a=>{if(a.pathname===location.pathname)a.classList.add('active');});
document.querySelectorAll('form[data-confirm]').forEach(f=>f.addEventListener('submit',e=>{if(!confirm(f.dataset.confirm))e.preventDefault();}));
// Native validation backs up unobtrusive rules if enhancement scripts are unavailable.
document.querySelectorAll('form[data-crm]').forEach(form=>{
 const kind=form.dataset.crm, today=new Date().toISOString().slice(0,10);
 function rules(){
  const status=form.querySelector('[name=Status]');
  if(kind==='opportunities'){
   const stage=form.querySelector('[name=Stage]').value;
   status.value=['Won','Lost'].includes(stage)?stage:'Open';
   const close=form.querySelector('[name=ExpectedCloseDate]');if(status.value==='Open')close.min=today;else close.removeAttribute('min');
  }
  if(kind==='followups'){
   const date=form.querySelector('[name=FollowUpDate]');const isNew=form.querySelector('[name=Id]').value==='00000000-0000-0000-0000-000000000000';
   if(isNew||status.value==='Planned')date.min=today;else date.removeAttribute('min');
  }
 }
 form.addEventListener('change',rules);rules();
 form.addEventListener('submit',e=>{rules();if(!form.checkValidity()){e.preventDefault();form.reportValidity();}});
});
