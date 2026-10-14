using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize(Roles=Roles.Admin)]
public class AdminController(UserManager<ApplicationUser> users,CrmDb db,Auditor audit):Controller {
 public async Task<IActionResult> Index(string q=""){
  var list=await users.Users.Where(x=>x.Email!.Contains(q)).OrderBy(x=>x.Email).Take(200).ToListAsync();
  var rows=new List<UserVm>();foreach(var u in list)rows.Add(new(){Id=u.Id,Email=u.Email!,Role=(await users.GetRolesAsync(u)).SingleOrDefault()??Roles.Sales,IsActive=u.IsActive,Unlock=await users.IsLockedOutAsync(u)});
  return View(rows);
 }
 [HttpGet] public async Task<IActionResult> Edit(string? id){if(id==null)return View(new UserVm());var u=await users.FindByIdAsync(id);if(u==null)return NotFound();return View(new UserVm{Id=u.Id,Email=u.Email!,Role=(await users.GetRolesAsync(u)).Single(),IsActive=u.IsActive});}
 [HttpPost] public async Task<IActionResult> Edit(UserVm vm){
  if(!Roles.All.Contains(vm.Role))ModelState.AddModelError(nameof(vm.Role),"Choose a valid role.");
  if(!ModelState.IsValid)return View(vm);
  await using var tx=await db.Database.BeginTransactionAsync();
  var isNew=string.IsNullOrEmpty(vm.Id);var u=isNew?new ApplicationUser():await users.FindByIdAsync(vm.Id);if(u==null)return NotFound();
  if(!isNew && u.Id==users.GetUserId(User) && (!vm.IsActive || vm.Role!=Roles.Admin)){ModelState.AddModelError("","You cannot deactivate or demote your own admin account.");return View(vm);}
  async Task<bool> Check(IdentityResult result){await Task.CompletedTask;foreach(var e in result.Errors)ModelState.AddModelError("",e.Description);return result.Succeeded;}
  var oldRole=isNew?null:(await users.GetRolesAsync(u)).SingleOrDefault();
  u.Email=vm.Email.Trim();u.UserName=vm.Email.Trim();u.IsActive=vm.IsActive;
  if(isNew){if(string.IsNullOrWhiteSpace(vm.Password)){ModelState.AddModelError(nameof(vm.Password),"Password required for a new user.");return View(vm);}if(!await Check(await users.CreateAsync(u,vm.Password)))return View(vm);}
  else {
   if(!await Check(await users.UpdateAsync(u)))return View(vm);
   if(!string.IsNullOrEmpty(vm.Password)){var token=await users.GeneratePasswordResetTokenAsync(u);if(!await Check(await users.ResetPasswordAsync(u,token,vm.Password)))return View(vm);audit.Add("PasswordReset","Users",u.Id);}
  }
  if(oldRole!=vm.Role){if(oldRole!=null && !await Check(await users.RemoveFromRoleAsync(u,oldRole)))return View(vm);if(!await Check(await users.AddToRoleAsync(u,vm.Role)))return View(vm);audit.Add("RoleChange","Users",u.Id,new{Role=oldRole},new{vm.Role});}
  if(vm.Unlock){if(!await Check(await users.SetLockoutEndDateAsync(u,null))||!await Check(await users.ResetAccessFailedCountAsync(u)))return View(vm);audit.Add("Unlock","Users",u.Id);}
  if(!await Check(await users.UpdateSecurityStampAsync(u)))return View(vm);
  audit.Add(isNew?"Create":"Update","Users",u.Id,after:new{u.Email,u.IsActive,vm.Role});await db.SaveChangesAsync();await tx.CommitAsync();TempData["Message"]="User saved.";return RedirectToAction("Index");
 }
 public async Task<IActionResult> Audit(string q="",string module="",string user="",DateTime? from=null,DateTime? to=null,int page=1){
  var query=db.AuditLogs.AsNoTracking().AsQueryable();if(q!="")query=query.Where(a=>a.Action.Contains(q));if(module!="")query=query.Where(a=>a.EntityName==module);if(user!="")query=query.Where(a=>a.UserId==user);if(from.HasValue)query=query.Where(a=>a.CreatedDate>=from.Value.Date);if(to.HasValue){var end=to.Value.Date.AddDays(1);query=query.Where(a=>a.CreatedDate<end);}
  ViewBag.Page=Math.Max(1,page);ViewBag.Total=await query.CountAsync();return View(await query.OrderByDescending(a=>a.CreatedDate).Skip((Math.Max(1,page)-1)*50).Take(50).ToListAsync());
 }
}
