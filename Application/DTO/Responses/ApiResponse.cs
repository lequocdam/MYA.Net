public class ApiResponse<T>
{
    public string Message { get; init; } = "";
    public T? Data { get; init; }
}