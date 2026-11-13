using SimplusClinic.Domain;
namespace SimplusClinic.WinForms;
internal enum FieldKind { Text, Multiline, Date, DateTime, Gender, Status, Patient, Doctor }
internal sealed record FieldSpec(string Property,string Label,FieldKind Kind=FieldKind.Text);
internal static class EditorFields
{
 public static List<FieldSpec> For(Type type)
 {
  if(type==typeof(Appointment))return [new("PatientId","Patient *",FieldKind.Patient),new("DoctorId","Doctor *",FieldKind.Doctor),new("StartAt","Start date and time *",FieldKind.DateTime),new("EndAt","End date and time *",FieldKind.DateTime),new("Status","Status *",FieldKind.Status)];
  if(type==typeof(MedicalRecord))return [new("PatientId","Patient *",FieldKind.Patient),new("DoctorId","Doctor *",FieldKind.Doctor),new("TreatmentDate","Date of treatment *",FieldKind.Date),new("Diagnosis","Diagnosis *",FieldKind.Multiline),new("Treatment","Treatment *",FieldKind.Multiline),new("Prescriptions","Prescriptions",FieldKind.Multiline)];
  List<FieldSpec> common=[new("FirstName","First name *"),new("MiddleName","Middle name"),new("LastName","Last name *"),new("DateOfBirth","Date of birth *",FieldKind.Date),new("Gender","Gender *",FieldKind.Gender),new("HouseNo","House number"),new("Street","Street"),new("Municipality","Municipality"),new("CityProvince","City / Province *"),new("ZipCode","ZIP code"),new("MobileNumber","Mobile number"),new("LandlineNumber","Landline number"),new("EmailAddress","Email address")];
  if(type==typeof(Patient)) common.AddRange([new("GuardianName","Guardian name"),new("GuardianAddress","Guardian complete address",FieldKind.Multiline),new("GuardianMobileNumber","Guardian mobile number"),new("GuardianLandlineNumber","Guardian landline number")]);
  else common.AddRange([new("Specialization","Specialization *"),new("LicenseNumber","License number *")]);
  return common;
 }
}
