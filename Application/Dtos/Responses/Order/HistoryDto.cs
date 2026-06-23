public class HistoryDto
{
    public Guid Id { get; set; }

    public DateTime Date { get; set; }

    public Status Status { get; set; }

    public string Note { get; set; }

    public Guid UserId { get; set; }
}
