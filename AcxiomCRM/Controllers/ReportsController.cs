using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
namespace AcxiomCRM.Controllers;
[Authorize]
public class ReportsController(CrmService crm,CrmDb db):Controller {
 public async Task<IActionResult> Index(string kind="customers",string q="",string status="",string owner="",DateTime? from=null,DateTime? to=null,string sort="newest",int page=1,bool export=false){
  ViewBag.Kind=kind;ViewBag.Owners=await crm.Owners();
  List<string[]> rows=[];string[] headers;
  if(kind is "audit" or "useractivity"){
   if(!User.IsInRole(Roles.Admin) && !(kind=="useractivity"&&User.IsInRole(Roles.Manager)))return Forbid();
   var query=db.AuditLogs.AsNoTracking().AsQueryable();if(!User.IsInRole(Roles.Admin))query=query.Where(x=>Modules.All.Contains(x.EntityName));
   if(from.HasValue)query=query.Where(x=>x.CreatedDate>=from.Value.Date);if(to.HasValue){var end=to.Value.Date.AddDays(1);query=query.Where(x=>x.CreatedDate<end);}if(owner!="")query=query.Where(x=>x.UserId==owner);if(q!="")query=query.Where(x=>x.Action.Contains(q)||x.EntityName.Contains(q));
   var data=await query.OrderByDescending(x=>x.CreatedDate).ToListAsync();
   if(kind=="audit"){headers=["Date (UTC)","User","Action","Module","Record","Result"];rows=data.Select(x=>new[]{x.CreatedDate.ToString("u"),x.UserId??"anonymous",x.Action,x.EntityName,x.RecordId,x.Result}).ToList();}
   else {headers=["User","Action","Count"];rows=data.GroupBy(x=>new{x.UserId,x.Action}).Select(g=>new[]{g.Key.UserId??"anonymous",g.Key.Action,g.Count().ToString()}).ToList();}
  }else if(kind is "pipeline" or "conversion"){
   if(kind=="pipeline"){var data=CrmController.Filter(await crm.All("opportunities"),q,status,owner,from,to,sort);headers=["Stage","Owner","Count","Amount","Weighted amount"];rows=data.GroupBy(x=>new{x.Stage,x.AssignedTo}).Select(g=>new[]{g.Key.Stage,g.Key.AssignedTo,g.Count().ToString(),g.Sum(x=>x.Amount).ToString("0.00"),g.Sum(x=>x.Amount*x.Probability/100).ToString("0.00")}).ToList();}
   else {var leads=CrmController.Filter(await crm.All("leads"),q,status,owner,from,to,sort);var ops=CrmController.Filter(await crm.All("opportunities"),q,"",owner,from,to,sort);headers=["Metric","Value"];rows=[ ["Total leads",leads.Count.ToString()],["Converted",leads.Count(x=>x.Status=="Converted").ToString()],["Conversion rate",(leads.Count==0?0:100m*leads.Count(x=>x.Status=="Converted")/leads.Count).ToString("0.0")+"%"],["Won opportunities",ops.Count(x=>x.Status=="Won").ToString()],["Lost opportunities",ops.Count(x=>x.Status=="Lost").ToString()] ];}
  }else {
   if(!Modules.All.Contains(kind))return NotFound();var data=CrmController.Filter(await crm.All(kind),q,status,owner,from,to,sort);
   headers=["Name","Status","Owner","Email","Phone","Source","Stage","Amount","Probability","Relevant date (UTC)","Overdue","Created (UTC)"];
   rows=data.Select(x=>new[]{x.Name,x.Status,x.AssignedTo,x.Email??"",x.Phone??"",x.Source,kind=="opportunities"?x.Stage:"",x.Amount.ToString("0.00"),x.Probability.ToString(),(x.ExpectedCloseDate??x.FollowUpDate??x.ActivityDate??x.ConvertedDate)?.ToString("yyyy-MM-dd")??"",x.Kind=="followups"&&x.Status=="Planned"&&x.FollowUpDate<DateTime.UtcNow.Date?"Yes":"No",x.CreatedDate.ToString("u")}).ToList();
  }
  if(export){static string Cell(string s)=>"\""+((s.Length>0 && "=+-@\t\r".Contains(s[0]))?"'"+s:s).Replace("\"","\"\"")+"\"";var csv=string.Join(",",headers.Select(Cell))+"\r\n"+string.Join("\r\n",rows.Select(r=>string.Join(",",r.Select(Cell))));return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray(),"text/csv",$"acxiom-{kind}.csv");}
  ViewBag.Headers=headers;ViewBag.Total=rows.Count;ViewBag.Page=Math.Max(1,page);return View(rows.Skip((Math.Max(1,page)-1)*25).Take(25).ToList());
 }
}
