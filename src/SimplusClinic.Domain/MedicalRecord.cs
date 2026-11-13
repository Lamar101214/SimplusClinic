using System.ComponentModel.DataAnnotations;
namespace SimplusClinic.Domain;
public sealed class MedicalRecord : Entity
{
 [Range(1, int.MaxValue)]
 public int PatientId { get; set; }
 [Range(1, int.MaxValue)]
 public int DoctorId { get; set; }
 [Required]
 [StringLength(4000)]
 public string Diagnosis { get; set; } = "";
 [Required]
 [StringLength(4000)]
 public string Treatment { get; set; } = "";
 [StringLength(4000)]
 public string Prescriptions { get; set; } = "";
 public DateTime TreatmentDate { get; set; } = DateTime.Today;
 public bool IsArchived { get; set; }
 public string PatientName { get; set; } = "";
 public string DoctorName { get; set; } = "";
}
