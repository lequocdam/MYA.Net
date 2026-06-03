public class EmailSettings
{
    public string Host     { get; init; } = "";
    public int    Port     { get; init; } = 587;
    public bool   UseSsl   { get; init; } = false;   // true nếu port 465
    public string UserName { get; init; } = "";
    public string Password { get; init; } = "";
    public string FromName { get; init; } = "MyApp";
    public string FromAddress { get; init; } = "";
}