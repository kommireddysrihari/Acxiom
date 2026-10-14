using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
namespace AcxiomCRM.ViewModels;
public static class Modules {
 public static readonly string[] All=["customers","leads","opportunities","followups","activities"];
 public static string[] Statuses(string kind)=>kind switch {
 "customers"=>["Active","Inactive"],"leads"=>["New","Contacted","Qualified","Unqualified","Converted","Lost"],
 "opportunities"=>["Open","Won","Lost"],"followups"=>["Planned","Completed","Missed","Cancelled"],"activities"=>["Planned","Completed","Cancelled"],_=>[]};
 public static readonly string[] Stages=["Qualification","Proposal","Negotiation","Won","Lost"];
 public static readonly string[] Types=["Call","Meeting","Email","Task"];
}
// This allowlisted request/response DTO deliberately excludes navigation objects and security fields.
public class RecordDto : IValidatableObject {
 public Guid Id {get;set;}
 public Guid Version {get;set;}
 public string RelatedName {get;set;}="";
 public string Kind {get;set;}="customers";
 [Required,StringLength(120)] public string Name {get;set;}="";
 [Required,StringLength(30)] public string Status {get;set;}="Active";
 [Required] public string AssignedTo {get;set;}="";
 [StringLength(2000)] public string Notes {get;set;}="";
 [EmailAddress,StringLength(254)] public string? Email {get;set;}
 [RegularExpression(@"^[6-9][0-9]{9}$",ErrorMessage="Enter a 10-digit Indian mobile number starting with 6, 7, 8 or 9.")] public string? Phone {get;set;}
 [StringLength(120)] public string CompanyName {get;set;}="";
 [StringLength(300)] public string Address {get;set;}="";
 [StringLength(80)] public string City {get;set;}="";
 [StringLength(80)] public string State {get;set;}="";
 [StringLength(80)] public string Source {get;set;}="";
 public string Priority {get;set;}="Normal";
 [Range(typeof(decimal),"0","100000000000")] public decimal ExpectedValue {get;set;}
 [Range(typeof(decimal),"0","100000000000")] public decimal Amount {get;set;}
 [Range(0,100)] public int Probability {get;set;}=10;
 public string Stage {get;set;}="Qualification";
 [DataType(DataType.Date)] public DateTime? ExpectedCloseDate {get;set;}
 [DataType(DataType.Date)] public DateTime? FollowUpDate {get;set;}
 [DataType(DataType.Date)] public DateTime? ActivityDate {get;set;}
 public string FollowUpType {get;set;}="Call";
 public string ActivityType {get;set;}="Call";
 public Guid? CustomerId {get;set;}
 public Guid? LeadId {get;set;}
 public Guid? OpportunityId {get;set;}
 public DateTime CreatedDate {get;set;}
 public DateTime? ClosedDate {get;set;}
 public DateTime? ConvertedDate {get;set;}
 public Guid? ConvertedCustomerId {get;set;}
 public string CustomerCode {get;set;}="";
 public string LeadCode {get;set;}="";
 public IEnumerable<ValidationResult> Validate(ValidationContext context) {
  if(context.GetService(typeof(IHttpContextAccessor)) is IHttpContextAccessor accessor && accessor.HttpContext?.Request.RouteValues["kind"] is string routeKind) Kind=routeKind;
  if(!Modules.All.Contains(Kind)) yield return new("Unknown module.",[nameof(Kind)]);
  if(!Modules.Statuses(Kind).Contains(Status)) yield return new("Select a valid status.",[nameof(Status)]);
  if(Kind is "customers" or "leads") {
   if(string.IsNullOrWhiteSpace(Email)) yield return new("Email is required.",[nameof(Email)]);
   if(string.IsNullOrWhiteSpace(Phone)) yield return new("Phone is required.",[nameof(Phone)]);
  }
  if(Kind=="leads" && !new[]{"Low","Normal","High"}.Contains(Priority)) yield return new("Select a valid priority.",[nameof(Priority)]);
  if(Kind=="opportunities") {
   if(CustomerId==null) yield return new("Select a customer.",[nameof(CustomerId)]);
   if(!Modules.Stages.Contains(Stage)) yield return new("Select a valid stage.",[nameof(Stage)]);
   var expected=Stage is "Won" or "Lost" ? Stage : "Open";
   if(Status!=expected) yield return new("Status must be Open for active stages, or match Won/Lost.",[nameof(Status)]);
   if(Amount<=0) yield return new("Opportunity amount must be greater than 0.",[nameof(Amount)]);
   if(ExpectedCloseDate==null || (Status=="Open" && ExpectedCloseDate.Value.Date<DateTime.UtcNow.Date)) yield return new("Set a close date today or later for an open opportunity.",[nameof(ExpectedCloseDate)]);
  }
  if(Kind=="followups") {
   if(CustomerId==null && LeadId==null && OpportunityId==null) yield return new("Select a related customer, lead or opportunity.",[nameof(CustomerId)]);
   if(FollowUpDate==null || (Status=="Planned" && FollowUpDate.Value.Date<DateTime.UtcNow.Date)) yield return new("Planned follow-up must be today or later.",[nameof(FollowUpDate)]);
   if(!Modules.Types.Contains(FollowUpType)) yield return new("Select a valid type.",[nameof(FollowUpType)]);
  }
  if(Kind=="activities") {
   if(CustomerId==null && LeadId==null) yield return new("Select a related customer or lead.",[nameof(CustomerId)]);
   if(ActivityDate==null) yield return new("Activity date is required.",[nameof(ActivityDate)]);
   if(!Modules.Types.Contains(ActivityType)) yield return new("Select a valid type.",[nameof(ActivityType)]);
  }
 }
}
public class LoginVm { [Required] public string Login {get;set;}=""; [Required,DataType(DataType.Password)] public string Password {get;set;}=""; }
public class RegisterVm { [Required,EmailAddress] public string Email {get;set;}=""; [Required,MinLength(10),DataType(DataType.Password)] public string Password {get;set;}=""; [Compare(nameof(Password)),DataType(DataType.Password)] public string ConfirmPassword {get;set;}=""; }
public class UserVm { public string Id {get;set;}=""; [Required,EmailAddress] public string Email {get;set;}=""; [Required] public string Role {get;set;}=Roles.Sales; public bool IsActive {get;set;}=true; [DataType(DataType.Password)] public string? Password {get;set;} public bool Unlock {get;set;} }
public record Choice(string Id,string Name);
public class ListVm { public string Kind {get;set;}=""; public List<RecordDto> Rows {get;set;}=[]; public int Total {get;set;} public int Page {get;set;}=1; public string Q {get;set;}=""; public string Status {get;set;}=""; public string Owner {get;set;}=""; public string Sort {get;set;}="newest"; public DateTime? From {get;set;} public DateTime? To {get;set;} }
public class DashboardVm { public List<RecordDto> Customers {get;set;}=[]; public List<RecordDto> Leads {get;set;}=[]; public List<RecordDto> Opportunities {get;set;}=[]; public List<RecordDto> FollowUps {get;set;}=[]; public DateTime? From {get;set;} public DateTime? To {get;set;} }
