using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AcxiomCRM.Controllers;
[Authorize,Route("crm/{kind}")]
public class CrmController(CrmService crm):Controller {
 internal static List<RecordDto> Filter(List<RecordDto> rows,string q,string status,string owner,DateTime? from,DateTime? to,string sort) {
  IEnumerable<RecordDto> query=rows;
  if(!string.IsNullOrWhiteSpace(q))query=query.Where(x=>string.Join(" ",x.Name,x.Email,x.Phone,x.CompanyName,x.Source,x.Stage,x.ActivityType,x.RelatedName,x.CustomerId,x.LeadId).Contains(q,StringComparison.OrdinalIgnoreCase));
  if(!string.IsNullOrEmpty(status))query=query.Where(x=>x.Status==status || x.Stage==status);
  if(!string.IsNullOrEmpty(owner))query=query.Where(x=>x.AssignedTo==owner);
  DateTime Date(RecordDto x)=>x.Kind=="followups"?x.FollowUpDate??x.CreatedDate:x.Kind=="activities"?x.ActivityDate??x.CreatedDate:x.CreatedDate;
  if(from.HasValue)query=query.Where(x=>Date(x).Date>=from.Value.Date);if(to.HasValue)query=query.Where(x=>Date(x).Date<=to.Value.Date);
  return (sort switch {"name"=>query.OrderBy(x=>x.Name),"oldest"=>query.OrderBy(x=>x.CreatedDate),"amount"=>query.OrderByDescending(x=>x.Amount),_=>query.OrderByDescending(x=>x.CreatedDate)}).ToList();
 }
 async Task Choices(){ViewBag.Owners=await crm.Owners();foreach(var kind in new[]{"customers","leads","opportunities"})ViewData[kind]=await crm.All(kind);}
 [HttpGet("")] public async Task<IActionResult> Index(string kind,string q="",string status="",string owner="",DateTime? from=null,DateTime? to=null,string sort="newest",int page=1){
  if(!Modules.All.Contains(kind))return NotFound();
  var rows=Filter(await crm.All(kind),q,status,owner,from,to,sort);page=Math.Clamp(page,1,Math.Max(1,(rows.Count+14)/15));
  await Choices();return View(new ListVm{Kind=kind,Rows=rows.Skip((page-1)*15).Take(15).ToList(),Total=rows.Count,Page=page,Q=q,Status=status,Owner=owner,From=from,To=to,Sort=sort});
 }
 [HttpGet("new")] public async Task<IActionResult> Create(string kind){if(!Modules.All.Contains(kind))return NotFound();await Choices();return View("Edit",new RecordDto{Kind=kind,AssignedTo=crm.UserId,Status=Modules.Statuses(kind)[0],ExpectedCloseDate=DateTime.UtcNow.Date.AddDays(30),FollowUpDate=DateTime.UtcNow.Date,ActivityDate=DateTime.UtcNow.Date});}
 [HttpGet("{id:guid}/edit")] public async Task<IActionResult> Edit(string kind,Guid id){try{var dto=await crm.Get(kind,id);await Choices();return View(dto);}catch(RuleException e){return StatusCode(e.Status,e.Message);}}
 [HttpPost("save")] public async Task<IActionResult> Save(string kind,RecordDto vm){
  vm.Kind=kind;ModelState.Clear();TryValidateModel(vm);
  if(ModelState.IsValid)try{var saved=await crm.Save(kind,vm.Id==Guid.Empty?null:vm.Id,vm);TempData["Message"]="Record saved.";return RedirectToAction("Details",new{kind,id=saved.Id});}catch(RuleException e){ModelState.AddModelError("",e.Message);}
  await Choices();return View("Edit",vm);
 }
 [HttpGet("{id:guid}")] public async Task<IActionResult> Details(string kind,Guid id){try{var dto=await crm.Get(kind,id);ViewBag.History=await crm.History(kind,id);
 var related=new List<RecordDto>();foreach(var module in new[]{"opportunities","followups","activities"})related.AddRange((await crm.All(module)).Where(x=>kind=="customers"?x.CustomerId==id:kind=="leads"?x.LeadId==id:kind=="opportunities"&&x.OpportunityId==id));ViewBag.Related=related;return View(dto);}catch(RuleException e){return StatusCode(e.Status,e.Message);}}
 [HttpPost("{id:guid}/delete")] public async Task<IActionResult> Delete(string kind,Guid id,Guid version){try{await crm.Delete(kind,id,version);TempData["Message"]="Record deleted.";}catch(RuleException e){TempData["Error"]=e.Message;}return RedirectToAction("Index",new{kind});}
 [HttpPost("{id:guid}/convert")] public async Task<IActionResult> Convert(string kind,Guid id,Guid version){if(kind!="leads")return NotFound();try{var customer=await crm.Convert(id,version);TempData["Message"]="Lead converted into a customer and opportunity.";return RedirectToAction("Details",new{kind="customers",id=customer.Id});}catch(RuleException e){TempData["Error"]=e.Message;return RedirectToAction("Details",new{kind,id});}}
}
