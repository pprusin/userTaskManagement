namespace Models
{
    public class TaskItem
{
    public int TaskId { get; set; }
    public string Tytul { get; set; }
    public string Opis { get; set; }
    public string Status { get; set; }
    public DateTime DataZlozenia { get; set; }
    public int? UserId { get; set; }
    public DateTime? DueDate { get; set; }
}
}

