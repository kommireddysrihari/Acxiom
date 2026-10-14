using AcxiomCRM.ViewModels;
using AcxiomCRM.Services;
using System.ComponentModel.DataAnnotations;
int passed=0;
void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);Console.WriteLine("PASS "+name);passed++;}
bool Valid(RecordDto dto){var errors=new List<ValidationResult>();return Validator.TryValidateObject(dto,new ValidationContext(dto),errors,true);}
RecordDto Customer()=>new(){Kind="customers",Name="Sample",Status="Active",AssignedTo="owner",Email="sample@example.com",Phone="9876543210"};
RecordDto Opportunity()=>new(){Kind="opportunities",Name="Contract",Status="Open",Stage="Proposal",AssignedTo="owner",CustomerId=Guid.NewGuid(),Amount=1000,Probability=50,ExpectedCloseDate=DateTime.UtcNow.Date.AddDays(1)};
Check(Valid(Customer()),"valid customer");
var c=Customer();c.Email="invalid";Check(!Valid(c),"invalid email");
c=Customer();c.Phone="123";Check(!Valid(c),"invalid phone");
c=Customer();c.Name=new string('x',121);Check(!Valid(c),"name maximum length");
c=Customer();c.Name=" ";Check(!Valid(c),"blank name");
var o=Opportunity();Check(Valid(o),"valid opportunity");
o.Amount=0;Check(!Valid(o),"zero opportunity amount");
o=Opportunity();o.Probability=101;Check(!Valid(o),"probability upper bound");
o=Opportunity();o.Probability=-1;Check(!Valid(o),"probability lower bound");
o=Opportunity();o.ExpectedCloseDate=DateTime.UtcNow.Date.AddDays(-1);Check(!Valid(o),"active close date in past");
o=Opportunity();o.Stage="Won";Check(!Valid(o),"stage/status consistency");
o=Opportunity();o.CustomerId=null;Check(!Valid(o),"required opportunity customer");
var f=new RecordDto{Kind="followups",Name="Call",Status="Planned",AssignedTo="owner",CustomerId=Guid.NewGuid(),FollowUpDate=DateTime.UtcNow.Date.AddDays(-1)};
Check(!Valid(f),"past planned follow-up");f.FollowUpDate=DateTime.UtcNow.Date;Check(Valid(f),"today follow-up");
Check(CrmService.CanTransition("New","Contacted"),"lead contact transition");
Check(CrmService.CanTransition("Contacted","Qualified"),"lead qualification transition");
Check(!CrmService.CanTransition("New","Qualified"),"skip qualification rejected");
Check(!CrmService.CanTransition("Qualified","Converted"),"ordinary conversion status rejected");
Check(!CrmService.CanTransition("Converted","New"),"converted lead terminal");
Console.WriteLine($"{passed} domain checks passed.");
