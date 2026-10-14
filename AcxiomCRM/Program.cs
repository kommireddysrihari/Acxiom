using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
var builder=WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CrmDb>(o=>o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddIdentity<ApplicationUser,IdentityRole>(o=>{
 o.Password.RequiredLength=10;o.Password.RequireDigit=true;o.Password.RequireLowercase=true;o.Password.RequireUppercase=true;o.Password.RequireNonAlphanumeric=true;
 o.Lockout.MaxFailedAccessAttempts=5;o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);o.User.RequireUniqueEmail=true;
}).AddEntityFrameworkStores<CrmDb>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o=>{
 o.LoginPath="/Account/Login";o.AccessDeniedPath="/Account/Denied";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;
 o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
 o.ExpireTimeSpan=TimeSpan.FromMinutes(30);o.SlidingExpiration=true;
 o.Events.OnRedirectToLogin=c=>{if(c.Request.Path.StartsWithSegments("/api")){c.Response.StatusCode=401;return Task.CompletedTask;}c.Response.Redirect(c.RedirectUri);return Task.CompletedTask;};
 o.Events.OnRedirectToAccessDenied=c=>{if(c.Request.Path.StartsWithSegments("/api")){c.Response.StatusCode=403;return Task.CompletedTask;}c.Response.Redirect(c.RedirectUri);return Task.CompletedTask;};
});
builder.Services.Configure<SecurityStampValidatorOptions>(o=>o.ValidationInterval=TimeSpan.Zero);
builder.Services.AddAntiforgery(o=>o.HeaderName="X-CSRF-TOKEN");
builder.Services.AddControllersWithViews(o=>o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddHttpContextAccessor();builder.Services.AddScoped<CrmService>();builder.Services.AddScoped<Auditor>();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.AddPolicy("auth",c=>RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
var app=builder.Build();
app.UseExceptionHandler(handler=>handler.Run(async context=>{context.Response.StatusCode=500;await context.Response.WriteAsJsonAsync(new{error="An unexpected error occurred. Please retry or contact the administrator."});}));
if(!app.Environment.IsDevelopment()){app.UseHsts();app.UseHttpsRedirection();}
app.Use(async(c,next)=>{c.Response.Headers["X-Content-Type-Options"]="nosniff";c.Response.Headers["X-Frame-Options"]="DENY";c.Response.Headers["Referrer-Policy"]="same-origin";await next();});
app.UseStaticFiles();app.UseRouting();app.UseRateLimiter();app.UseAuthentication();
// Check activation on every request, including existing authenticated cookies.
app.Use(async(c,next)=>{if(c.User.Identity?.IsAuthenticated==true){var manager=c.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();var u=await manager.GetUserAsync(c.User);if(u==null || !u.IsActive){await c.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>().SignOutAsync();c.Response.StatusCode=401;return;}}await next();});
app.UseAuthorization();app.MapControllers();app.MapControllerRoute("default","{controller=Dashboard}/{action=Index}/{id?}");
await Seed.Initialize(app.Services,app.Configuration);
app.Run();
public partial class Program {}
