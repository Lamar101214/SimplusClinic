namespace SimplusClinic.Domain;
public static class AppointmentRules
{
 public static bool Overlaps(DateTime start, DateTime end, DateTime otherStart, DateTime otherEnd)
   => start < otherEnd && end > otherStart;
 public static bool Conflicts(Appointment requested, Appointment existing)
   => requested.Id != existing.Id && requested.Status != "Cancelled" && existing.Status != "Cancelled"
      && (requested.DoctorId == existing.DoctorId || requested.PatientId == existing.PatientId)
      && Overlaps(requested.StartAt, requested.EndAt, existing.StartAt, existing.EndAt);
}
