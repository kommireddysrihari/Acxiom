using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
namespace AcxiomCRM.Controllers;
// Cookie-authenticated same-origin API; all unsafe requests require an antiforgery header.
[Authorize,ApiController,Route("api")]
public class ApiController(CrmService crm,IAntiforgery antiforgery):ControllerBase {
 [HttpGet("csrf")] public IActionResult Csrf(){Response.Headers.CacheControl="no-store";return Ok(new{requestToken=antiforgery.GetAndStoreTokens(HttpContext).RequestToken});}
 async Task<IActionResult> Execute(Func<Task<IActionResult>> action){try{return await action();}catch(RuleException e){return StatusCode(e.Status,new{error=e.Message});}}
 [HttpGet("{kind:regex(^(customers|leads|opportunities|followups|activities)$)}")]
 public Task<IActionResult> List(string kind,string q="",string status="",string owner="",DateTime? from=null,DateTime? to=null,string sort="newest",int page=1,int pageSize=25)=>Execute(async()=>{var rows=CrmController.Filter(await crm.All(kind),q,status,owner,from,to,sort);page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);return Ok(new{total=rows.Count,page,pageSize,items=rows.Skip((page-1)*pageSize).Take(pageSize)});});
 [HttpGet("{kind:regex(^(customers|leads|opportunities|followups|activities)$)}/{id:guid}")]
 public Task<IActionResult> Get(string kind,Guid id)=>Execute(async()=>Ok(await crm.Get(kind,id)));
 [HttpPost("{kind:regex(^(customers|leads|opportunities|followups|activities)$)}")]
 public Task<IActionResult> Create(string kind,[FromBody]RecordDto dto)=>Execute(async()=>{var row=await crm.Save(kind,null,dto);return Created($"/api/{kind}/{row.Id}",row);});
 [HttpPut("{kind:regex(^(customers|leads|opportunities|followups|activities)$)}/{id:guid}")]
 public Task<IActionResult> Update(string kind,Guid id,[FromBody]RecordDto dto)=>Execute(async()=>Ok(await crm.Save(kind,id,dto)));
 [HttpDelete("{kind:regex(^(customers|leads|opportunities|followups|activities)$)}/{id:guid}")]
 public Task<IActionResult> Delete(string kind,Guid id,[FromQuery]Guid version)=>Execute(async()=>{await crm.Delete(kind,id,version);return NoContent();});
 [HttpPost("leads/{id:guid}/convert")]
 public Task<IActionResult> Convert(Guid id,[FromQuery]Guid version)=>Execute(async()=>Ok(await crm.Convert(id,version)));
 [HttpGet("reports/pipeline")] public Task<IActionResult> Pipeline()=>Execute(async()=>Ok((await crm.All("opportunities")).GroupBy(x=>new{x.Stage,x.AssignedTo}).Select(g=>new{g.Key.Stage,g.Key.AssignedTo,amount=g.Sum(x=>x.Amount),weighted=g.Sum(x=>x.Amount*x.Probability/100),count=g.Count()})));
}
