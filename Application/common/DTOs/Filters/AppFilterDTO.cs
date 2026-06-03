public abstract class AppFilterDTO
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
    
    private int page = 1;
    public int Page
    {
        get => page;
        set => page = value < 1 ? 1 : value;
    }

    private int pageSize = 20;
    public int PageSize
    {
        get => pageSize;
        set => pageSize = value > 100
            ? 100
            : value < 1
                ? 1
                : value;
    }
}