using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Data;
public class CrmDb(DbContextOptions<CrmDb> options):IdentityDbContext<ApplicationUser>(options) {
 public DbSet<Customer> Customers=>Set<Customer>(); public DbSet<Lead> Leads=>Set<Lead>();
 public DbSet<Opportunity> Opportunities=>Set<Opportunity>(); public DbSet<FollowUp> FollowUps=>Set<FollowUp>();
 public DbSet<Activity> Activities=>Set<Activity>(); public DbSet<AuditLog> AuditLogs=>Set<AuditLog>();
 protected override void OnModelCreating(ModelBuilder b) {
  base.OnModelCreating(b);
  b.Entity<CrmRecord>().UseTpcMappingStrategy();
  b.Entity<CrmRecord>().Property(x=>x.Id).ValueGeneratedNever();
  b.Entity<CrmRecord>().HasOne(x=>x.Owner).WithMany().HasForeignKey(x=>x.AssignedTo).OnDelete(DeleteBehavior.Restrict);
  b.Entity<CrmRecord>().HasIndex(x=>x.AssignedTo);
  b.Entity<Customer>().HasIndex(x=>x.Email).IsUnique(); b.Entity<Customer>().HasIndex(x=>x.Phone).IsUnique();
  b.Entity<Customer>().HasIndex(x=>x.CustomerCode).IsUnique(); b.Entity<Lead>().HasIndex(x=>x.LeadCode).IsUnique();
  b.Entity<Lead>().HasOne(x=>x.ConvertedCustomer).WithMany().HasForeignKey(x=>x.ConvertedCustomerId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Opportunity>().HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Opportunity>().HasOne(x=>x.Lead).WithMany().HasForeignKey(x=>x.LeadId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<FollowUp>().HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<FollowUp>().HasOne(x=>x.Lead).WithMany().HasForeignKey(x=>x.LeadId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<FollowUp>().HasOne(x=>x.Opportunity).WithMany().HasForeignKey(x=>x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Activity>().HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<Activity>().HasOne(x=>x.Lead).WithMany().HasForeignKey(x=>x.LeadId).OnDelete(DeleteBehavior.Restrict);
  b.Entity<AuditLog>().HasIndex(x=>x.CreatedDate);
 }
 public override Task<int> SaveChangesAsync(CancellationToken token=default) {
  if(ChangeTracker.Entries<AuditLog>().Any(e=>e.State is EntityState.Modified or EntityState.Deleted)) throw new InvalidOperationException("Audit entries are append-only.");
  return base.SaveChangesAsync(token);
 }
}
