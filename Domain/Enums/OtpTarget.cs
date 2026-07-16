namespace Domain.Enums;

public enum OtpPurpose
{
    Register = 1,

    /// <summary>
    /// Xác thực đăng nhập (Passwordless Login / 2FA).
    /// </summary>
    Login = 2,

    /// <summary>
    /// Xác thực yêu cầu khôi phục mật khẩu (Quên mật khẩu).
    /// </summary>
    ForgotPassword = 3,

    /// <summary>
    /// Xác thực khi thay đổi số điện thoại liên kết.
    /// </summary>
    ChangePhone = 4,

    /// <summary>
    /// Xác thực khi thay đổi địa chỉ Email liên kết.
    /// </summary>
    ChangeEmail = 5,

    /// <summary>
    /// Xác thực các giao dịch nhạy cảm hoặc yêu cầu bảo mật cao (Ví dụ: Rút tiền, chuyển khoản).
    /// </summary>
    TransactionVerification = 6,

    /// <summary>
    /// Xác thực trước khi thực hiện xóa tài khoản vĩnh viễn.
    /// </summary>
    DeleteAccount = 7
}