using System.Data;
using Microsoft.Data.SqlClient;
using SimplusClinic.Application;
using SimplusClinic.Domain;
namespace SimplusClinic.Infrastructure;
public sealed class AuthenticationRepository : IAuthenticationRepository
{
 private static SqlParameter S(string name, string value,int length)=>DatabaseHelper.P(name,SqlDbType.NVarChar,value,length);
 private static SqlParameter B(string name,byte[] value,int length)=>DatabaseHelper.P(name,SqlDbType.VarBinary,value,length);
 private static ClinicUser User(SqlDataReader r)=>new((int)r["Id"],(string)r["Username"],(string)r["DisplayName"],(string)r["Role"],(bool)r["IsActive"]);
 public async Task<bool> NeedsSetupAsync(CancellationToken ct=default)
 => (await DatabaseHelper.QueryAsync("EXEC dbo.AuthNeedsSetup",r=>(bool)r[0],[],ct))[0];
 public async Task<LoginChallenge> ChallengeAsync(string username,CancellationToken ct=default)
 => (await DatabaseHelper.QueryAsync("EXEC dbo.AuthChallenge @Username",r=>new LoginChallenge((byte[])r[0],(int)r[1]),[S("@Username",username,50)],ct))[0];
 public async Task<LoginSession?> SignInAsync(string username,byte[] proof,byte[] token,CancellationToken ct=default)
 => (await DatabaseHelper.QueryAsync("EXEC dbo.AuthSignIn @Username,@Proof,@Token",r=>new LoginSession(User(r),token,DateTime.SpecifyKind((DateTime)r["ExpiresAtUtc"],DateTimeKind.Utc)),[S("@Username",username,50),B("@Proof",proof,32),B("@Token",token,32)],ct)).SingleOrDefault();
 public async Task BootstrapAsync(string username,string displayName,byte[] salt,byte[] hash,int iterations,CancellationToken ct=default)
 => _=await DatabaseHelper.SaveAsync("dbo.AuthBootstrap",[S("@Username",username,50),S("@DisplayName",displayName,100),B("@Salt",salt,16),B("@Hash",hash,32),DatabaseHelper.P("@Iterations",SqlDbType.Int,iterations)],ct);
 public async Task<IReadOnlyList<ClinicUser>> UsersAsync(byte[] token,CancellationToken ct=default)
 => await DatabaseHelper.QueryAsync("EXEC dbo.AuthListUsers @Token",User,[B("@Token",token,32)],ct);
 public async Task CreateUserAsync(byte[] token,string username,string displayName,string role,byte[] salt,byte[] hash,int iterations,CancellationToken ct=default)
 => _=await DatabaseHelper.SaveAsync("dbo.AuthCreateUser",[B("@Token",token,32),S("@Username",username,50),S("@DisplayName",displayName,100),S("@Role",role,10),B("@Salt",salt,16),B("@Hash",hash,32),DatabaseHelper.P("@Iterations",SqlDbType.Int,iterations)],ct);
 public async Task SetActiveAsync(byte[] token,int id,bool active,CancellationToken ct=default)
 => _=await DatabaseHelper.SaveAsync("dbo.AuthSetActive",[B("@Token",token,32),DatabaseHelper.P("@Id",SqlDbType.Int,id),DatabaseHelper.P("@Active",SqlDbType.Bit,active)],ct);
 public async Task SignOutAsync(byte[] token,CancellationToken ct=default)
 => _=await DatabaseHelper.SaveAsync("dbo.AuthSignOut",[B("@Token",token,32)],ct);
}
