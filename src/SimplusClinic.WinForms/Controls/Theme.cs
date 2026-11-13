using System.Drawing.Drawing2D;
namespace SimplusClinic.WinForms;
internal static class Theme
{
 public static readonly Color Background=ColorTranslator.FromHtml("#1E1E2E"), Surface=ColorTranslator.FromHtml("#252538"),
  Elevated=ColorTranslator.FromHtml("#303048"), Accent=ColorTranslator.FromHtml("#00ADB5"), Blue=ColorTranslator.FromHtml("#3A86FF"),
  Text=ColorTranslator.FromHtml("#EEEEEE"), Muted=ColorTranslator.FromHtml("#AAAABD"), Danger=ColorTranslator.FromHtml("#FF718B"), Success=ColorTranslator.FromHtml("#59D6B0");
 public static void Apply(Control c)
 {
  c.BackColor=Background;c.ForeColor=Text;c.Font=new Font("Segoe UI",10F);
 }
    public static Label Label(
       string text,
       int size = 10,
       Color? color = null)
    {
        return new AlignedLabel
        {
            Text = text,
            AutoSize = true,
            ForeColor = color ?? Text,
            Font = new Font("Segoe UI", size),
            Margin = new Padding(0, 0, 0, 8),
            Padding = Padding.Empty,
            TextAlign = ContentAlignment.TopLeft
        };
    }
    public static Button Button(string text,EventHandler? click=null)
 {
  var b=new RoundedButton {Text=text,AutoSize=false,Size=new Size(130,40),Margin=new Padding(0,0,10,0)};
  if(click!=null)b.Click+=click;
  return b;
 }
 public static TextBox Input() => new(){BackColor=Elevated,ForeColor=Text,BorderStyle=BorderStyle.FixedSingle,Width=185,MaxLength=250};
 public static ComboBox Combo() => new(){BackColor=Elevated,ForeColor=Text,FlatStyle=FlatStyle.Flat,DropDownStyle=ComboBoxStyle.DropDownList,Width=240};
 public static DataGridView Grid()
 {
  var g=new DataGridView {Dock=DockStyle.Fill,BackgroundColor=Surface,BorderStyle=BorderStyle.None,ReadOnly=true,
   AllowUserToAddRows=false,AllowUserToDeleteRows=false,AllowUserToResizeRows=false,RowHeadersVisible=false,
   AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,
   AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,EnableHeadersVisualStyles=false,
   GridColor=Elevated,ColumnHeadersHeight=44,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing};
  g.ColumnHeadersDefaultCellStyle=new(){BackColor=Elevated,ForeColor=Muted,Font=new Font("Segoe UI Semibold",10),Padding=new Padding(10,0,0,0),SelectionBackColor=Elevated};
  g.DefaultCellStyle=new(){BackColor=Surface,ForeColor=Text,SelectionBackColor=ColorTranslator.FromHtml("#164B57"),SelectionForeColor=Text,Padding=new Padding(10,0,0,0),NullValue="—"};
  g.AlternatingRowsDefaultCellStyle.BackColor=ColorTranslator.FromHtml("#28283D");
  g.RowTemplate.Height=46;
  g.CellFormatting+=(_,e)=>{if(e.Value is string s && s is "Scheduled" or "Completed" or "Cancelled") e.CellStyle!.ForeColor=s=="Completed"?Success:s=="Cancelled"?Danger:Blue;};
  return g;
 }
 public static void Columns(DataGridView grid,params (string Property,string Title,int Weight)[] columns)
 {
  foreach(var c in columns)grid.Columns.Add(new DataGridViewTextBoxColumn {DataPropertyName=c.Property,HeaderText=c.Title,FillWeight=c.Weight,MinimumWidth=70,SortMode=DataGridViewColumnSortMode.NotSortable});
 }
}
internal sealed class RoundedButton : Button
{
 public RoundedButton()
 {
  FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Theme.Accent;ForeColor=Color.White;
  FlatAppearance.MouseOverBackColor=Theme.Blue;FlatAppearance.MouseDownBackColor=Theme.Elevated;
  Cursor=Cursors.Hand;Font=new Font("Segoe UI Semibold",10);UseVisualStyleBackColor=false;
 }
 protected override void OnResize(EventArgs e)
 {
  base.OnResize(e);if(Width<16||Height<16)return;
  using var path=new GraphicsPath();const int d=16;
  path.AddArc(0,0,d,d,180,90);path.AddArc(Width-d,0,d,d,270,90);path.AddArc(Width-d,Height-d,d,d,0,90);path.AddArc(0,Height-d,d,d,90,90);path.CloseFigure();
  var previous=Region;Region=new Region(path);previous?.Dispose();
 }
}
internal sealed class AlignedLabel : Label
{
    protected override void OnPaint(PaintEventArgs e)
    {
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.Left |
            TextFormatFlags.Top |
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix);
    }
}
