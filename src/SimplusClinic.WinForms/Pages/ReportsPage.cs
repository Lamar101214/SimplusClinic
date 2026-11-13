using SimplusClinic.Domain;
namespace SimplusClinic.WinForms;
internal sealed class ReportsPage : PageBase
{
 private readonly AppServices services;
 private readonly ComboBox kind=Theme.Combo(),doctor=Theme.Combo(),specialization=Theme.Combo();
 private readonly DataGridView grid=Theme.Grid();
 private readonly Label state=Theme.Label("",10,Theme.Muted);
 private readonly Button previous=Theme.Button("Previous"),next=Theme.Button("Next");
 private int page,generation;private bool ready;private CancellationTokenSource? request;
 public ReportsPage(AppServices services)
 {
  this.services=services;var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5};
  layout.RowStyles.Add(new(SizeType.Absolute,55));layout.RowStyles.Add(new(SizeType.Absolute,65));layout.RowStyles.Add(new(SizeType.Absolute,40));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,55));
  layout.Controls.Add(Theme.Label("Reports & directory",26),0,0);
  var filters=new FlowLayoutPanel{Dock=DockStyle.Fill};kind.Width=275;doctor.Width=260;specialization.Width=240;
  kind.Items.AddRange(["Complete patient list","Complete doctor list","Patients per doctor","Doctors per specialization"]);kind.SelectedIndex=0;
  kind.AccessibleName="Report type";doctor.AccessibleName="Doctor";specialization.AccessibleName="Specialization";
  filters.Controls.AddRange([kind,doctor,specialization]);layout.Controls.Add(filters,0,1);
  layout.Controls.Add(Theme.Label("Active records · Patient / doctor links include medical records and non-cancelled appointments.",10,Theme.Muted),0,2);
  Theme.Columns(grid,("Id","ID",15),("Name","Full name",40),("Detail","Location / Specialization",35),("MobileNumber","Mobile number",25));layout.Controls.Add(grid,0,3);
  var footer=new FlowLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(0,10,0,0)};previous.Width=100;next.Width=100;footer.Controls.AddRange([previous,next,Theme.Button("Refresh",async(_,_)=>await Ui.RunAsync(this,InitializeAsync)),state]);layout.Controls.Add(footer,0,4);Controls.Add(layout);
  foreach(var combo in new[]{kind,doctor,specialization})combo.SelectedIndexChanged+=async(_,_)=>{if(ready){page=0;await Ui.RunAsync(this,ReloadAsync);}};
  previous.Click+=async(_,_)=>{if(page>0){page--;await Ui.RunAsync(this,ReloadAsync);}};next.Click+=async(_,_)=>{page++;await Ui.RunAsync(this,ReloadAsync);};
  Load+=async(_,_)=>await Ui.RunAsync(this,InitializeAsync);
 }
 private async Task InitializeAsync()
 {
  ready=false;var doctors=await services.Overview.DoctorsAsync(0,Lifetime.Token);var specs=await services.Overview.SpecializationsAsync(Lifetime.Token);if(IsDisposed)return;
  doctor.DisplayMember="Name";doctor.DataSource=doctors.ToList();specialization.DataSource=specs.ToList();ready=true;page=0;await ReloadAsync();
 }
 private async Task ReloadAsync()
 {
  request?.Cancel();request?.Dispose();request=CancellationTokenSource.CreateLinkedTokenSource(Lifetime.Token);int current=++generation;
  var selected=(ReportKind)kind.SelectedIndex;doctor.Visible=selected==ReportKind.PatientsPerDoctor;specialization.Visible=selected==ReportKind.DoctorsPerSpecialization;
  previous.Enabled=false;next.Enabled=false;state.Text="Loading…";
  var rows=await services.Overview.ReportAsync(selected,(doctor.SelectedItem as LookupItem)?.Id??0,specialization.SelectedItem?.ToString()??"",page,request.Token);
  if(current!=generation||IsDisposed)return;grid.DataSource=rows.ToList();previous.Enabled=page>0;next.Enabled=rows.Count==100;state.Text=$"Page {page+1} · {rows.Count} rows";
 }
 protected override void Dispose(bool disposing){if(disposing){request?.Cancel();request?.Dispose();}base.Dispose(disposing);}
}
