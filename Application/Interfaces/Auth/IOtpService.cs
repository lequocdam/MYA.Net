public interface IOtpService
{
    Task<string> SendOtpAsync(RegisterDTO dto, CancellationToken ct);
    Task ResendOtpAsync(ResendOtpDto dto, CancellationToken ct);
    Task<UserDTO> VerifyOtpAsync(OtpDTO dto);
}