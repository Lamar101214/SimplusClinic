using SimplusClinic.Application;
namespace SimplusClinic.WinForms;
internal static class Ui
{
 public static async Task RunAsync(Control owner,Func<Task> action)
 {
  try { await action(); }
  catch(OperationCanceledException) { }
  catch(Exception ex) { if(!owner.IsDisposed)ShowError(owner,ex); }
 }
 public static void ShowError(IWin32Window? owner,Exception ex)
 {
  string message=ex is ClinicException?ex.Message:"An unexpected error occurred. Close this window and retry. Error type: "+ex.GetType().Name;
  using var dialog=new AlertDialog("Unable to complete request",message,false);
  if(owner==null)dialog.ShowDialog();else dialog.ShowDialog(owner);
 }
 public static bool Confirm(IWin32Window owner,string title,string message)
 {
  using var dialog=new AlertDialog(title,message,true);return dialog.ShowDialog(owner)==DialogResult.OK;
 }
}
internal sealed class AlertDialog : Form
{
 public AlertDialog(string title,string message,bool confirm)
 {
  Theme.Apply(this);Text=title;StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(540,280);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;
  var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(26),RowCount=3,ColumnCount=1};
  layout.RowStyles.Add(new(SizeType.Absolute,45));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,44));
  layout.Controls.Add(Theme.Label(title,16,Theme.Accent),0,0);
  layout.Controls.Add(new Label {Text=message,Dock=DockStyle.Fill,AutoEllipsis=true},0,1);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};
  var ok=Theme.Button(confirm?"Confirm":"Close");ok.DialogResult=DialogResult.OK;actions.Controls.Add(ok);AcceptButton=ok;
  if(confirm){var cancel=Theme.Button("Cancel");cancel.BackColor=Theme.Elevated;cancel.DialogResult=DialogResult.Cancel;actions.Controls.Add(cancel);CancelButton=cancel;}
  layout.Controls.Add(actions,0,2);Controls.Add(layout);
 }
}
internal abstract class PageBase : UserControl
{
 protected readonly CancellationTokenSource Lifetime=new();
 protected PageBase(){Theme.Apply(this);Dock=DockStyle.Fill;Padding=new Padding(28);}
 protected override void Dispose(bool disposing){if(disposing){Lifetime.Cancel();Lifetime.Dispose();}base.Dispose(disposing);}
}
