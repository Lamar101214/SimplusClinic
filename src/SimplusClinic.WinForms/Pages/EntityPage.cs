using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SimplusClinic.Domain;
using SimplusClinic.Application;

namespace SimplusClinic.WinForms;

internal sealed class EntityPage<T> : PageBase
    where T : Entity, new()
{
    private readonly AppServices services;
    private readonly EntityService<T> service;

    private readonly DataGridView grid = Theme.Grid();

    private readonly Dictionary<string, TextBox> filters = [];

    private readonly CheckBox archived = new()
    {
        Text = "Include archived",
        AutoSize = true,
        ForeColor = Theme.Muted,
        Margin = new Padding(0, 4, 0, 12)
    };

    private readonly Label status =
        Theme.Label("Loading…", 10, Theme.Muted);

    private readonly Button previous = Theme.Button("Previous");
    private readonly Button next = Theme.Button("Next");

    private readonly System.Windows.Forms.Timer debounce = new()
    {
        Interval = 300
    };

    private CancellationTokenSource? request;

    private int page;
    private int generation;
    private bool mutating;

    public EntityPage(
        AppServices services,
        EntityService<T> service)
    {
        this.services = services;
        this.service = service;

        SuspendLayout();

        bool person =
            typeof(T) == typeof(Patient) ||
            typeof(T) == typeof(Doctor);

        bool doctor = typeof(T) == typeof(Doctor);
        bool appointment = typeof(T) == typeof(Appointment);

        string title = typeof(T) == typeof(MedicalRecord)
            ? "Medical records"
            : typeof(T).Name + "s";

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

        // Headings, filters, and actions size to their contents.
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // The grid occupies the remaining space.
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Page heading
        var heading = Theme.Label(title, 26);
        heading.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        heading.Margin = new Padding(0, 0, 0, 4);
        heading.Padding = Padding.Empty;

        layout.Controls.Add(heading, 0, 0);

        // Page subtitle
        var subtitle = Theme.Label(
            "Manage your clinic’s " + title.ToLower() +
            ". Search updates as you type.",
            10,
            Theme.Muted);

        subtitle.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        subtitle.Margin = new Padding(0, 0, 0, 24);
        subtitle.Padding = Padding.Empty;

        layout.Controls.Add(subtitle, 0, 1);

        // Search filters
        var search = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8),
            Padding = Padding.Empty
        };

        void Filter(string key, string label)
        {
            var box = Theme.Input();

            box.AccessibleName = label;
            box.PlaceholderText = label;
            box.Width = 185;
            box.Margin = new Padding(0, 0, 12, 12);

            box.MaxLength =
                key.EndsWith("Id") || key == "Id" ? 20 :
                key == "LicenseNumber" ? 60 :
                key == "Specialization" ? 120 :
                250;

            box.TextChanged += (_, _) => Queue();

            filters[key] = box;
            search.Controls.Add(box);
        }

        if (person)
        {
            Filter("Id", title.TrimEnd('s') + " ID");
            Filter("Name", "First, middle or last name");

            if (doctor)
            {
                Filter("Specialization", "Specialization");
                Filter("LicenseNumber", "License number");
            }
        }
        else
        {
            Filter("PatientId", "Patient ID");
            Filter("PatientName", "Patient name");
            Filter("DoctorId", "Doctor ID");
            Filter("DoctorName", "Doctor name");
        }

        if (!appointment)
        {
            archived.CheckedChanged += (_, _) => Queue();
            search.Controls.Add(archived);
        }

        layout.Controls.Add(search, 0, 2);

        // Action buttons
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8),
            Padding = Padding.Empty
        };

        var addButton = Theme.Button(
            "+ Add new",
            async (_, _) => await Ui.RunAsync(
                this,
                () => EditAsync(true, false)));

        var editButton = Theme.Button(
            "Edit",
            async (_, _) => await Ui.RunAsync(
                this,
                () => EditAsync(false, false)));

        var viewButton = Theme.Button(
            "View details",
            async (_, _) => await Ui.RunAsync(
                this,
                () => EditAsync(false, true)));

        var archiveButton = Theme.Button(
            appointment ? "Cancel booking" : "Archive / restore",
            async (_, _) => await Ui.RunAsync(this, ArchiveAsync));

        archiveButton.Width = 160;
        archiveButton.BackColor = Theme.Elevated;

        var refreshButton = Theme.Button(
            "Refresh",
            async (_, _) => await Ui.RunAsync(this, LoadAsync));

        foreach (var button in new[]
        {
            addButton,
            editButton,
            viewButton,
            archiveButton,
            refreshButton
        })
        {
            button.Margin = new Padding(0, 0, 10, 8);
            actions.Controls.Add(button);
        }

        layout.Controls.Add(actions, 0, 3);

        // Data grid
        grid.Margin = Padding.Empty;

        if (person)
        {
            Theme.Columns(
                grid,
                ("Id", "ID", 12),
                ("FullName", "Name", 38));

            if (doctor)
            {
                Theme.Columns(
                    grid,
                    ("Specialization", "Specialization", 26),
                    ("LicenseNumber", "License", 22));
            }

            Theme.Columns(
                grid,
                ("MobileNumber", "Mobile", 26),
                ("CityProvince", "City / Province", 26),
                ("IsArchived", "Archived", 15));
        }
        else
        {
            Theme.Columns(
                grid,
                ("Id", "ID", 10),
                ("PatientName", "Patient", 28),
                ("DoctorName", "Doctor", 28));

            if (appointment)
            {
                Theme.Columns(
                    grid,
                    ("StartAt", "Start", 30),
                    ("EndAt", "End", 30),
                    ("Status", "Status", 20));
            }
            else
            {
                Theme.Columns(
                    grid,
                    ("TreatmentDate", "Treatment date", 24),
                    ("Diagnosis", "Diagnosis", 45),
                    ("IsArchived", "Archived", 16));
            }
        }

        foreach (DataGridViewColumn column in grid.Columns)
        {
            if (column.DataPropertyName is "StartAt" or "EndAt")
            {
                column.DefaultCellStyle.Format =
                    "dd MMM yyyy HH:mm";
            }
            else if (column.DataPropertyName == "TreatmentDate")
            {
                column.DefaultCellStyle.Format = "dd MMM yyyy";
            }
        }

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                await Ui.RunAsync(
                    this,
                    () => EditAsync(false, true));
            }
        };

        layout.Controls.Add(grid, 0, 4);

        // Pagination
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 12, 0, 0),
            Padding = Padding.Empty
        };

        previous.Width = 100;
        next.Width = 100;

        previous.Margin = new Padding(0, 0, 10, 8);
        next.Margin = new Padding(0, 0, 10, 8);
        status.Margin = new Padding(12, 10, 0, 8);

        previous.Click += async (_, _) =>
        {
            if (page > 0)
            {
                page--;
                await Ui.RunAsync(this, LoadAsync);
            }
        };

        next.Click += async (_, _) =>
        {
            page++;
            await Ui.RunAsync(this, LoadAsync);
        };

        footer.Controls.AddRange([previous, next, status]);

        layout.Controls.Add(footer, 0, 5);
        Controls.Add(layout);

        debounce.Tick += async (_, _) =>
        {
            debounce.Stop();
            await Ui.RunAsync(this, LoadAsync);
        };

        Load += async (_, _) =>
            await Ui.RunAsync(this, LoadAsync);

        ResumeLayout(true);
    }

    private void Queue()
    {
        page = 0;
        generation++;

        request?.Cancel();

        debounce.Stop();
        debounce.Start();
    }

    private string F(string key)
    {
        return filters.TryGetValue(key, out var textBox)
            ? textBox.Text
            : "";
    }

    private async Task LoadAsync()
    {
        debounce.Stop();

        request?.Cancel();
        request?.Dispose();

        request = CancellationTokenSource.CreateLinkedTokenSource(
            Lifetime.Token);

        int current = ++generation;

        status.Text = "Loading…";
        previous.Enabled = false;
        next.Enabled = false;

        try
        {
            var rows = await service.SearchAsync(
                new SearchCriteria(
                    F("Id"),
                    F("Name"),
                    F("Specialization"),
                    F("LicenseNumber"),
                    F("PatientId"),
                    F("PatientName"),
                    F("DoctorId"),
                    F("DoctorName"),
                    archived.Checked,
                    page),
                request.Token);

            if (current != generation || IsDisposed)
                return;

            grid.DataSource = rows.ToList();

            previous.Enabled = page > 0;
            next.Enabled = rows.Count == 100;

            status.Text =
                $"Page {page + 1}  ·  {rows.Count} results" +
                (rows.Count == 0
                    ? "  ·  No matching records"
                    : "");
        }
        catch
        {
            if (!IsDisposed && current == generation)
            {
                status.Text = "Unable to load. Use Refresh to retry.";
                previous.Enabled = page > 0;
            }

            throw;
        }
    }

    private T Selected()
    {
        return grid.CurrentRow?.DataBoundItem as T
            ?? throw new ClinicException("Select a row first.");
    }

    private static T Copy(T value)
    {
        return JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(value))!;
    }

    private async Task EditAsync(bool add, bool readOnly)
    {
        T value = add ? new T() : Copy(Selected());

        if (add && value is Appointment appointment)
        {
            var now = DateTime.Now;

            appointment.StartAt = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                0).AddMinutes(30);

            appointment.EndAt = appointment.StartAt.AddMinutes(30);
        }

        using var editor = new EntityEditor<T>(
            value,
            service,
            services,
            readOnly);

        if (editor.ShowDialog(this) == DialogResult.OK)
            await LoadAsync();
    }

    private async Task ArchiveAsync()
    {
        if (mutating)
            return;

        T value = Copy(Selected());
        string action;

        if (value is Appointment appointment)
        {
            if (appointment.Status == "Completed")
            {
                throw new ClinicException(
                    "Completed visits cannot be cancelled from this " +
                    "action. Use Edit to correct the status.");
            }

            appointment.Status = "Cancelled";
            action = "Cancel appointment";
        }
        else
        {
            var property = typeof(T).GetProperty("IsArchived")!;
            bool isArchived = (bool)property.GetValue(value)!;

            property.SetValue(value, !isArchived);

            action = isArchived
                ? "Restore record"
                : "Archive record";
        }

        if (!Ui.Confirm(
            this,
            action,
            "Apply this change to item #" + value.Id +
            "? Medical history and linked data will be retained."))
        {
            return;
        }

        mutating = true;

        try
        {
            await service.SaveAsync(value, Lifetime.Token);
            await LoadAsync();
        }
        finally
        {
            mutating = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            debounce.Dispose();

            request?.Cancel();
            request?.Dispose();
        }

        base.Dispose(disposing);
    }
}