using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimplusClinic.WinForms;

internal sealed class DashboardPage : PageBase
{
    private readonly AppServices services;

    private readonly Label[] metrics = Enumerable.Range(0, 4)
        .Select(_ => Theme.Label("—", 30))
        .ToArray();

    private readonly DataGridView grid = Theme.Grid();

    private readonly Label state =
        Theme.Label("Loading…", 10, Theme.Muted);

    public DashboardPage(AppServices services)
    {
        this.services = services;

        SuspendLayout();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        // Text and cards determine their own height.
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // The appointment table uses the remaining space.
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Main heading
        var heading = Theme.Label("Your clinic, at a glance", 26);
        heading.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        heading.Margin = new Padding(0, 0, 0, 4);
        heading.Padding = Padding.Empty;

        // Subtitle
        var subtitle = Theme.Label(
            "Welcome back. Here’s what’s happening today.",
            11,
            Theme.Muted);

        subtitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        subtitle.Margin = new Padding(0, 0, 0, 24);
        subtitle.Padding = Padding.Empty;

        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);

        // Metric cards
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 24),
            Padding = Padding.Empty
        };

        cards.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        string[] names =
        [
            "ACTIVE PATIENTS",
            "ACTIVE DOCTORS",
            "TODAY’S VISITS",
            "MEDICAL RECORDS"
        ];

        for (int i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 25));

            var card = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.Surface,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, i == 3 ? 0 : 14, 0)
            };

            card.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            card.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var caption = Theme.Label(names[i], 9, Theme.Muted);
            caption.Margin = new Padding(0, 0, 0, 12);
            caption.BackColor = Theme.Surface;

            metrics[i].Margin = Padding.Empty;
            metrics[i].BackColor = Theme.Surface;

            card.Controls.Add(caption, 0, 0);
            card.Controls.Add(metrics[i], 0, 1);

            cards.Controls.Add(card, i, 0);
        }

        layout.Controls.Add(cards, 0, 2);

        // Appointment heading and refresh button
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16),
            Padding = Padding.Empty
        };

        toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        toolbar.ColumnStyles.Add(
            new ColumnStyle(SizeType.AutoSize));

        toolbar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var appointmentsTitle =
            Theme.Label("Today’s appointments", 17);

        appointmentsTitle.Anchor = AnchorStyles.Left;
        appointmentsTitle.Margin = Padding.Empty;

        var refreshButton = Theme.Button(
            "Refresh",
            async (_, _) => await Ui.RunAsync(this, RefreshAsync));

        refreshButton.Anchor = AnchorStyles.Right;
        refreshButton.Margin = Padding.Empty;

        toolbar.Controls.Add(appointmentsTitle, 0, 0);
        toolbar.Controls.Add(refreshButton, 1, 0);

        layout.Controls.Add(toolbar, 0, 3);

        // Appointment table
        grid.Margin = Padding.Empty;

        Theme.Columns(
            grid,
            ("StartAt", "Time", 25),
            ("PatientName", "Patient", 35),
            ("DoctorName", "Doctor", 35),
            ("Status", "Status", 20));

        grid.Columns[0].DefaultCellStyle.Format = "HH:mm";

        layout.Controls.Add(grid, 0, 4);

        // Footer
        state.Margin = new Padding(0, 12, 0, 0);
        layout.Controls.Add(state, 0, 5);

        Controls.Add(layout);

        Load += async (_, _) =>
            await Ui.RunAsync(this, RefreshAsync);

        ResumeLayout(true);
    }

    private async Task RefreshAsync()
    {
        var summary = await services.Overview.SummaryAsync(
            DateTime.Today,
            Lifetime.Token);

        var rows = await services.Overview.ScheduleAsync(
            DateTime.Today,
            Lifetime.Token);

        if (IsDisposed)
            return;

        int[] values =
        [
            summary.Patients,
            summary.Doctors,
            summary.TodayAppointments,
            summary.MedicalRecords
        ];

        for (int i = 0; i < metrics.Length; i++)
            metrics[i].Text = values[i].ToString("N0");

        grid.DataSource = rows.ToList();

        state.Text = rows.Count == 0
            ? "No appointments scheduled for today."
            : "Updated " + DateTime.Now.ToString("HH:mm:ss")
              + " · Clinic local time";
    }
}