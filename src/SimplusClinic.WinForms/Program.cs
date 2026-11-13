using SimplusClinic.Application;
using SimplusClinic.Infrastructure;
namespace SimplusClinic.WinForms;
internal static class Program
{
 [STAThread]
 private static void Main()
 {
  ApplicationConfiguration.Initialize();
  System.Windows.Forms.Application.ThreadException += (_, e) => Ui.ShowError(null, e.Exception);
  var auth = new AuthenticationService(new AuthenticationRepository());
  while (true)
  {
   using var login = new LoginForm(auth);
   if (login.ShowDialog() != DialogResult.OK || login.Session is null) break;
   var session = login.Session;
   using var main = new MainForm(new AppServices(), auth, session);
   main.ShowDialog();
   System.Security.Cryptography.CryptographicOperations.ZeroMemory(session.Token);
   if (!main.ReturnToLogin) break;
  }
 }
}
internal sealed class AppServices
{
 public EntityService<SimplusClinic.Domain.Patient> Patients { get; } = new(new PatientRepository());
 public EntityService<SimplusClinic.Domain.Doctor> Doctors { get; } = new(new DoctorRepository());
 public EntityService<SimplusClinic.Domain.MedicalRecord> Records { get; } = new(new MedicalRecordRepository());
 public AppointmentService Appointments { get; } = new(new AppointmentRepository());
 public IOverviewRepository Overview { get; } = new OverviewRepository();
}
