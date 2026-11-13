using SimplusClinic.Domain;
namespace SimplusClinic.Application;
public class EntityService<T>(IRepository<T> repository) where T : Entity
{
 public Task<IReadOnlyList<T>> SearchAsync(SearchCriteria criteria, CancellationToken ct = default)
 {
  if (criteria.Page < 0 || criteria.PageSize is < 1 or > 100) throw new ClinicException("Invalid page size.");

  return repository.SearchAsync(criteria, ct);
 }
 public virtual Task<int> SaveAsync(T entity, CancellationToken ct = default)
 {
  EntityValidator.Validate(entity);

  return repository.SaveAsync(entity, ct);
 }
}
public sealed class AppointmentService(IAppointmentRepository repository) : EntityService<Appointment>(repository)
{
public Task<ConflictResult> CheckConflictAsync(Appointment appointment, CancellationToken ct = default)
 {
  EntityValidator.Validate(appointment);

  return repository.CheckConflictAsync(appointment, ct);
 }
 public override async Task<int> SaveAsync(Appointment entity, CancellationToken ct = default)
 {
  var conflict = await CheckConflictAsync(entity, ct);
  if (conflict.HasConflict) throw new ClinicException(conflict.Message);
  
  return await base.SaveAsync(entity, ct);
 }
}
