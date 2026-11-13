using SimplusClinic.Domain;
namespace SimplusClinic.Application;
public interface IRepository<T> where T : Entity
{
 Task<IReadOnlyList<T>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default);
 Task<int> SaveAsync(T entity, CancellationToken ct = default);
}
public interface IAppointmentRepository : IRepository<Appointment>
{
 Task<ConflictResult> CheckConflictAsync(Appointment appointment, CancellationToken ct = default);
}
public interface IOverviewRepository
{
 Task<IReadOnlyList<LookupItem>> PatientsAsync(int includeId = 0, CancellationToken ct = default);
 Task<IReadOnlyList<LookupItem>> DoctorsAsync(int includeId = 0, CancellationToken ct = default);
 Task<IReadOnlyList<string>> SpecializationsAsync(CancellationToken ct = default);
 Task<DashboardSummary> SummaryAsync(DateTime day, CancellationToken ct = default);
 Task<IReadOnlyList<Appointment>> ScheduleAsync(DateTime day, CancellationToken ct = default);
 Task<IReadOnlyList<ReportRow>> ReportAsync(ReportKind kind, int doctorId, string specialization, int page, CancellationToken ct = default);
}
