using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace AcxiomCRM.Models;
public class ApplicationUser : IdentityUser { public bool IsActive { get; set; } = true; }
public static class Roles { public const string Admin="Admin", Manager="Manager", Sales="SalesExecutive"; public static readonly string[] All=[Admin,Manager,Sales]; }
public abstract class CrmRecord {
 public Guid Id {get;set;}=Guid.NewGuid();
 [MaxLength(120)] public string Name {get;set;}="";
 [MaxLength(30)] public string Status {get;set;}="";
 public string AssignedTo {get;set;}="";
 public ApplicationUser? Owner {get;set;}
 [MaxLength(2000)] public string Notes {get;set;}="";
 public DateTime CreatedDate {get;set;}=DateTime.UtcNow;
 public DateTime ModifiedDate {get;set;}=DateTime.UtcNow;
 public string CreatedBy {get;set;}="";
 [ConcurrencyCheck] public Guid Version {get;set;}=Guid.NewGuid();
}
public class Customer : CrmRecord {
 public string CustomerCode {get;set;}="";
 public string Email {get;set;}="";
 public string Phone {get;set;}="";
 public string CompanyName {get;set;}="";
 public string Address {get;set;}="";
 public string City {get;set;}="";
 public string State {get;set;}="";
}
public class Lead : CrmRecord {
 public string LeadCode {get;set;}="";
 public string Email {get;set;}="";
 public string Phone {get;set;}="";
 public string CompanyName {get;set;}="";
 public string Source {get;set;}="";
 public string Priority {get;set;}="Normal";
 public decimal ExpectedValue {get;set;}
 public Guid? ConvertedCustomerId {get;set;}
 public Customer? ConvertedCustomer {get;set;}
 public DateTime? ConvertedDate {get;set;}
}
public class Opportunity : CrmRecord {
 public Guid? CustomerId {get;set;}
 public Customer? Customer {get;set;}
 public Guid? LeadId {get;set;}
 public Lead? Lead {get;set;}
 public decimal Amount {get;set;}
 public int Probability {get;set;}
 public string Stage {get;set;}="Qualification";
 public DateTime? ExpectedCloseDate {get;set;}
 public DateTime? ClosedDate {get;set;}
 public string Source {get;set;}="";
}
public class FollowUp : CrmRecord {
 public Guid? CustomerId {get;set;}
 public Customer? Customer {get;set;}
 public Guid? LeadId {get;set;}
 public Lead? Lead {get;set;}
 public Guid? OpportunityId {get;set;}
 public Opportunity? Opportunity {get;set;}
 public DateTime? FollowUpDate {get;set;}
 public string FollowUpType {get;set;}="Call";
}
public class Activity : CrmRecord {
 public Guid? CustomerId {get;set;}
 public Customer? Customer {get;set;}
 public Guid? LeadId {get;set;}
 public Lead? Lead {get;set;}
 public DateTime? ActivityDate {get;set;}
 public string ActivityType {get;set;}="Call";
}
public class AuditLog {
 public long Id {get;set;}
 public string? UserId {get;set;}
 public string Action {get;set;}="";
 public string EntityName {get;set;}="";
 public string RecordId {get;set;}="";
 public string? OldValue {get;set;}
 public string? NewValue {get;set;}
 public string Result {get;set;}="Success";
 public DateTime CreatedDate {get;set;}=DateTime.UtcNow;
 public string? IpAddress {get;set;}
}
