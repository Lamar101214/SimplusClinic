using System.Configuration;
using SimplusClinic.Application;
using SimplusClinic.Domain;
using SimplusClinic.Infrastructure;
internal static class AuthIntegration
{
 public static async Task RunAsync(Action<bool,string> check)
 {
  string connection=Environment.GetEnvironmentVariable("SIMPLUS_TEST_CONNECTION")??
       throw new Exception("Set SIMPLUS_TEST_CONNECTION to a disposable database initialized with scripts 01, 02 and 03.");

  var config=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
  config.ConnectionStrings.ConnectionStrings.Remove("SimplusClinic");config.ConnectionStrings.ConnectionStrings.Add(new("SimplusClinic",connection,"Microsoft.Data.SqlClient"));config.Save(ConfigurationSaveMode.Modified);ConfigurationManager.RefreshSection("connectionStrings");
  
  var repo=new AuthenticationRepository();var auth=new AuthenticationService(repo);
  if(!await auth.NeedsSetupAsync())
      throw new Exception("Auth integration tests require a disposable database with no AppUsers. Never delete production users to run these tests.");
  
  async Task<bool> Bootstrap(string name)
        {try
            {await auth.BootstrapAsync(name,"Test Admin","Synthetic admin passphrase");
                return true;
            }

           catch(ClinicException ex) 
                when(ex.Message.Contains("already complete")){return false;}}

  var setup=await Task.WhenAll(Bootstrap("test.admin1"),Bootstrap("test.admin2"));check(setup.Count(x=>x)==1,"SQL auth: exactly one first administrator is created");

  var admin=await auth.SignInAsync(setup[0]?"test.admin1":"test.admin2","Synthetic admin passphrase");

  async Task Reject(Func<Task> action,string label)
        {try{await action();}
            catch(ClinicException){check(true,label);return;
                    }
                throw new Exception(label);
                            }

  await auth.CreateUserAsync(admin,"test.staff","Test Staff","Staff","Synthetic staff passphrase");
  await Reject(()=>auth.CreateUserAsync(admin,"TEST.STAFF","Duplicate","Staff","Synthetic staff passphrase"),"SQL auth: duplicate username blocked");

  var staff=await auth.SignInAsync("test.staff","Synthetic staff passphrase");

  await Reject(async()=>{await repo.UsersAsync(staff.Token);},"SQL auth: server rejects staff token for admin operation");
  await Reject(async()=>{await repo.UsersAsync(new byte[32]);},"SQL auth: server rejects fabricated token");

  var users=await auth.UsersAsync(admin);var staffUser=users.Single(u=>u.Username=="test.staff");
  await auth.SetActiveAsync(admin,staffUser.Id,false);
  await Reject(async()=>{await auth.SignInAsync("test.staff","Synthetic staff passphrase");},"SQL auth: disabled account cannot log in");
  await auth.SetActiveAsync(admin,staffUser.Id,true);

  for(int i=0;i<5;i++)await Reject(async()=>{await auth.SignInAsync("test.staff","incorrect password");},"SQL auth: failed attempt "+(i+1));
  await Reject(async()=>{await auth.SignInAsync("test.staff","Synthetic staff passphrase");},"SQL auth: lockout blocks even correct password");
  await auth.SignOutAsync(admin);
  await Reject(async()=>{await repo.UsersAsync(admin.Token);},"SQL auth: signed-out session rejected");

  Console.WriteLine("Synthetic auth accounts remain in the disposable database. Drop that test database after inspecting results.");
 }
}
