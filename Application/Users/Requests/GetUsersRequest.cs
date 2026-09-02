public sealed class GetUsersRequest
{
    public string? Keyword { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}