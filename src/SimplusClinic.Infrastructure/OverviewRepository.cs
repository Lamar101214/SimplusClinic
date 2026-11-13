using System.Data;
using SimplusClinic.Domain;
using SimplusClinic.Application;
namespace SimplusClinic.Infrastructure;
public sealed class OverviewRepository : IOverviewRepository
{
 public async Task<IReadOnlyList<LookupItem>> PatientsAsync(int includeId=0,CancellationToken ct=default)
 => await DatabaseHelper.QueryAsync("SELECT Id, CONCAT(FirstName,N' ',LastName,N' · #',Id,CASE WHEN IsArchived=1 THEN N' (archived)' ELSE N'' END) Name FROM dbo.Patients WHERE IsArchived=0 OR Id=@id ORDER BY LastName,FirstName,Id",r=>new LookupItem((int)r["Id"],(string)r["Name"]),[DatabaseHelper.P("@id",SqlDbType.Int,includeId)],ct);
 public async Task<IReadOnlyList<LookupItem>> DoctorsAsync(int includeId=0,CancellationToken ct=default)
 => await DatabaseHelper.QueryAsync("SELECT Id, CONCAT(FirstName,N' ',LastName,N' · #',Id,CASE WHEN IsArchived=1 THEN N' (archived)' ELSE N'' END) Name FROM dbo.Doctors WHERE IsArchived=0 OR Id=@id ORDER BY LastName,FirstName,Id",r=>new LookupItem((int)r["Id"],(string)r["Name"]),[DatabaseHelper.P("@id",SqlDbType.Int,includeId)],ct);
 public async Task<IReadOnlyList<string>> SpecializationsAsync(CancellationToken ct=default)
 => await DatabaseHelper.QueryAsync("SELECT DISTINCT Specialization FROM dbo.Doctors WHERE IsArchived=0 ORDER BY Specialization",r=>(string)r[0],[],ct);
 public async Task<DashboardSummary> SummaryAsync(DateTime day,CancellationToken ct=default)
 => (await DatabaseHelper.QueryAsync("""
 SELECT (SELECT COUNT(*) FROM dbo.Patients WHERE IsArchived=0) Patients,
 (SELECT COUNT(*) FROM dbo.Doctors WHERE IsArchived=0) Doctors,
 (SELECT COUNT(*) FROM dbo.Appointments WHERE StartAt>=@day AND StartAt<DATEADD(day,1,@day) AND Status<>N'Cancelled') Appointments,
 (SELECT COUNT(*) FROM dbo.MedicalRecords WHERE IsArchived=0) Records;
 """,r=>new DashboardSummary((int)r[0],(int)r[1],(int)r[2],(int)r[3]),[DatabaseHelper.P("@day",SqlDbType.Date,day.Date)],ct))[0];
 public async Task<IReadOnlyList<Appointment>> ScheduleAsync(DateTime day,CancellationToken ct=default)
 => await DatabaseHelper.QueryAsync("""
 SELECT a.*,CONCAT(p.FirstName,N' ',p.LastName) PatientName,CONCAT(d.FirstName,N' ',d.LastName) DoctorName
 FROM dbo.Appointments a JOIN dbo.Patients p ON p.Id=a.PatientId JOIN dbo.Doctors d ON d.Id=a.DoctorId
 WHERE a.StartAt>=@day AND a.StartAt<DATEADD(day,1,@day) ORDER BY a.StartAt,a.Id;
 """,AppointmentRepository.Map,[DatabaseHelper.P("@day",SqlDbType.Date,day.Date)],ct);
 public async Task<IReadOnlyList<ReportRow>> ReportAsync(ReportKind kind,int doctorId,string specialization,int page,CancellationToken ct=default)
 {
  // Only fixed SQL identifiers; all user input remains parameterized.
  string sql=kind switch {
   ReportKind.Patients => "SELECT p.Id,CONCAT(p.FirstName,N' ',p.MiddleName,N' ',p.LastName) Name,p.CityProvince Detail,p.MobileNumber FROM dbo.Patients p WHERE p.IsArchived=0",
   ReportKind.Doctors => "SELECT d.Id,CONCAT(d.FirstName,N' ',d.MiddleName,N' ',d.LastName) Name,d.Specialization Detail,d.MobileNumber FROM dbo.Doctors d WHERE d.IsArchived=0",
   ReportKind.PatientsPerDoctor => """
   SELECT p.Id,CONCAT(p.FirstName,N' ',p.MiddleName,N' ',p.LastName) Name,p.CityProvince Detail,p.MobileNumber FROM dbo.Patients p WHERE p.IsArchived=0 AND
   (EXISTS(SELECT 1 FROM dbo.MedicalRecords m WHERE m.PatientId=p.Id AND m.DoctorId=@doctor AND m.IsArchived=0)
    OR EXISTS(SELECT 1 FROM dbo.Appointments a WHERE a.PatientId=p.Id AND a.DoctorId=@doctor AND a.Status<>N'Cancelled'))
   """,
   ReportKind.DoctorsPerSpecialization => "SELECT d.Id,CONCAT(d.FirstName,N' ',d.MiddleName,N' ',d.LastName) Name,d.Specialization Detail,d.MobileNumber FROM dbo.Doctors d WHERE d.IsArchived=0 AND d.Specialization=@specialization",
   _=>throw new ClinicException("Unknown report.") };
  return await DatabaseHelper.QueryAsync(sql+" ORDER BY Name,Id OFFSET @skip ROWS FETCH NEXT 100 ROWS ONLY",r=>new ReportRow((int)r[0],(string)r[1],(string)r[2],(string)r[3]),[DatabaseHelper.P("@doctor",SqlDbType.Int,doctorId),DatabaseHelper.P("@specialization",SqlDbType.NVarChar,specialization,120),DatabaseHelper.P("@skip",SqlDbType.Int,checked(page*100))],ct);
 }
}
