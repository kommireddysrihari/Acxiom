using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
namespace AcxiomCRM.Services;
public class RuleException(string message,int status=400):Exception(message) {public int Status {get;}=status;}
public class CrmService(CrmDb db,IHttpContextAccessor accessor,Auditor audit) {
 ClaimsPrincipal User=>accessor.HttpContext!.User;
 public string UserId=>User.FindFirstValue(ClaimTypes.NameIdentifier)!;
 public bool Elevated=>User.IsInRole(Roles.Admin)||User.IsInRole(Roles.Manager);
 public IQueryable<CrmRecord> Query(string kind) {
  IQueryable<CrmRecord> q=kind switch {"customers"=>db.Customers,"leads"=>db.Leads,"opportunities"=>db.Opportunities,"followups"=>db.FollowUps,"activities"=>db.Activities,_=>throw new RuleException("Unknown module.",404)};
  return Elevated?q:q.Where(x=>x.AssignedTo==UserId);
 }
 static readonly HashSet<string> ReadOnly=["Id","Version","Kind","CreatedDate","ClosedDate","ConvertedDate","ConvertedCustomerId","CustomerCode","LeadCode","RelatedName"];
 public static RecordDto Dto(CrmRecord entity,string kind) {
  var dto=new RecordDto{Kind=kind};
  foreach(var p in typeof(RecordDto).GetProperties()) {var source=entity.GetType().GetProperty(p.Name);if(source!=null && source.PropertyType==p.PropertyType)p.SetValue(dto,source.GetValue(entity));}
  return dto;
 }
 static void Apply(RecordDto dto,CrmRecord entity) {
  foreach(var p in typeof(RecordDto).GetProperties().Where(p=>!ReadOnly.Contains(p.Name))) {var target=entity.GetType().GetProperty(p.Name);if(target!=null && target.PropertyType==p.PropertyType)target.SetValue(entity,p.GetValue(dto));}
 }
 public async Task<List<RecordDto>> All(string kind) {
  var rows=(await Query(kind).AsNoTracking().OrderByDescending(x=>x.CreatedDate).ToListAsync()).Select(x=>Dto(x,kind)).ToList();
  if(kind is "opportunities" or "followups" or "activities") {
   var names=await Query("customers").AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Name);
   var leads=await Query("leads").AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Name);
   foreach(var row in rows)row.RelatedName=string.Join(" / ",new[]{row.CustomerId.HasValue?names.GetValueOrDefault(row.CustomerId.Value):null,row.LeadId.HasValue?leads.GetValueOrDefault(row.LeadId.Value):null}.Where(x=>x!=null));
  }
  return rows;
 }
 public async Task<RecordDto> Get(string kind,Guid id)=>Dto(await Find(kind,id),kind);
 async Task<CrmRecord> Find(string kind,Guid id)=>await Query(kind).SingleOrDefaultAsync(x=>x.Id==id)??throw new RuleException("Record not found in your scope.",404);
 public async Task<List<Choice>> Owners()=> await db.Users.Where(u=>u.IsActive && (Elevated || u.Id==UserId)).OrderBy(u=>u.Email).Select(u=>new Choice(u.Id,u.Email!)).ToListAsync();
 async Task Validate(RecordDto dto,bool create) {
  dto.Name=(dto.Name??"").Trim();dto.Email=dto.Email?.Trim().ToLowerInvariant();dto.Phone=dto.Phone?.Trim();
  var errors=new List<ValidationResult>();
  if(!Validator.TryValidateObject(dto,new ValidationContext(dto),errors,true))throw new RuleException(string.Join(" ",errors.Select(x=>x.ErrorMessage)));
  if(!await db.Users.AnyAsync(x=>x.Id==dto.AssignedTo && x.IsActive))throw new RuleException("Choose an active owner.");
  if(!Elevated && dto.AssignedTo!=UserId)throw new RuleException("You can only assign records to yourself.",403);
  if(dto.Kind=="customers" && await db.Customers.AnyAsync(x=>x.Id!=dto.Id && (x.Email==dto.Email || x.Phone==dto.Phone)))throw new RuleException("Customer email or phone already exists.",409);
  if(dto.Kind=="leads" && create && dto.Status!="New")throw new RuleException("New leads must start in New status.");
  if(dto.Kind=="followups" && create && dto.FollowUpDate!.Value.Date<DateTime.UtcNow.Date)throw new RuleException("A new follow-up cannot be in the past.");
  foreach(var (kind,id) in new[]{("customers",dto.CustomerId),("leads",dto.LeadId),("opportunities",dto.OpportunityId)}) if(id.HasValue) {
   var related=await Find(kind,id.Value);
   // An assigned sales user must also own linked records, including when assigned by a manager.
   var ownerIsSales=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where ur.UserId==dto.AssignedTo && r.Name==Roles.Sales select ur).AnyAsync();
   if(ownerIsSales && related.AssignedTo!=dto.AssignedTo)throw new RuleException("Related records must have the same sales owner. Reassign related records first.");
  }
 }
 public static bool CanTransition(string old,string next)=>old==next || (old switch {"New"=>new[]{"Contacted","Lost"},"Contacted"=>["Qualified","Unqualified","Lost"],"Qualified"=>["Lost"],"Unqualified"=>["Contacted","Lost"],_=>[]}).Contains(next);
 public async Task<RecordDto> Save(string kind,Guid? id,RecordDto dto) {
  dto.Kind=kind;dto.Id=id??Guid.Empty;
  var entity=id.HasValue?await Find(kind,id.Value):kind switch {"customers"=>(CrmRecord)new Customer(),"leads"=>new Lead(),"opportunities"=>new Opportunity(),"followups"=>new FollowUp(),"activities"=>new Activity(),_=>throw new RuleException("Unknown module.",404)};
  await Validate(dto,!id.HasValue);
  var before=id.HasValue?Dto(entity,kind):null;
  if(id.HasValue && entity.Version!=dto.Version)throw new RuleException("This record changed. Reload before saving.",409);
  if(entity is Lead && id.HasValue && !CanTransition(entity.Status,dto.Status))throw new RuleException("Invalid lead transition. Convert a Qualified lead using Convert.");
  if(id.HasValue && entity.AssignedTo!=dto.AssignedTo) {
   // Prevent ownership changes from silently breaking existing record relationships.
   bool linked=kind switch {
    "customers"=>await db.Opportunities.AnyAsync(x=>x.CustomerId==id)||await db.FollowUps.AnyAsync(x=>x.CustomerId==id)||await db.Activities.AnyAsync(x=>x.CustomerId==id)||await db.Leads.AnyAsync(x=>x.ConvertedCustomerId==id),
    "leads"=>await db.Opportunities.AnyAsync(x=>x.LeadId==id)||await db.FollowUps.AnyAsync(x=>x.LeadId==id)||await db.Activities.AnyAsync(x=>x.LeadId==id),
    "opportunities"=>await db.FollowUps.AnyAsync(x=>x.OpportunityId==id),_=>false};
   if(linked)throw new RuleException("Linked records exist. Ownership transfer requires a coordinated team reassignment.",409);
  }
  Apply(dto,entity);entity.ModifiedDate=DateTime.UtcNow;entity.Version=Guid.NewGuid();
  if(entity is Opportunity op){op.ClosedDate=op.Status is "Won" or "Lost"?op.ClosedDate??DateTime.UtcNow:null;}
  if(!id.HasValue) {entity.CreatedBy=UserId;if(entity is Customer c)c.CustomerCode="CUS-"+entity.Id.ToString("N")[..10];if(entity is Lead l)l.LeadCode="LEAD-"+entity.Id.ToString("N")[..10];db.Add(entity);}
  var after=Dto(entity,kind);audit.Add(id.HasValue?"Update":"Create",kind,entity.Id.ToString(),before,after);
  try {await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){throw new RuleException("Record changed. Reload and retry.",409);}catch(DbUpdateException){throw new RuleException("Duplicate or conflicting data. Check related records.",409);}
  return after;
 }
 public async Task Delete(string kind,Guid id,Guid version) {
  var entity=await Find(kind,id);if(entity.Version!=version)throw new RuleException("Record changed. Reload before deleting.",409);
  if(entity is Lead {Status:"Converted"})throw new RuleException("Converted leads are retained for history.",409);
  audit.Add("Delete",kind,id.ToString(),Dto(entity,kind));db.Remove(entity);
  try{await db.SaveChangesAsync();}catch(DbUpdateConcurrencyException){throw new RuleException("Record changed. Reload.",409);}catch(DbUpdateException){throw new RuleException("This record has linked records. Remove the links or mark it inactive.",409);}
 }
 public async Task<RecordDto> Convert(Guid id,Guid version) {
  await using var tx=await db.Database.BeginTransactionAsync();
  var lead=(Lead)await Find("leads",id);
  if(lead.Status!="Qualified" || lead.Version!=version)throw new RuleException("Only an unchanged Qualified lead can be converted.",409);
  if(lead.ExpectedValue<=0)throw new RuleException("Set an expected value greater than zero before conversion.");
  if(await db.Customers.AnyAsync(x=>x.Email==lead.Email || x.Phone==lead.Phone))throw new RuleException("A customer with this email or phone exists. Resolve the duplicate first.",409);
  var before=Dto(lead,"leads");
  var customer=new Customer{Name=lead.Name,Email=lead.Email,Phone=lead.Phone,CompanyName=lead.CompanyName,AssignedTo=lead.AssignedTo,CreatedBy=UserId,Status="Active",Notes=lead.Notes};
  customer.CustomerCode="CUS-"+customer.Id.ToString("N")[..10];
  var opportunity=new Opportunity{Name=lead.Name+" opportunity",CustomerId=customer.Id,LeadId=lead.Id,AssignedTo=lead.AssignedTo,CreatedBy=UserId,Status="Open",Stage="Qualification",Amount=lead.ExpectedValue,Probability=10,ExpectedCloseDate=DateTime.UtcNow.Date.AddDays(30),Source=lead.Source};
  lead.Status="Converted";lead.ConvertedCustomerId=customer.Id;lead.ConvertedDate=DateTime.UtcNow;lead.ModifiedDate=DateTime.UtcNow;lead.Version=Guid.NewGuid();
  db.AddRange(customer,opportunity);
  audit.Add("Convert","leads",id.ToString(),before,Dto(lead,"leads"));audit.Add("Create","customers",customer.Id.ToString(),after:Dto(customer,"customers"));audit.Add("Create","opportunities",opportunity.Id.ToString(),after:Dto(opportunity,"opportunities"));
  try{await db.SaveChangesAsync();await tx.CommitAsync();}catch(DbUpdateException){throw new RuleException("Conversion conflicted with another update. Reload.",409);}
  return Dto(customer,"customers");
 }
 public async Task<List<AuditLog>> History(string kind,Guid id) {await Find(kind,id);return await db.AuditLogs.AsNoTracking().Where(a=>a.EntityName==kind && a.RecordId==id.ToString()).OrderByDescending(a=>a.CreatedDate).Take(100).ToListAsync();}
}
