using System.Data;
using Microsoft.Data.SqlClient;
using SimplusClinic.Domain;
using SimplusClinic.Application;
namespace SimplusClinic.Infrastructure;
public sealed class DoctorRepository : IRepository<Doctor>
{
 public async Task<IReadOnlyList<Doctor>> SearchAsync(SearchCriteria c, CancellationToken ct = default)
 => await DatabaseHelper.QueryAsync("""
 SELECT x.* FROM dbo.Doctors x
 WHERE (@archived=1 OR x.IsArchived=0) AND (@id=N'' OR CONVERT(nvarchar(20),x.Id) LIKE @idLike ESCAPE '~') AND (@name=N'' OR CONCAT(x.FirstName,N' ',NULLIF(x.MiddleName,N''),N' ',x.LastName) LIKE @nameLike ESCAPE '~' OR CONCAT(x.FirstName,N' ',x.LastName) LIKE @nameLike ESCAPE '~' OR CONCAT(x.LastName,N' ',x.FirstName,N' ',x.MiddleName) LIKE @nameLike ESCAPE '~') AND x.Specialization LIKE @specialization ESCAPE '~' AND x.LicenseNumber LIKE @license ESCAPE '~'
 ORDER BY x.LastName,x.FirstName,x.Id OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
 """, Map, SearchParameters.Create(c), ct);
 public Task<int> SaveAsync(Doctor value, CancellationToken ct = default)
 => DatabaseHelper.SaveAsync("dbo.SaveDoctor", new[] {
 DatabaseHelper.P("@Id",SqlDbType.Int,value.Id), DatabaseHelper.P("@Version",SqlDbType.Binary,value.Version.Length==0?null:value.Version,8),
 DatabaseHelper.P("@FirstName",SqlDbType.NVarChar,value.FirstName,80),
 DatabaseHelper.P("@MiddleName",SqlDbType.NVarChar,value.MiddleName,80),
 DatabaseHelper.P("@LastName",SqlDbType.NVarChar,value.LastName,80),
 DatabaseHelper.P("@HouseNo",SqlDbType.NVarChar,value.HouseNo,30),
 DatabaseHelper.P("@Street",SqlDbType.NVarChar,value.Street,120),
 DatabaseHelper.P("@Municipality",SqlDbType.NVarChar,value.Municipality,100),
 DatabaseHelper.P("@CityProvince",SqlDbType.NVarChar,value.CityProvince,100),
 DatabaseHelper.P("@ZipCode",SqlDbType.NVarChar,value.ZipCode,15),
 DatabaseHelper.P("@DateOfBirth",SqlDbType.Date,value.DateOfBirth),
 DatabaseHelper.P("@Gender",SqlDbType.NVarChar,value.Gender,30),
 DatabaseHelper.P("@MobileNumber",SqlDbType.NVarChar,value.MobileNumber,30),
 DatabaseHelper.P("@LandlineNumber",SqlDbType.NVarChar,value.LandlineNumber,30),
 DatabaseHelper.P("@EmailAddress",SqlDbType.NVarChar,value.EmailAddress,254),
 DatabaseHelper.P("@Specialization",SqlDbType.NVarChar,value.Specialization,120),
 DatabaseHelper.P("@LicenseNumber",SqlDbType.NVarChar,value.LicenseNumber,60),
 DatabaseHelper.P("@IsArchived",SqlDbType.Bit,value.IsArchived),
 },ct);
 internal static Doctor Map(SqlDataReader r) => new() {
 Id=(int)r["Id"],Version=(byte[])r["Version"],
 FirstName=(string)r["FirstName"],
 MiddleName=(string)r["MiddleName"],
 LastName=(string)r["LastName"],
 HouseNo=(string)r["HouseNo"],
 Street=(string)r["Street"],
 Municipality=(string)r["Municipality"],
 CityProvince=(string)r["CityProvince"],
 ZipCode=(string)r["ZipCode"],
 DateOfBirth=(DateTime)r["DateOfBirth"],
 Gender=(string)r["Gender"],
 MobileNumber=(string)r["MobileNumber"],
 LandlineNumber=(string)r["LandlineNumber"],
 EmailAddress=(string)r["EmailAddress"],
 Specialization=(string)r["Specialization"],
 LicenseNumber=(string)r["LicenseNumber"],
 IsArchived=(bool)r["IsArchived"],
 };
}
