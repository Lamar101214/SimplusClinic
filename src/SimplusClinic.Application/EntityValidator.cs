using System.ComponentModel.DataAnnotations;
using SimplusClinic.Domain;
namespace SimplusClinic.Application;
public static class EntityValidator
{
 public static void Validate(Entity entity)
 {
  
 foreach 
       (var property in entity.GetType().GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite))
        property.SetValue(entity, ((string?)property.GetValue(entity) ?? "").Trim());

        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(entity, new ValidationContext(entity), errors, true);

if (errors.Count > 0) 
    throw new ClinicException(string.Join(Environment.NewLine, errors.Select(e => e.ErrorMessage)));
    string email = entity 
        switch { Patient p => p.EmailAddress, Doctor d => d.EmailAddress, _ => "" };

if(email.Length>0 && !new EmailAddressAttribute().IsValid(email)) 
    throw new ClinicException("Enter a valid email address or leave it blank.");
        DateTime? dob = entity switch { Patient p => p.DateOfBirth, Doctor d => d.DateOfBirth, _ => null };

if (dob.HasValue && (dob.Value.Date > DateTime.Today || dob.Value.Year < 1900)) 
    throw new ClinicException("Date of birth must be between 1900 and today.");

if (entity is MedicalRecord m && (m.TreatmentDate.Date > DateTime.Today || m.TreatmentDate.Year < 1900)) 
    throw new ClinicException("Treatment date must be between 1900 and today.");

if (entity is Appointment a)
  {
   if (a.StartAt >= a.EndAt) 
     throw new ClinicException("End time must be after start time.");

   if ((a.EndAt - a.StartAt).TotalHours > 24) 
     throw new ClinicException("Appointments cannot exceed 24 hours.");

   if (a.Status is not ("Scheduled" or "Completed" or "Cancelled")) 
     throw new ClinicException("Select a valid appointment status.");

          }
     }
}
