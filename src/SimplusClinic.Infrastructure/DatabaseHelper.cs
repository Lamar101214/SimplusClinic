using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using SimplusClinic.Application;
namespace SimplusClinic.Infrastructure;
public static class DatabaseHelper
{
 public static SqlParameter P(string name, SqlDbType type, object? value, int size = 0)
  => new(name, type) { Value = value ?? DBNull.Value, Size = size };
 public static string Like(string value) => "%" + value.Trim().Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~[") + "%";
 private static SqlConnection Connection() => new(ConfigurationManager.ConnectionStrings["SimplusClinic"]?.ConnectionString
  ?? throw new ClinicException("Missing SimplusClinic connection string in App.config."));
 public static async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader,T> map, IEnumerable<SqlParameter> parameters, CancellationToken ct = default)
 {
  try
  {
   await using var connection = Connection();
   await connection.OpenAsync(ct);
   await using var command = new SqlCommand(sql, connection) { CommandTimeout = 30 };
   command.Parameters.AddRange(parameters.ToArray());
   await using var reader = await command.ExecuteReaderAsync(ct);
   var items = new List<T>();
   while(await reader.ReadAsync(ct)) items.Add(map(reader));
   return items;
  }
  catch(SqlException ex) { throw Translate(ex); }
 }
 public static async Task<int> SaveAsync(string procedure, IEnumerable<SqlParameter> parameters, CancellationToken ct = default)
 {
  try
  {
   await using var connection = Connection();
   await connection.OpenAsync(ct);
   await using var command = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure, CommandTimeout = 30 };
   command.Parameters.AddRange(parameters.ToArray());
   return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
  }
  catch(SqlException ex) { throw Translate(ex); }
 }
 private static ClinicException Translate(SqlException ex) => new(ex.Number switch
 {
  >=51001 and <=51006 => ex.Message,
  51100 => ex.Message,
  2812 => "A required stored procedure is missing. For login, run Database/03_LoginUpgrade.sql in SSMS on the SimplusClinic database, then retry.",
  2601 or 2627 => "That license number is already registered, including archived doctors.",
  547 => "The item has invalid linked data or violates a database rule.",
  -2 or 1205 => "The request timed out or was interrupted by another transaction. Please refresh and retry.",
  _ => "The database request failed. Check the server, configuration and Windows account permissions. SQL code: " + ex.Number
 });
}
