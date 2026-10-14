using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AcxiomCRM.Controllers;
[Authorize]
public class DashboardController(CrmService crm):Controller {
 public async Task<IActionResult> Index(string period="all",DateTime? from=null,DateTime? to=null){
  var today=DateTime.UtcNow.Date;
  if(period=="today"){from=today;to=today;}if(period=="week"){from=today.AddDays(-((int)today.DayOfWeek+6)%7);to=today;}if(period=="month"){from=new(today.Year,today.Month,1);to=today;}
  List<RecordDto> Filter(List<RecordDto> data)=>data.Where(x=>(!from.HasValue||x.CreatedDate.Date>=from.Value.Date)&&(!to.HasValue||x.CreatedDate.Date<=to.Value.Date)).ToList();
  return View(new DashboardVm{Customers=Filter(await crm.All("customers")),Leads=Filter(await crm.All("leads")),Opportunities=Filter(await crm.All("opportunities")),FollowUps=await crm.All("followups"),From=from,To=to});
 }
}
