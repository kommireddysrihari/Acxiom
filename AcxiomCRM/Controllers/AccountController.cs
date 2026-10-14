using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace AcxiomCRM.Controllers;
[EnableRateLimiting("auth")]
public class AccountController(UserManager<ApplicationUser> users,SignInManager<ApplicationUser> signIn,Auditor audit,CrmDb db,IConfiguration config):Controller {
 [HttpGet,AllowAnonymous] public IActionResult Login()=>View(new LoginVm());
 [HttpPost,AllowAnonymous] public async Task<IActionResult> Login(LoginVm vm){
  if(!ModelState.IsValid)return View(vm);
  var user=await users.FindByEmailAsync(vm.Login)??await users.FindByNameAsync(vm.Login);
  var result=user!=null&&user.IsActive?await signIn.PasswordSignInAsync(user,vm.Password,false,true):Microsoft.AspNetCore.Identity.SignInResult.Failed;
  await audit.Event(result.Succeeded?"Login":"FailedLogin","Authentication",user?.Id??"unknown",result.IsLockedOut?"LockedOut":result.Succeeded?"Success":"Failure",user?.Id);
  if(result.Succeeded)return RedirectToAction("Index","Dashboard");
  ModelState.AddModelError("","Login failed. Check credentials or wait if your account is locked.");return View(vm);
 }
 [HttpGet,AllowAnonymous] public IActionResult Register()=>config.GetValue<bool>("Registration:Enabled")?View(new RegisterVm()):NotFound();
 [HttpPost,AllowAnonymous] public async Task<IActionResult> Register(RegisterVm vm){
  if(!config.GetValue<bool>("Registration:Enabled"))return NotFound();
  if(!ModelState.IsValid)return View(vm);
  await using var tx=await db.Database.BeginTransactionAsync();
  var u=new ApplicationUser{UserName=vm.Email.Trim(),Email=vm.Email.Trim()};var result=await users.CreateAsync(u,vm.Password);
  if(result.Succeeded){var assigned=await users.AddToRoleAsync(u,Roles.Sales);if(assigned.Succeeded){await audit.Event("Register","Users",u.Id,userId:u.Id);await tx.CommitAsync();await signIn.SignInAsync(u,false);return RedirectToAction("Index","Dashboard");}result=assigned;}
  foreach(var e in result.Errors)ModelState.AddModelError("",e.Description);return View(vm);
 }
 [HttpPost,Authorize] public async Task<IActionResult> Logout(){await audit.Event("Logout","Authentication",users.GetUserId(User)!);await signIn.SignOutAsync();return RedirectToAction("Login");}
 [HttpGet] public IActionResult Denied(){Response.StatusCode=403;return View();}
}
