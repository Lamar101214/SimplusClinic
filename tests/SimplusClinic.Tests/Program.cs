using SimplusClinic.Domain;
using SimplusClinic.Application;
using SimplusClinic.Infrastructure;
using System.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;

int passed=0;
void Check(bool condition,string label){if(!condition)throw new Exception("FAIL: "+label);Console.WriteLine("PASS: "+label);passed++;}
void Invalid(Entity value,string label){try{EntityValidator.Validate(value);}catch(ClinicException){Check(true,label);return;}throw new Exception("FAIL: "+label);}
var day=new DateTime(2030,1,1,10,0,0);
Appointment Booking(int id,int patient,int doctor,int from,int to,string status="Scheduled")=>new(){Id=id,PatientId=patient,DoctorId=doctor,StartAt=day.AddMinutes(from),EndAt=day.AddMinutes(to),Status=status};
var existing=Booking(1,1,1,0,30);
Check(AppointmentRules.Conflicts(Booking(2,2,1,15,45),existing),"Same doctor overlap");
Check(AppointmentRules.Conflicts(Booking(2,1,2,15,45),existing),"Same patient overlap");
Check(!AppointmentRules.Conflicts(Booking(2,2,2,15,45),existing),"Independent resources can overlap");
Check(!AppointmentRules.Conflicts(Booking(2,1,1,30,60),existing),"Adjacent intervals allowed");
Check(AppointmentRules.Conflicts(Booking(2,1,2,-15,60),existing),"Containing interval blocked");
Check(AppointmentRules.Conflicts(Booking(2,1,2,5,10),existing),"Contained interval blocked");
Check(!AppointmentRules.Conflicts(Booking(1,1,1,0,30),existing),"Self update excluded");
Check(!AppointmentRules.Conflicts(Booking(2,1,1,0,30,"Cancelled"),existing),"Cancelled request excluded");
Check(!AppointmentRules.Conflicts(existing,Booking(2,1,1,0,30,"Cancelled")),"Cancelled existing booking excluded");
Check(AppointmentRules.Conflicts(Booking(2,1,1,0,30,"Completed"),existing),"Completed visits reserve history interval");
Check(AppointmentRules.Overlaps(day.AddHours(13),day.AddHours(15),day.AddHours(14),day.AddHours(16)),"Midnight-crossing overlap");
Invalid(Booking(0,1,1,0,0),"Zero duration rejected");Invalid(Booking(0,1,1,30,0),"Negative duration rejected");Invalid(Booking(0,1,1,0,1441),"Over 24 hours rejected");Invalid(Booking(0,0,1,0,30),"Missing patient rejected");Invalid(Booking(0,1,1,0,30,"Unknown"),"Invalid status rejected");

var patient=new Patient{FirstName=" Ada ",LastName=" Test ",CityProvince="Manila",DateOfBirth=new DateTime(2000,1,1)};
EntityValidator.Validate(patient);Check(patient.FirstName=="Ada","Whitespace normalized; optional email accepted");
patient.EmailAddress="invalid";Invalid(patient,"Invalid email rejected");patient.EmailAddress="ada@example.test";EntityValidator.Validate(patient);Check(true,"Valid email accepted");
patient.DateOfBirth=DateTime.Today.AddDays(1);Invalid(patient,"Future birth date rejected");patient.DateOfBirth=new DateTime(2000,1,1);patient.FirstName=new string('x',81);Invalid(patient,"Oversized names rejected");
Check(DatabaseHelper.Like("A%_[~") == "%A~%~_~[~~%","LIKE wildcards are escaped");

var fake=new FakeAppointments { Conflict=true };
var service=new AppointmentService(fake);

try{await service.SaveAsync(Booking(0,1,1,0,30));
    throw new Exception("Conflict was not blocked");
             }
      catch(ClinicException){Check(fake.Saves==0,"Preflight blocks conflicting write");}

fake.Conflict=false;await service.SaveAsync(Booking(0,1,1,0,30));Check(fake.Saves==1,"Available booking reaches repository");
await AuthTests.RunAsync(Check);

if(args.Contains("--auth-integration")) await AuthIntegration.RunAsync(Check);
if(args.Contains("--integration"))await Integration.RunAsync(Check);

Console.WriteLine($"All {passed} checks passed.");

sealed class FakeAppointments : IAppointmentRepository
{
 public bool Conflict;public int Saves;
 public Task<ConflictResult> CheckConflictAsync(Appointment a,CancellationToken ct=default)=>Task.FromResult(new ConflictResult(Conflict,"Conflict"));
 public Task<int> SaveAsync(Appointment a,CancellationToken ct=default){Saves++;return Task.FromResult(1);}
 public Task<IReadOnlyList<Appointment>> SearchAsync(SearchCriteria c,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<Appointment>>([]);
}

static class Integration
{
 public static async Task RunAsync(Action<bool,string> check)
 {
  string connection=Environment.GetEnvironmentVariable("SIMPLUS_TEST_CONNECTION")??
            throw new Exception("Set SIMPLUS_TEST_CONNECTION to a disposable SimplusClinic database. Integration tests leave clearly labelled synthetic records.");
  
  var config=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
  config.ConnectionStrings.ConnectionStrings.Remove("SimplusClinic");config.ConnectionStrings.ConnectionStrings.Add(new("SimplusClinic",connection,"Microsoft.Data.SqlClient"));config.Save(ConfigurationSaveMode.Modified);ConfigurationManager.RefreshSection("connectionStrings");
  
 var pr=new PatientRepository();var dr=new DoctorRepository();var ar=new AppointmentRepository();var mr=new MedicalRecordRepository();
 string marker="TEST-"+Guid.NewGuid().ToString("N")[..12];

  async Task<int> AddPatient(string suffix)=>await pr.SaveAsync
            (new Patient{FirstName=marker,LastName=suffix,CityProvince="Synthetic",DateOfBirth=new DateTime(2000,1,1)});

  async Task<int> AddDoctor(string suffix)=>await dr.SaveAsync
            (new Doctor{FirstName=marker,LastName=suffix,CityProvince="Synthetic",DateOfBirth=new DateTime(1980,1,1),Specialization="Test",LicenseNumber=marker+suffix});
  
  int p1=await AddPatient("P1"),p2=await AddPatient("P2"),d1=await AddDoctor("D1"),d2=await AddDoctor("D2");
  DateTime start=new DateTime(2040,1,1,10,0,0).AddDays(Random.Shared.Next(0,1000));
  Appointment A(int patient,int doctor,int offset=0)=>new(){PatientId=patient,DoctorId=doctor,StartAt=start.AddMinutes(offset),EndAt=start.AddMinutes(offset+30),Status="Scheduled"};
  async Task<int> Attempt(Appointment a){try{return await ar.SaveAsync(a);}catch(ClinicException ex) when(ex.Message.Contains("Schedule conflict")){return 0;}}

  var simultaneous=await Task.WhenAll(Attempt(A(p1,d1)),Attempt(A(p2,d1)));
  check(simultaneous.Count(id=>id>0)==1,"SQL: simultaneous same-doctor writes allow exactly one");
  int winningPatient=simultaneous[0]>0?p1:p2;

  check(await Attempt(A(winningPatient,d2))==0,"SQL: same patient blocked across doctors");
  check(await Attempt(A(p2,d1,30))>0,"SQL: adjacent interval allowed");
  var appointments=await ar.SearchAsync(new(DoctorId:d1.ToString()));
  foreach(var item in appointments.Where(x=>x.DoctorId==d1)){item.Status="Cancelled";
            await ar.SaveAsync(item);}

  check(await Attempt(A(p1,d1))>0,"SQL: cancelled slots can be reused");

  var patients=await pr.SearchAsync(new(Id:p1.ToString()));var patient=patients.Single(x=>x.Id==p1);var stale=patients.Single(x=>x.Id==p1);
  await pr.SaveAsync(patient);
  try
        {await pr.SaveAsync(stale);
            throw new Exception("Stale update accepted");}catch(ClinicException ex) 
            when(ex.Message.Contains("another user")){check(true,"SQL: stale rowversion rejected");}
  patient=(await pr.SearchAsync(new(Id:p1.ToString()))).Single(x=>x.Id==p1);patient.IsArchived=true;

  try
        {await pr.SaveAsync(patient);
            throw new Exception("Scheduled archive accepted");}
                catch(ClinicException ex) 
                    when(ex.Message.Contains("scheduled"))
                        {check(true,"SQL: archive blocked for scheduled visits");}

  int recordId=await mr.SaveAsync(new MedicalRecord{PatientId=p1,DoctorId=d1,Diagnosis="Synthetic test",Treatment="Testing",TreatmentDate=DateTime.Today});
  var record=(await mr.SearchAsync(new(PatientId:p1.ToString()))).Single(x=>x.Id==recordId);record.Diagnosis="Synthetic revision";await mr.SaveAsync(record);
 
  await using var db=new SqlConnection(connection);await db.OpenAsync();
  await using(var cmd=new SqlCommand("SELECT COUNT(*) FROM dbo.MedicalRecords FOR SYSTEM_TIME ALL WHERE Id=@id",db)){cmd.Parameters.Add("@id",SqlDbType.Int).Value=recordId;check(Convert.ToInt32(await cmd.ExecuteScalarAsync())>=2,"SQL: medical record history retained");}
  // Leave no scheduled test visits; preserve the audit evidence and synthetic entities.

  foreach(var item in await ar.SearchAsync(new(DoctorId:d1.ToString()))){if(item.DoctorId==d1){item.Status="Cancelled";
                await ar.SaveAsync(item);}}

  Console.WriteLine("Synthetic data marker: "+marker+". Drop the disposable database after inspection.");
 }
}
