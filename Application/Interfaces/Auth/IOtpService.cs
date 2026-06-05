public interface IOtpService
{
    Task SendOtpAsync(RegisterDTO dto, CancellationToken ct);
    Task<UserDTO> VerifyOtpAsync(OtpDTO dto);
}