namespace SimplusClinic.Domain;
public sealed record SearchCriteria(string Id = "", string Name = "", string Specialization = "", string LicenseNumber = "", string PatientId = "", string PatientName = "", string DoctorId = "", string DoctorName = "", bool IncludeArchived = false, int Page = 0, int PageSize = 100);
public sealed record LookupItem(int Id, string Name) { public override string ToString() => Name; }
public sealed record DashboardSummary(int Patients, int Doctors, int TodayAppointments, int MedicalRecords);
public enum ReportKind { Patients, Doctors, PatientsPerDoctor, DoctorsPerSpecialization }
public sealed record ReportRow(int Id, string Name, string Detail, string MobileNumber);
public sealed record ConflictResult(bool HasConflict, string Message);
