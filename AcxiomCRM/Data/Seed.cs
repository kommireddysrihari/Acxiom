using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Data;
public static class Seed {
 public static async Task Initialize(IServiceProvider services,IConfiguration config) {
  using var scope=services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<CrmDb>();
  // Local interview database bootstrap. Use reviewed EF migrations for deployed systems.
  await db.Database.EnsureCreatedAsync();
  var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
  foreach(var role in Roles.All)if(!await roles.RoleExistsAsync(role)){var result=await roles.CreateAsync(new(role));if(!result.Succeeded)throw new InvalidOperationException("Role setup failed.");}
  var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
  if((await users.GetUsersInRoleAsync(Roles.Admin)).Count==0) {
   var email=config["Bootstrap:Email"];var password=config["Bootstrap:Password"];
   if(string.IsNullOrWhiteSpace(email)||string.IsNullOrWhiteSpace(password))throw new InvalidOperationException("First run: set Bootstrap:Email and Bootstrap:Password with dotnet user-secrets. See README.");
   var u=new ApplicationUser{UserName=email,Email=email,EmailConfirmed=true};
   var created=await users.CreateAsync(u,password);if(!created.Succeeded)throw new InvalidOperationException("Admin setup failed: "+string.Join(" ",created.Errors.Select(e=>e.Description)));
   var assigned=await users.AddToRoleAsync(u,Roles.Admin);if(!assigned.Succeeded)throw new InvalidOperationException("Admin role setup failed.");
  }
 }
}
