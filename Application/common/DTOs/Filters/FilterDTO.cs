public class FilterDTO
{
    public OrderStatus? Status   { get; set; }

    public string? Code { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }
 
    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 100 ? 100 : value < 1 ? 1 : value;
    }
}

