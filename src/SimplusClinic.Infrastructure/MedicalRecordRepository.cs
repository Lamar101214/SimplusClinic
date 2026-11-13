using System.Data;
using Microsoft.Data.SqlClient;
using SimplusClinic.Domain;
using SimplusClinic.Application;
namespace SimplusClinic.Infrastructure;
public sealed class MedicalRecordRepository : IRepository<MedicalRecord>
{
 public async Task<IReadOnlyList<MedicalRecord>> SearchAsync(SearchCriteria c, CancellationToken ct = default)
 => await DatabaseHelper.QueryAsync("""
 SELECT x.*, CONCAT(p.FirstName,N' ',p.LastName) AS PatientName, CONCAT(d.FirstName,N' ',d.LastName) AS DoctorName FROM dbo.MedicalRecords x JOIN dbo.Patients p ON p.Id=x.PatientId JOIN dbo.Doctors d ON d.Id=x.DoctorId
 WHERE (@pid=N'' OR CONVERT(nvarchar(20),x.PatientId) LIKE @pidLike ESCAPE '~') AND (@did=N'' OR CONVERT(nvarchar(20),x.DoctorId) LIKE @didLike ESCAPE '~') AND (CONCAT(p.FirstName,N' ',p.LastName) LIKE @patient ESCAPE '~' OR CONCAT(p.LastName,N' ',p.FirstName) LIKE @patient ESCAPE '~') AND (CONCAT(d.FirstName,N' ',d.LastName) LIKE @doctor ESCAPE '~' OR CONCAT(d.LastName,N' ',d.FirstName) LIKE @doctor ESCAPE '~') AND (@archived=1 OR x.IsArchived=0)
 ORDER BY x.TreatmentDate DESC,x.Id DESC OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
 """, Map, SearchParameters.Create(c), ct);
 public Task<int> SaveAsync(MedicalRecord value, CancellationToken ct = default)
 => DatabaseHelper.SaveAsync("dbo.SaveMedicalRecord", new[] {
 DatabaseHelper.P("@Id",SqlDbType.Int,value.Id), DatabaseHelper.P("@Version",SqlDbType.Binary,value.Version.Length==0?null:value.Version,8),
 DatabaseHelper.P("@PatientId",SqlDbType.Int,value.PatientId),
 DatabaseHelper.P("@DoctorId",SqlDbType.Int,value.DoctorId),
 DatabaseHelper.P("@Diagnosis",SqlDbType.NVarChar,value.Diagnosis,4000),
 DatabaseHelper.P("@Treatment",SqlDbType.NVarChar,value.Treatment,4000),
 DatabaseHelper.P("@Prescriptions",SqlDbType.NVarChar,value.Prescriptions,4000),
 DatabaseHelper.P("@TreatmentDate",SqlDbType.Date,value.TreatmentDate),
 DatabaseHelper.P("@IsArchived",SqlDbType.Bit,value.IsArchived),
 },ct);
 internal static MedicalRecord Map(SqlDataReader r) => new() {
 Id=(int)r["Id"],Version=(byte[])r["Version"],
 PatientId=(int)r["PatientId"],
 DoctorId=(int)r["DoctorId"],
 Diagnosis=(string)r["Diagnosis"],
 Treatment=(string)r["Treatment"],
 Prescriptions=(string)r["Prescriptions"],
 TreatmentDate=(DateTime)r["TreatmentDate"],
 IsArchived=(bool)r["IsArchived"],
 PatientName=(string)r["PatientName"],DoctorName=(string)r["DoctorName"],
 };
}
