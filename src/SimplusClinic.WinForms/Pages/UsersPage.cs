using SimplusClinic.Application;
using SimplusClinic.Domain;
namespace SimplusClinic.WinForms;
internal sealed class UsersPage : PageBase
{
 private readonly AuthenticationService auth;
 private readonly LoginSession session;
 private readonly DataGridView grid=Theme.Grid();
 private readonly TextBox username=Theme.Input(),displayName=Theme.Input(),password=Theme.Input(),confirmation=Theme.Input();
 private readonly ComboBox role=Theme.Combo();
 private readonly Label status=Theme.Label("",10,Theme.Muted);
 private readonly Button create=Theme.Button("Create account"),toggle=Theme.Button("Enable / disable"),refresh=Theme.Button("Refresh");
 private bool busy;
 public UsersPage(AuthenticationService auth,LoginSession session)
 {
  this.auth=auth;this.session=session;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=6,Margin=Padding.Empty};layout.ColumnStyles.Add(new(SizeType.Percent,100));
  for(int i=0;i<4;i++)layout.RowStyles.Add(new(SizeType.AutoSize));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.AutoSize));
  var heading=Theme.Label("User accounts",26);heading.Margin=new Padding(0,0,0,8);layout.Controls.Add(heading,0,0);
  var subtitle=Theme.Label("Admins manage accounts. Staff can use the clinic modules.",10,Theme.Muted);subtitle.Margin=new Padding(0,0,0,20);layout.Controls.Add(subtitle,0,1);
  var fields=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Margin=new Padding(0,0,0,10)};
  void Field(string text,Control input){var panel=new Panel{Size=new Size(220,76),Margin=new Padding(0,0,14,8)};var label=Theme.Label(text,10,Theme.Muted);label.Dock=DockStyle.Top;input.Dock=DockStyle.Bottom;input.AccessibleName=text;panel.Controls.Add(input);panel.Controls.Add(label);fields.Controls.Add(panel);}
  username.MaxLength=50;displayName.MaxLength=100;password.MaxLength=128;confirmation.MaxLength=128;password.UseSystemPasswordChar=true;confirmation.UseSystemPasswordChar=true;
  role.Items.AddRange(["Staff","Admin"]);role.SelectedIndex=0;
  Field("Username",username);Field("Display name",displayName);Field("Role",role);Field("Password (12–128 characters)",password);Field("Confirm password",confirmation);
  var show=new CheckBox{Text="Show passwords",AutoSize=true,ForeColor=Theme.Muted};show.CheckedChanged+=(_,_)=>{password.UseSystemPasswordChar=!show.Checked;confirmation.UseSystemPasswordChar=!show.Checked;};fields.Controls.Add(show);layout.Controls.Add(fields,0,2);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,Margin=new Padding(0,0,0,16)};toggle.Width=160;toggle.BackColor=Theme.Elevated;actions.Controls.AddRange([create,toggle,refresh]);layout.Controls.Add(actions,0,3);
  grid.Margin=Padding.Empty;Theme.Columns(grid,("Id","ID",12),("Username","Username",25),("DisplayName","Display name",35),("Role","Role",18),("IsActive","Active",15));layout.Controls.Add(grid,0,4);
  status.Margin=new Padding(0,12,0,0);layout.Controls.Add(status,0,5);Controls.Add(layout);
  create.Click+=async(_,_)=>await PerformAsync(async()=>{if(password.Text!=confirmation.Text)throw new ClinicException("Passwords do not match.");await auth.CreateUserAsync(session,username.Text,displayName.Text,role.SelectedItem!.ToString()!,password.Text,Lifetime.Token);username.Clear();displayName.Clear();password.Clear();confirmation.Clear();await ReloadAsync();});
  toggle.Click+=async(_,_)=>await PerformAsync(async()=>{var user=grid.CurrentRow?.DataBoundItem as ClinicUser??throw new ClinicException("Select an account first.");if(!Ui.Confirm(this,user.IsActive?"Disable account":"Enable account",$"Change access for {user.Username}?"))return;await auth.SetActiveAsync(session,user.Id,!user.IsActive,Lifetime.Token);await ReloadAsync();});
  refresh.Click+=async(_,_)=>await PerformAsync(ReloadAsync);Load+=async(_,_)=>await PerformAsync(ReloadAsync);
 }
 private async Task PerformAsync(Func<Task> action)
 {
  if(busy)return;busy=true;create.Enabled=toggle.Enabled=refresh.Enabled=false;
  try{await Ui.RunAsync(this,action);}finally{busy=false;if(!IsDisposed)create.Enabled=toggle.Enabled=refresh.Enabled=true;}
 }
 private async Task ReloadAsync(){var users=await auth.UsersAsync(session,Lifetime.Token);if(IsDisposed)return;grid.DataSource=users.ToList();status.Text=$"{users.Count} accounts · Disabled users cannot sign in.";}
}
