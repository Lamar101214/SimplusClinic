using System.Security.Cryptography;
using SimplusClinic.Application;
using SimplusClinic.Domain;
internal static class AuthTests
{
 public static async Task RunAsync(Action<bool,string> check)
 {
  var salt=PasswordHasher.NewSalt();var otherSalt=PasswordHasher.NewSalt();
  var hash=PasswordHasher.Derive("a long sample passphrase",salt,PasswordHasher.Iterations);
  check(hash.Length==32 && salt.Length==16,"Auth: expected hash and salt sizes");
  check(CryptographicOperations.FixedTimeEquals(hash,PasswordHasher.Derive("a long sample passphrase",salt,PasswordHasher.Iterations)),"Auth: deterministic password derivation");
  check(!CryptographicOperations.FixedTimeEquals(hash,PasswordHasher.Derive("a different passphrase",salt,PasswordHasher.Iterations)),"Auth: wrong password differs");
  check(!CryptographicOperations.FixedTimeEquals(hash,PasswordHasher.Derive("a long sample passphrase",otherSalt,PasswordHasher.Iterations)),"Auth: independent salts differ");
  void Rejected(Action action,string label){try{action();}catch(ClinicException){check(true,label);return;}throw new Exception(label);}
  Rejected(()=>PasswordHasher.ValidateNewPassword("short"),"Auth: short password rejected");
  Rejected(()=>PasswordHasher.ValidateNewPassword(new string('x',129)),"Auth: excessive password length rejected");
  Rejected(()=>PasswordHasher.Derive("password",salt,1),"Auth: unsafe iteration count rejected");
  var fake=new FakeAuth(salt,hash);var auth=new AuthenticationService(fake);
  var session=await auth.SignInAsync(" ADMIN ","a long sample passphrase");
  check(session.User.Username=="admin" && session.Token.Length==32,"Auth: normalized username and random session token");
  async Task RejectAsync(Func<Task> action,string label){try{await action();}catch(ClinicException){check(true,label);return;}throw new Exception(label);}
  await RejectAsync(async()=>{await auth.SignInAsync("admin","wrong");},"Auth: failed login rejected");
  await RejectAsync(()=>auth.BootstrapAsync("bad username","Test","long enough passphrase"),"Auth: invalid username rejected");
  await RejectAsync(()=>auth.BootstrapAsync("valid"," ","long enough passphrase"),"Auth: empty display name rejected");
  var staff=session with {User=session.User with {Role="Staff"}};
  await RejectAsync(async()=>{await auth.UsersAsync(staff);},"Auth: staff denied account listing");
  await RejectAsync(()=>auth.CreateUserAsync(staff,"test","Test","Admin","long enough passphrase"),"Auth: staff denied account creation");
  await RejectAsync(()=>auth.SetActiveAsync(session,session.User.Id,false),"Auth: self disable blocked");
  await RejectAsync(async()=>{await auth.UsersAsync(session with {ExpiresAtUtc=DateTime.UtcNow.AddMinutes(-1)});},"Auth: expired admin session rejected");
  await RejectAsync(()=>auth.CreateUserAsync(session,"test","Test","Owner","long enough passphrase"),"Auth: invalid role rejected");
  await auth.CreateUserAsync(session,"New.Staff","New Staff","Staff","long enough passphrase");
  check(fake.Created==1,"Auth: admin account creation reaches repository");
  await auth.SignOutAsync(session);check(fake.SignedOut,"Auth: logout revokes session through repository");
 }
 private sealed class FakeAuth(byte[] salt,byte[] expectedHash) : IAuthenticationRepository
 {
  public int Created;public bool SignedOut;
  public Task<bool> NeedsSetupAsync(CancellationToken ct=default)=>Task.FromResult(false);
  public Task<LoginChallenge> ChallengeAsync(string username,CancellationToken ct=default)=>Task.FromResult(new LoginChallenge(salt,PasswordHasher.Iterations));
  public Task<LoginSession?> SignInAsync(string username,byte[] proof,byte[] token,CancellationToken ct=default)=>Task.FromResult<LoginSession?>(username=="admin" && CryptographicOperations.FixedTimeEquals(proof,expectedHash)?new LoginSession(new(1,"admin","Administrator","Admin",true),token,DateTime.UtcNow.AddHours(8)):null);
  public Task BootstrapAsync(string u,string n,byte[] s,byte[] h,int i,CancellationToken ct=default)=>Task.CompletedTask;
  public Task<IReadOnlyList<ClinicUser>> UsersAsync(byte[] token,CancellationToken ct=default)=>Task.FromResult<IReadOnlyList<ClinicUser>>([]);
  public Task CreateUserAsync(byte[] t,string u,string n,string role,byte[] s,byte[] h,int i,CancellationToken ct=default){Created++;return Task.CompletedTask;}
  public Task SetActiveAsync(byte[] t,int id,bool active,CancellationToken ct=default)=>Task.CompletedTask;
  public Task SignOutAsync(byte[] t,CancellationToken ct=default){SignedOut=true;return Task.CompletedTask;}
 }
}
