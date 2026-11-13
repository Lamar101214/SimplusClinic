using System.ComponentModel.DataAnnotations;
namespace SimplusClinic.Domain;
public sealed class Appointment : Entity
{
 [Range(1, int.MaxValue)]
 public int PatientId { get; set; }
 [Range(1, int.MaxValue)]
 public int DoctorId { get; set; }
 public DateTime StartAt { get; set; } = DateTime.Today;
 public DateTime EndAt { get; set; } = DateTime.Today;
 [Required]
 [StringLength(20)]
 public string Status { get; set; } = "Scheduled";
 public string PatientName { get; set; } = "";
 public string DoctorName { get; set; } = "";
}
