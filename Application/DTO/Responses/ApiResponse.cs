public class APIResponse<T>
{
    public string Message { get; init; } = "";
    public T Data { get; init; } = null;
}