# Simplus Clinic

**Login update:** Start with [START_HERE_LOGIN.md](START_HERE_LOGIN.md). Existing installations run only `Database/03_LoginUpgrade.sql`, then create the first administrator in the app.

Modular C# / .NET 10 Windows Forms clinic application using SQL Server and ADO.NET. The attached Clinic Management System case study is the functional baseline. All requested modules are implemented as editable source, not a UI mockup.

## Start here

1. On Windows, install the .NET 10 SDK and SQL Server / SQL Server Express LocalDB. Use a Visual Studio version with .NET 10 and the **.NET desktop development** workload, or the dotnet CLI.
2. In SSMS, enable **Query → SQLCMD Mode**. Execute `Database/01_CreateDatabase.sql`, then `Database/02_ProceduresAndPermissions.sql`, then `Database/03_LoginUpgrade.sql`. The first script stops if the database already exists; do not rerun it against a populated database. No automatic migration runs at application startup.
3. Edit `src/SimplusClinic.WinForms/App.config`. The supplied connection uses `(localdb)\MSSQLLocalDB` and Windows authentication. For SQL Express use `Server=.\SQLEXPRESS` or your actual server. `TrustServerCertificate=True` is a local-development setting; use a trusted server certificate and `False` for deployment.
4. Open `SimplusClinic.slnx`, set `SimplusClinic.WinForms` as the startup project and build, or run:

```powershell
.\build.ps1
# Launch after building:
dotnet run --project src/SimplusClinic.WinForms
# Optional Windows x64 deployment directory, including runtime:
.\build.ps1 -Publish
```

5. Add a patient and doctor first. Create a medical record or appointment using the linked dropdowns. An empty installation intentionally contains no real or demo patient data.

## Included behavior

- Patients: every requested demographic, address, contact and guardian field; add, edit, view, archive and restore.
- Doctors: every requested demographic/contact field, specialization and unique license; add, edit, view, archive and restore.
- Medical records: linked patient/doctor names, diagnosis, treatment, prescriptions and date; add, edit, view, archive and restore. SQL temporal history retains earlier versions.
- Appointments: patient/doctor lookups, start/end date and time, status, edit/view/cancel; live debounced conflict status and a dark alert modal on rejected save.
- Simultaneous ID/name/specialization/license filters, parameterized `LIKE` queries and joins. Search is case-sensitive or insensitive according to database collation. Wildcard characters typed by users are treated literally. Names match first/middle/last components, first-last, full first-middle-last and last-first order.
- Overview: live counts and today’s appointments. Reports: active patients, active doctors, active patients per doctor and doctors per specialization. Each directory/report is paged at 100 rows, with no overall result cap.
- Responsive database I/O, stale-search cancellation, optimistic rowversion checks, archive-safe relationships, operation audit records and database-enforced scheduling.
- Dark dashboard, sidebar, custom window controls, resize borders, rounded buttons, keyboard-focusable controls, styled grids, status colors, scrollable editors and DPI scaling.

## Projects and responsibility

| Project | Responsibility |
| --- | --- |
| Domain | POCO models, search/report DTOs and interval rules |
| Application | Repository contracts, validation and services |
| Infrastructure | Static DatabaseHelper, typed ADO.NET mapping, parameterized queries, repositories |
| WinForms | Composition root, dashboard, reusable entity editors, module pages, reports and theme controls |
| Tests | Console-based rule/service tests and opt-in live SQL integration checks |

The UI never constructs SQL. Services validate entities; repositories call stored procedures for writes. Connections, commands and readers are disposed per operation. Stored procedures are the final concurrency authority.

## Scheduling policy

Intervals are half-open: `[StartAt, EndAt)`. A 10:00–10:30 appointment can be followed by 10:30–11:00. Overlap is `existing.StartAt < requested.EndAt AND existing.EndAt > requested.StartAt`. The same doctor **or** patient blocks the slot. Both scheduled and completed appointments reserve their historical interval; cancelled appointments do not. Updates exclude their own ID. Overnight appointments work; duration must be positive and at most 24 hours.

The editor performs a debounced preview; saving repeats validation in the service. `SaveAppointment` then checks again inside a transaction holding an exclusive application lock. All save procedures share this lock so archiving cannot race scheduling. This deliberately favors correctness over throughput for a small clinic. Only procedure writes are allowed for the `clinic_staff` database role. A database owner can bypass these protections and must not be used as the deployment identity.

## Record retention and report meaning

“Delete” is implemented as archive for patient, doctor and medical-record data, and cancel for appointments. References are never cascade-deleted. Archiving a patient or doctor requires completing or cancelling **all** scheduled appointments, including overdue ones. Historical medical records can retain archived links. Existing archived names appear in the editor of their linked record. Restore uses the same concurrency checks.

Patients per doctor means distinct active patients associated through an active medical record **or any non-cancelled appointment**. Dropdowns list active doctors; use the medical-record search to inspect historical records for archived doctors. Complete lists are active directories; the patient/doctor modules provide the include-archived view.

## Verification

See `docs/VERIFICATION.md` for executed checks and outstanding checks. The login update was cross-compiled with the .NET 10 SDK on Linux and passed all 42 console rule/service checks. Windows UI operation and live SQL Server integration have not been tested here. Complete the supplied Windows acceptance checklist before using real clinical data.
