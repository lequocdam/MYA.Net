public interface IOtpService
{
    Task SendOTPAsync(RegisterDTO dto);
    Task<UserDTO> VerifyOtpAsync(OtpDTO dto);
}