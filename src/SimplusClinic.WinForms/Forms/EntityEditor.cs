using System.ComponentModel.DataAnnotations;
using System.Reflection;
using SimplusClinic.Domain;
using SimplusClinic.Application;
namespace SimplusClinic.WinForms;
internal sealed class EntityEditor<T> : Form where T : Entity
{
 private readonly T entity;
 private readonly EntityService<T> service;
 private readonly AppServices services;
 private readonly bool readOnly;
 private readonly Dictionary<string,Control> inputs=[];
 private readonly Label status=Theme.Label("* Required fields",10,Theme.Muted);
 private readonly Button save=Theme.Button("Save changes");
 private readonly System.Windows.Forms.Timer debounce=new(){Interval=400};
 private CancellationTokenSource? check;
 private readonly CancellationTokenSource lifetime=new();
 private int generation;
 private bool saving;
 public EntityEditor(T value,EntityService<T> service,AppServices services,bool readOnly=false)
 {
  entity=value;this.service=service;this.services=services;this.readOnly=readOnly;
  Theme.Apply(this);Text=(readOnly?"View":entity.Id==0?"Add":"Edit")+" "+typeof(T).Name;
  ClientSize=new Size(850,740);MinimumSize=new Size(700,560);StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;AutoScaleMode=AutoScaleMode.Dpi;
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(24),RowCount=4,ColumnCount=1};
  layout.RowStyles.Add(new(SizeType.Absolute,45));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,58));layout.RowStyles.Add(new(SizeType.Absolute,44));
  layout.Controls.Add(Theme.Label(Text+(entity.Id>0?"  #"+entity.Id:""),20),0,0);
  var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
  var fields=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(0,4,12,10)};
  fields.ColumnStyles.Add(new(SizeType.Percent,50));fields.ColumnStyles.Add(new(SizeType.Percent,50));
  int i=0;
  foreach(var spec in EditorFields.For(typeof(T)))
  {
   var property=typeof(T).GetProperty(spec.Property)!;
   Control input=CreateInput(spec,property.GetValue(entity));input.Name=spec.Property;input.AccessibleName=spec.Label;input.Dock=DockStyle.Top;input.Enabled=!readOnly;
   if(input is TextBox text)text.MaxLength=property.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength??250;
   inputs.Add(spec.Property,input);
   var cell=new Panel{Dock=DockStyle.Fill,Height=spec.Kind==FieldKind.Multiline?150:84,Margin=new Padding(0,0,18,8)};
   var label=Theme.Label(spec.Label,10,Theme.Muted);label.Dock=DockStyle.Top;label.Height=26;label.AutoSize=false;
   cell.Controls.Add(input);cell.Controls.Add(label);
   fields.Controls.Add(cell,i%2,i/2);i++;
   if(input is TextBox tb)tb.TextChanged+=Changed;
   if(input is ComboBox cb)cb.SelectedIndexChanged+=Changed;
   if(input is DateTimePicker dp)dp.ValueChanged+=Changed;
  }
  scroll.Controls.Add(fields);layout.Controls.Add(scroll,0,1);status.Dock=DockStyle.Fill;status.AutoSize=false;layout.Controls.Add(status,0,2);
  var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};
  save.Enabled=false;save.Visible=!readOnly;save.Click+=async(_,_)=>await Ui.RunAsync(this,SaveAsync);
  var cancel=Theme.Button(readOnly?"Close":"Cancel");cancel.BackColor=Theme.Elevated;cancel.DialogResult=DialogResult.Cancel;CancelButton=cancel;
  buttons.Controls.Add(save);buttons.Controls.Add(cancel);layout.Controls.Add(buttons,0,3);Controls.Add(layout);
  debounce.Tick+=async(_,_)=>{debounce.Stop();await Ui.RunAsync(this,CheckAsync);};
  Shown+=async(_,_)=>await Ui.RunAsync(this,InitializeAsync);
  FormClosing+=(_,e)=>{if(saving)e.Cancel=true;};
 }
    private Control CreateInput(FieldSpec spec, object? value)
    {
        if (spec.Kind is FieldKind.Date or FieldKind.DateTime)
        {
            var picker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = spec.Kind == FieldKind.Date
                    ? "dd MMM yyyy"
                    : "dd MMM yyyy  HH:mm",
                ShowUpDown = spec.Kind == FieldKind.DateTime
            };

            picker.Value = (DateTime)value!;
            return picker;
        }

        if (spec.Kind is FieldKind.Gender or FieldKind.Status
            or FieldKind.Patient or FieldKind.Doctor)
        {
            var combo = Theme.Combo();

            if (spec.Kind is FieldKind.Patient or FieldKind.Doctor)
            {
                // Set the source FIRST to avoid NotSupportedException.
                combo.AutoCompleteSource = AutoCompleteSource.ListItems;
                combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            }
            else
            {
                combo.Items.AddRange(
                    spec.Kind == FieldKind.Gender
                        ? new object[]
                        {
                        "Female",
                        "Male",
                        "Other",
                        "Prefer not to say"
                        }
                        : new object[]
                        {
                        "Scheduled",
                        "Completed",
                        "Cancelled"
                        });

                if (value is string selectedValue)
                {
                    if (!combo.Items.Contains(selectedValue))
                        combo.Items.Add(selectedValue);

                    combo.SelectedItem = selectedValue;
                }
            }

            return combo;
        }

        var text = Theme.Input();
        text.Text = value as string ?? "";

        if (spec.Kind == FieldKind.Multiline)
        {
            text.Multiline = true;
            text.Height = 112;
            text.ScrollBars = ScrollBars.Vertical;
            text.AcceptsReturn = true;
        }

        return text;
    }
    private async Task InitializeAsync()
 {
  foreach(var spec in EditorFields.For(typeof(T)).Where(f=>f.Kind is FieldKind.Patient or FieldKind.Doctor))
  {
   int id=(int)typeof(T).GetProperty(spec.Property)!.GetValue(entity)!;
   var options=spec.Kind==FieldKind.Patient?await services.Overview.PatientsAsync(id,lifetime.Token):await services.Overview.DoctorsAsync(id,lifetime.Token);
   if(IsDisposed)return;
   var combo=(ComboBox)inputs[spec.Property];combo.DisplayMember="Name";combo.ValueMember="Id";combo.DataSource=options.ToList();
   combo.SelectedIndex=-1;if(id!=0)combo.SelectedValue=id;
  }
  if(IsDisposed)return;save.Enabled=!readOnly;Changed(this,EventArgs.Empty);
 }
 private void ReadInputs()
 {
  foreach(var spec in EditorFields.For(typeof(T)))
  {
   Control input=inputs[spec.Property];
   object value=input switch {DateTimePicker d=>spec.Kind==FieldKind.Date?d.Value.Date:new DateTime(d.Value.Year,d.Value.Month,d.Value.Day,d.Value.Hour,d.Value.Minute,0), ComboBox c when spec.Kind is FieldKind.Patient or FieldKind.Doctor=>(c.SelectedItem as LookupItem)?.Id??0,ComboBox c=>c.SelectedItem?.ToString()??"",_=>input.Text.Trim()};
   typeof(T).GetProperty(spec.Property)!.SetValue(entity,value);
  }
 }
 private void Changed(object? sender,EventArgs e)
 {
  if(readOnly||saving||entity is not Appointment)return;
  generation++;check?.Cancel();debounce.Stop();status.Text="Checking schedule…";status.ForeColor=Theme.Muted;debounce.Start();
 }
 private async Task CheckAsync()
 {
  if(entity is not Appointment appointment||saving||readOnly)return;
  check?.Cancel();check?.Dispose();check=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);int current=generation;
  try
  {
   ReadInputs();var result=await services.Appointments.CheckConflictAsync(appointment,check.Token);
   if(current!=generation||IsDisposed)return;
   status.Text=result.Message;status.ForeColor=result.HasConflict?Theme.Danger:Theme.Success;
  }
  catch(ClinicException ex){if(!IsDisposed && current==generation){status.Text=ex.Message;status.ForeColor=Theme.Danger;}}
 }
 private async Task SaveAsync()
 {
  if(saving)return;saving=true;save.Enabled=false;foreach(var input in inputs.Values)input.Enabled=false;debounce.Stop();check?.Cancel();generation++;
  try
  {
   ReadInputs();await service.SaveAsync(entity,lifetime.Token);
   saving=false;DialogResult=DialogResult.OK;Close();
  }
  finally{saving=false;if(!IsDisposed){save.Enabled=true;foreach(var input in inputs.Values)input.Enabled=true;}}
 }
 protected override void Dispose(bool disposing)
 {
  if(disposing){debounce.Dispose();check?.Cancel();check?.Dispose();lifetime.Cancel();lifetime.Dispose();}base.Dispose(disposing);
 }
}
