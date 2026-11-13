using SimplusClinic.Domain;
namespace SimplusClinic.Application;
public interface IAuthenticationRepository
{
 Task<bool> NeedsSetupAsync(CancellationToken ct = default);
 Task<LoginChallenge> ChallengeAsync(string username, CancellationToken ct = default);
 Task<LoginSession?> SignInAsync(string username, byte[] proof, byte[] token, CancellationToken ct = default);
 Task BootstrapAsync(string username, string displayName, byte[] salt, byte[] hash, int iterations, CancellationToken ct = default);
 Task<IReadOnlyList<ClinicUser>> UsersAsync(byte[] token, CancellationToken ct = default);
 Task CreateUserAsync(byte[] token, string username, string displayName, string role, byte[] salt, byte[] hash, int iterations, CancellationToken ct = default);
 Task SetActiveAsync(byte[] token, int id, bool active, CancellationToken ct = default);
 Task SignOutAsync(byte[] token, CancellationToken ct = default);
}
