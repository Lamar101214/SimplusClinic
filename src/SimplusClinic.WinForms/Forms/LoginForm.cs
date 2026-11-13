using SimplusClinic.Application;
using SimplusClinic.Domain;
namespace SimplusClinic.WinForms;
internal sealed class LoginForm : Form
{
 private readonly AuthenticationService auth;
 private readonly TextBox username=Theme.Input(),password=Theme.Input(),displayName=Theme.Input(),confirm=Theme.Input();
 private readonly Label heading=Theme.Label("Welcome back",24),description=Theme.Label("Sign in to Simplus Clinic",11,Theme.Muted);
 private readonly Label state=new(){AutoSize=true,ForeColor=Theme.Muted,MaximumSize=new Size(430,0),Margin=new Padding(0,12,0,12)};
 private readonly Button submit=Theme.Button("Sign in"),retry=Theme.Button("Retry connection");
 private readonly CheckBox reveal=new(){Text="Show password",AutoSize=true,ForeColor=Theme.Muted,Margin=new Padding(0,8,0,12)};
 private readonly List<Control> setupControls=[];
 private bool setup,busy;
 private readonly CancellationTokenSource lifetime=new();
 public LoginSession? Session { get; private set; }
 public LoginForm(AuthenticationService auth)
 {
  this.auth=auth;Theme.Apply(this);Text="Simplus Clinic • Sign in";StartPosition=FormStartPosition.CenterScreen;
  FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;MinimizeBox=true;ClientSize=new Size(540,700);AutoScaleMode=AutoScaleMode.Dpi;
  var outer=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(40,30,40,25)};
  var layout=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=Padding.Empty,Padding=Padding.Empty};
  layout.ColumnStyles.Add(new(SizeType.Percent,100));
  void Add(Control control){int row=layout.RowCount++;layout.RowStyles.Add(new(SizeType.AutoSize));layout.Controls.Add(control,0,row);}
  Add(Theme.Label("+ SIMPLUS",22,Theme.Accent));Add(Theme.Label("CLINIC MANAGEMENT",9,Theme.Muted));
  heading.Margin=new Padding(0,20,0,8);Add(heading);description.Margin=new Padding(0,0,0,24);Add(description);
  void Field(string label,TextBox input,bool onlySetup=false)
  {
   var caption=Theme.Label(label,10,Theme.Muted);caption.Margin=new Padding(0,0,0,6);
   input.Dock=DockStyle.Fill;input.Margin=new Padding(0,0,0,14);input.AccessibleName=label;
   Add(caption);Add(input);if(onlySetup){setupControls.Add(caption);setupControls.Add(input);}
  }
  username.MaxLength=50;displayName.MaxLength=100;password.MaxLength=128;confirm.MaxLength=128;
  password.UseSystemPasswordChar=true;confirm.UseSystemPasswordChar=true;
  Field("Display name",displayName,true);Field("Username",username);Field("Password",password);Field("Confirm password",confirm,true);
  reveal.CheckedChanged+=(_,_)=>{password.UseSystemPasswordChar=!reveal.Checked;confirm.UseSystemPasswordChar=!reveal.Checked;};Add(reveal);
  submit.Width=430;submit.Dock=DockStyle.Top;submit.Enabled=false;Add(submit);Add(state);
  retry.AutoSize=true;retry.BackColor=Theme.Elevated;Add(retry);
  outer.Controls.Add(layout);Controls.Add(outer);AcceptButton=submit;
  foreach(var control in setupControls)control.Visible=false;
  retry.Click+=async(_,_)=>await InitializeAsync();submit.Click+=async(_,_)=>await SubmitAsync();
  Shown+=async(_,_)=>await InitializeAsync();FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};
 }
 private async Task InitializeAsync()
 {
  if(busy)return;busy=true;submit.Enabled=false;retry.Enabled=false;state.Text="Connecting to clinic database…";state.ForeColor=Theme.Muted;
  try
  {
   setup=await auth.NeedsSetupAsync(lifetime.Token);if(IsDisposed)return;
   foreach(var control in setupControls)control.Visible=setup;
   heading.Text=setup?"Set up your clinic":"Welcome back";
   description.Text=setup?"Create your first administrator account.":"Sign in to Simplus Clinic";
   submit.Text=setup?"Create administrator":"Sign in";
   state.Text=setup?"Use a 12–128 character password or passphrase. No default account is created.":"Enter the account provided by your administrator.";
   submit.Enabled=true;if(setup)displayName.Focus();else username.Focus();
  }
  catch(Exception ex){state.Text=ex is ClinicException?ex.Message:"Unable to connect. Check App.config and retry.";state.ForeColor=Theme.Danger;}
  finally{busy=false;if(!IsDisposed)retry.Enabled=true;}
 }
 private async Task SubmitAsync()
 {
  if(busy)return;busy=true;submit.Enabled=false;retry.Enabled=false;state.ForeColor=Theme.Muted;state.Text=setup?"Creating administrator…":"Signing in…";
  foreach(var input in new[]{username,password,displayName,confirm})input.Enabled=false;
  try
  {
   if(setup)
   {
    if(password.Text!=confirm.Text)throw new ClinicException("Passwords do not match.");
    await auth.BootstrapAsync(username.Text,displayName.Text,password.Text,lifetime.Token);
    setup=false;foreach(var control in setupControls)control.Visible=false;
    heading.Text="Welcome back";description.Text="Your administrator account is ready.";submit.Text="Sign in";
    password.Clear();confirm.Clear();state.ForeColor=Theme.Success;state.Text="Administrator created. Enter your password to sign in.";
   }
   else
   {
    Session=await auth.SignInAsync(username.Text,password.Text,lifetime.Token);
    password.Clear();confirm.Clear();busy=false;DialogResult=DialogResult.OK;Close();
   }
  }
  catch(Exception ex){state.Text=ex is ClinicException?ex.Message:"Sign-in could not be completed. Please retry.";state.ForeColor=Theme.Danger;password.Clear();confirm.Clear();}
  finally
  {
   busy=false;if(!IsDisposed){submit.Enabled=true;retry.Enabled=true;foreach(var input in new[]{username,password,displayName,confirm})input.Enabled=true;password.Focus();}
  }
 }
 protected override void Dispose(bool disposing){if(disposing){lifetime.Cancel();lifetime.Dispose();}base.Dispose(disposing);}
}
