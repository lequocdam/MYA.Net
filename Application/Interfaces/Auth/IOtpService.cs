public interface IOtpService
{
    Task SendOtpAsync(RegisterDTO dto);
    Task<UserDTO> VerifyOtpAsync(OtpDTO dto);
}