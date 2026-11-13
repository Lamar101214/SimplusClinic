namespace SimplusClinic.Domain;
public abstract class Entity
{
 public int Id { get; set; }
 public byte[] Version { get; set; } = [];
}
