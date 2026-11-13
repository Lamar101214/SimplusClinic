using System.ComponentModel.DataAnnotations;
namespace SimplusClinic.Domain;
public sealed class Patient : Entity
{
 [Required]
 [StringLength(80)]
 public string FirstName { get; set; } = "";
 [StringLength(80)]
 public string MiddleName { get; set; } = "";
 [Required]
 [StringLength(80)]
 public string LastName { get; set; } = "";
 [StringLength(30)]
 public string HouseNo { get; set; } = "";
 [StringLength(120)]
 public string Street { get; set; } = "";
 [StringLength(100)]
 public string Municipality { get; set; } = "";
 [Required]
 [StringLength(100)]
 public string CityProvince { get; set; } = "";
 [StringLength(15)]
 public string ZipCode { get; set; } = "";
 public DateTime DateOfBirth { get; set; } = DateTime.Today;
 [Required]
 [StringLength(30)]
 public string Gender { get; set; } = "Prefer not to say";
 [StringLength(30)]
 public string MobileNumber { get; set; } = "";
 [StringLength(30)]
 public string LandlineNumber { get; set; } = "";
 [StringLength(254)]
 public string EmailAddress { get; set; } = "";
 [StringLength(180)]
 public string GuardianName { get; set; } = "";
 [StringLength(400)]
 public string GuardianAddress { get; set; } = "";
 [StringLength(30)]
 public string GuardianMobileNumber { get; set; } = "";
 [StringLength(30)]
 public string GuardianLandlineNumber { get; set; } = "";
 public bool IsArchived { get; set; }
 public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
}
