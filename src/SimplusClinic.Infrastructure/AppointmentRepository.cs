using System.Data;
using Microsoft.Data.SqlClient;
using SimplusClinic.Domain;
using SimplusClinic.Application;
namespace SimplusClinic.Infrastructure;
public sealed class AppointmentRepository : IAppointmentRepository
{
 public async Task<IReadOnlyList<Appointment>> SearchAsync(SearchCriteria c, CancellationToken ct = default)
 => await DatabaseHelper.QueryAsync("""
 SELECT x.*, CONCAT(p.FirstName,N' ',p.LastName) AS PatientName, CONCAT(d.FirstName,N' ',d.LastName) AS DoctorName FROM dbo.Appointments x JOIN dbo.Patients p ON p.Id=x.PatientId JOIN dbo.Doctors d ON d.Id=x.DoctorId
 WHERE (@pid=N'' OR CONVERT(nvarchar(20),x.PatientId) LIKE @pidLike ESCAPE '~') AND (@did=N'' OR CONVERT(nvarchar(20),x.DoctorId) LIKE @didLike ESCAPE '~') AND (CONCAT(p.FirstName,N' ',p.LastName) LIKE @patient ESCAPE '~' OR CONCAT(p.LastName,N' ',p.FirstName) LIKE @patient ESCAPE '~') AND (CONCAT(d.FirstName,N' ',d.LastName) LIKE @doctor ESCAPE '~' OR CONCAT(d.LastName,N' ',d.FirstName) LIKE @doctor ESCAPE '~')
 ORDER BY x.StartAt DESC,x.Id DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
 """, Map, SearchParameters.Create(c), ct);
 public Task<int> SaveAsync(Appointment value, CancellationToken ct = default)
 => DatabaseHelper.SaveAsync("dbo.SaveAppointment", new[] {
 DatabaseHelper.P("@Id",SqlDbType.Int,value.Id), DatabaseHelper.P("@Version",SqlDbType.Binary,value.Version.Length==0?null:value.Version,8),
 DatabaseHelper.P("@PatientId",SqlDbType.Int,value.PatientId),
 DatabaseHelper.P("@DoctorId",SqlDbType.Int,value.DoctorId),
 DatabaseHelper.P("@StartAt",SqlDbType.DateTime2,value.StartAt),
 DatabaseHelper.P("@EndAt",SqlDbType.DateTime2,value.EndAt),
 DatabaseHelper.P("@Status",SqlDbType.NVarChar,value.Status,20),
 },ct);
 internal static Appointment Map(SqlDataReader r) => new() {
 Id=(int)r["Id"],Version=(byte[])r["Version"],
 PatientId=(int)r["PatientId"],
 DoctorId=(int)r["DoctorId"],
 StartAt=(DateTime)r["StartAt"],
 EndAt=(DateTime)r["EndAt"],
 Status=(string)r["Status"],
 PatientName=(string)r["PatientName"],DoctorName=(string)r["DoctorName"],
 };
 public async Task<ConflictResult> CheckConflictAsync(Appointment a, CancellationToken ct = default)
 {
  if(a.Status=="Cancelled") return new(false,"Cancelled appointments do not reserve time.");
  var rows=await DatabaseHelper.QueryAsync("""
  SELECT TOP(1) Id FROM dbo.Appointments WHERE Id<>@id AND Status<>N'Cancelled'
  AND StartAt < @end AND EndAt > @start AND (DoctorId=@doctor OR PatientId=@patient);
  """,r=>(int)r["Id"],new[]{DatabaseHelper.P("@id",SqlDbType.Int,a.Id),DatabaseHelper.P("@end",SqlDbType.DateTime2,a.EndAt),DatabaseHelper.P("@start",SqlDbType.DateTime2,a.StartAt),DatabaseHelper.P("@doctor",SqlDbType.Int,a.DoctorId),DatabaseHelper.P("@patient",SqlDbType.Int,a.PatientId)},ct);
  return rows.Count>0?new(true,"Schedule conflict: this patient or doctor is already booked during this interval."):new(false,"Time available · checked against saved appointments");
 }
}
