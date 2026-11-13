using System.Security.Cryptography;
using System.Text.RegularExpressions;
using SimplusClinic.Domain;
namespace SimplusClinic.Application;
public sealed class AuthenticationService(IAuthenticationRepository repository)
{
 public Task<bool> NeedsSetupAsync(CancellationToken ct = default) => repository.NeedsSetupAsync(ct);
 private static string Username(string value)
 {
  value = value.Trim().ToLowerInvariant();
  if (!Regex.IsMatch(value, "\\A[a-z0-9._-]{3,50}\\z"))
   throw new ClinicException("Username must contain 3–50 letters, numbers, dots, underscores or hyphens.");
  return value;
 }
 private static string Name(string value)
 {
  value = value.Trim();
  if (value.Length is < 1 or > 100) throw new ClinicException("Display name must contain 1–100 characters.");
  return value;
 }
 public async Task<LoginSession> SignInAsync(string username, string password, CancellationToken ct = default)
 {
  username = Username(username);
  if (password.Length is < 1 or > 128) 
            throw new ClinicException("Enter your username and password.");

  var challenge = await repository.ChallengeAsync(username, ct);
  byte[] proof = await Task.Run(() => PasswordHasher.Derive(password, challenge.Salt, challenge.Iterations), ct);
  byte[] token = RandomNumberGenerator.GetBytes(32);

  try
  {
   var session = await repository.SignInAsync(username, proof, token, ct);
   if (session is null) 
                throw new ClinicException("Sign-in failed. Check your credentials. If your account is locked, wait 15 minutes; disabled accounts require an administrator.");
   return session;
  }

  catch { CryptographicOperations.ZeroMemory(token); throw; }
  finally { CryptographicOperations.ZeroMemory(proof); }
 }
 public async Task BootstrapAsync(string username, string displayName, string password, CancellationToken ct = default)
 {
  username = Username(username); displayName = Name(displayName); PasswordHasher.ValidateNewPassword(password);
        
  byte[] salt = PasswordHasher.NewSalt();
  byte[] hash = await Task.Run(() => PasswordHasher.Derive(password, salt, PasswordHasher.Iterations), ct);

  try 
        { await repository.BootstrapAsync(username, displayName, salt, hash, PasswordHasher.Iterations, ct);}

  finally 
        { CryptographicOperations.ZeroMemory(hash);}
 }
 public Task<IReadOnlyList<ClinicUser>> UsersAsync(LoginSession session, CancellationToken ct = default)

 {
    RequireAdmin(session); 
        
    return repository.UsersAsync(session.Token, ct);
 }
 public async Task CreateUserAsync(LoginSession session, string username, string displayName, string role, string password, CancellationToken ct = default)
 {
  
    RequireAdmin(session); username=Username(username); displayName=Name(displayName);
  if (role is not ("Admin" or "Staff")) 
      throw new ClinicException("Select Admin or Staff.");
            PasswordHasher.ValidateNewPassword(password);
            byte[] salt=PasswordHasher.NewSalt();
            byte[] hash=await Task.Run(() => PasswordHasher.Derive(password,salt,PasswordHasher.Iterations),ct);

  try 
        { 
            await repository.CreateUserAsync(session.Token,username,displayName,role,salt,hash,PasswordHasher.Iterations,ct); }

  finally { CryptographicOperations.ZeroMemory(hash); }
 }
 public Task SetActiveAsync(LoginSession session,int id,bool active,CancellationToken ct=default)
 {
  RequireAdmin(session);
  if(id==session.User.Id)
      throw new ClinicException("You cannot disable your own account.");

  return repository.SetActiveAsync(session.Token,id,active,ct);
 }
 public Task SignOutAsync(LoginSession session,CancellationToken ct=default)=>repository.SignOutAsync(session.Token,ct);
 private static void RequireAdmin(LoginSession session)
 {
  if(session.ExpiresAtUtc<=DateTime.UtcNow || session.User.Role!="Admin")
     throw new ClinicException("An active administrator session is required.");
 }
}
