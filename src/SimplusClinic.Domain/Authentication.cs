namespace SimplusClinic.Domain;
public sealed record ClinicUser(int Id, string Username, string DisplayName, string Role, bool IsActive);
public sealed record LoginChallenge(byte[] Salt, int Iterations);
public sealed record LoginSession(ClinicUser User, byte[] Token, DateTime ExpiresAtUtc);
