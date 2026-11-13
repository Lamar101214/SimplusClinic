using System.Security.Cryptography;
namespace SimplusClinic.Application;
public static class PasswordHasher
{
 public const int Iterations = 600_000;
 public static byte[] NewSalt() => RandomNumberGenerator.GetBytes(16);
 public static byte[] Derive(string password, byte[] salt, int iterations)
 {
  if (salt.Length != 16 || iterations < 600_000 || iterations > 2_000_000)
   throw new ClinicException("Invalid password hashing configuration.");

    return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
 }
 public static void ValidateNewPassword(string password)
 {
  if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 128)
     throw new ClinicException("Use a password or passphrase containing 12–128 characters.");

     }
}
