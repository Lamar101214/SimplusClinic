using SimplusClinic.Application;
using SimplusClinic.Domain;
using System.Runtime.InteropServices;
namespace SimplusClinic.WinForms;
internal sealed class MainForm : Form
{
 private readonly Panel content=new(){Dock=DockStyle.Fill};
 private readonly Label time=Theme.Label("",10,Theme.Muted);
 private readonly System.Windows.Forms.Timer clock=new(){Interval=1000};
 private readonly List<Button> navigation=[];
 public bool ReturnToLogin { get; private set; }
 private bool closingSession,allowClose;
 private readonly AuthenticationService auth;
 private readonly LoginSession session;
 public MainForm(AppServices services, AuthenticationService auth, LoginSession session)
 {
  this.auth=auth;this.session=session;
  Theme.Apply(this);Text="Simplus Clinic";FormBorderStyle=FormBorderStyle.None;MinimumSize=new Size(1100,740);Size=new Size(1440,900);StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;
  var sidebar=new Panel{Dock=DockStyle.Left,Width=220,BackColor=Theme.Surface,Padding=new Padding(18,30,18,20)};
  var nav=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};
  nav.Controls.Add(Theme.Label("+ SIMPLUS",20,Theme.Accent));nav.Controls.Add(Theme.Label("CLINIC MANAGEMENT",9,Theme.Muted));
  nav.Controls.Add(new Panel{Height=32,Width=180});
  void Add(string title,Func<UserControl> factory)
  {
   var button=Theme.Button(title);button.Size=new Size(184,46);button.Margin=new Padding(0,0,0,12);button.TextAlign=ContentAlignment.MiddleLeft;button.Padding=new Padding(14,0,0,0);button.BackColor=Theme.Surface;
   button.Click+=(_,_)=>{foreach(var b in navigation)b.BackColor=Theme.Surface;button.BackColor=Theme.Accent;Switch(factory());};
   navigation.Add(button);nav.Controls.Add(button);
  }
  Add("Overview",()=>new DashboardPage(services));Add("Patients",()=>new EntityPage<SimplusClinic.Domain.Patient>(services,services.Patients));
  Add("Doctors",()=>new EntityPage<SimplusClinic.Domain.Doctor>(services,services.Doctors));Add("Medical records",()=>new EntityPage<SimplusClinic.Domain.MedicalRecord>(services,services.Records));
  Add("Appointments",()=>new EntityPage<SimplusClinic.Domain.Appointment>(services,services.Appointments));Add("Reports",()=>new ReportsPage(services));
  if(session.User.Role=="Admin") Add("User accounts",()=>new UsersPage(auth,session));
  var logout=Theme.Button("Log out",(_,_)=>{if(Ui.Confirm(this,"Log out","Return to the sign-in screen?")){ReturnToLogin=true;Close();}});
  logout.Size=new Size(184,42);logout.BackColor=Theme.Elevated;nav.Controls.Add(logout);nav.AutoScroll=true;
  sidebar.Controls.Add(nav);
  var header=new Panel{Dock=DockStyle.Top,Height=68,BackColor=Theme.Surface,Padding=new Padding(28,18,15,10)};
  var account=Theme.Label(session.User.DisplayName+"  /  "+session.User.Role,11);account.Dock=DockStyle.Left;header.Controls.Add(account);
  var right=new FlowLayoutPanel{Dock=DockStyle.Right,Width=490,FlowDirection=FlowDirection.RightToLeft};
  var close=Theme.Button("×",(_,_)=>Close());close.Width=42;close.BackColor=Theme.Elevated;
  var maximize=Theme.Button("□",(_,_)=>WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized);maximize.Width=42;maximize.BackColor=Theme.Elevated;
  var minimize=Theme.Button("−",(_,_)=>WindowState=FormWindowState.Minimized);minimize.Width=42;minimize.BackColor=Theme.Elevated;
  time.Margin=new Padding(0,10,15,0);right.Controls.AddRange([close,maximize,minimize,time]);header.Controls.Add(right);
  header.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};
  var main=new Panel{Dock=DockStyle.Fill};main.Controls.Add(content);main.Controls.Add(header);Controls.Add(main);Controls.Add(sidebar);
  clock.Tick+=(_,_)=>time.Text=DateTime.Now.ToString("ddd, dd MMM  •  HH:mm");clock.Start();time.Text=DateTime.Now.ToString("ddd, dd MMM  •  HH:mm");
  Shown+=(_,_)=>navigation[0].PerformClick();
  clock.Tick+=(_,_)=>{if(!closingSession && session.ExpiresAtUtc<=DateTime.UtcNow && OwnedForms.Length==0){ReturnToLogin=true;Close();}};
  FormClosing+=EndSessionAsync;
 }
 private async void EndSessionAsync(object? sender,FormClosingEventArgs e)
 {
  if(allowClose)return;
  e.Cancel=true;if(closingSession)return;closingSession=true;Enabled=false;clock.Stop();
  try{using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(5));await auth.SignOutAsync(session,timeout.Token);}
  catch{ /* Always close the local session; an unreachable server session expires after 8 hours. */ }
  finally{allowClose=true;Close();}
 }
 private void Switch(UserControl page){foreach(Control old in content.Controls.Cast<Control>().ToArray())old.Dispose();content.Controls.Clear();content.Controls.Add(page);}
 protected override void Dispose(bool disposing){if(disposing)clock.Dispose();base.Dispose(disposing);}
 protected override void WndProc(ref Message m)
 {
  const int WM_NCHITTEST=0x84;base.WndProc(ref m);
  if(m.Msg==WM_NCHITTEST && WindowState==FormWindowState.Normal && (int)m.Result==1)
  {
   long packed=m.LParam.ToInt64();var point=PointToClient(new Point(unchecked((short)(packed&0xffff)),unchecked((short)((packed>>16)&0xffff))));
   bool l=point.X<7,r=point.X>ClientSize.Width-7,t=point.Y<7,b=point.Y>ClientSize.Height-7;
   m.Result=(IntPtr)(l&&t?13:r&&t?14:l&&b?16:r&&b?17:l?10:r?11:t?12:b?15:1);
  }
 }
 [DllImport("user32.dll")]private static extern bool ReleaseCapture();
 [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr hWnd,int msg,IntPtr wParam,IntPtr lParam);
}
