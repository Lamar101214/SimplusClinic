using System.Data;
using Microsoft.Data.SqlClient;
using SimplusClinic.Domain;
namespace SimplusClinic.Infrastructure;
internal static class SearchParameters
{
 public static SqlParameter[] Create(SearchCriteria c) => [
 DatabaseHelper.P("@archived",SqlDbType.Bit,c.IncludeArchived),
 DatabaseHelper.P("@id",SqlDbType.NVarChar,c.Id,100), DatabaseHelper.P("@idLike",SqlDbType.NVarChar,DatabaseHelper.Like(c.Id),220),
 DatabaseHelper.P("@name",SqlDbType.NVarChar,c.Name,250), DatabaseHelper.P("@nameLike",SqlDbType.NVarChar,DatabaseHelper.Like(c.Name),520),
 DatabaseHelper.P("@specialization",SqlDbType.NVarChar,DatabaseHelper.Like(c.Specialization),260),
 DatabaseHelper.P("@license",SqlDbType.NVarChar,DatabaseHelper.Like(c.LicenseNumber),140),
 DatabaseHelper.P("@pid",SqlDbType.NVarChar,c.PatientId,100), DatabaseHelper.P("@pidLike",SqlDbType.NVarChar,DatabaseHelper.Like(c.PatientId),220),
 DatabaseHelper.P("@did",SqlDbType.NVarChar,c.DoctorId,100), DatabaseHelper.P("@didLike",SqlDbType.NVarChar,DatabaseHelper.Like(c.DoctorId),220),
 DatabaseHelper.P("@patient",SqlDbType.NVarChar,DatabaseHelper.Like(c.PatientName),520), DatabaseHelper.P("@doctor",SqlDbType.NVarChar,DatabaseHelper.Like(c.DoctorName),520),
 DatabaseHelper.P("@skip",SqlDbType.Int,checked(c.Page*c.PageSize)), DatabaseHelper.P("@take",SqlDbType.Int,c.PageSize)];
}
