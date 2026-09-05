namespace App.Domain.Entities;

public enum ContactChangeType
{
    Email = 1,
    Phone = 2
}

public enum UserChangeStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Expired = 4
}

/// <summary>
/// Đại diện cho MỘT yêu cầu đổi Email/Phone đang chờ xác nhận qua OTP.
/// Đây là entity cho WORKFLOW (pending → confirmed/cancelled/expired),
/// KHÔNG phải bảng audit log — record vẫn được giữ lại sau khi xử lý xong
/// (không xoá) để phục vụ truy vết, nhưng trách nhiệm chính là điều phối luồng.
/// </summary>
public class UserChange
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public UserChangeType Type { get; private set; }
    public string NewValue { get; private set; } = default!;
    public UserChangeStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime ExpiredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public byte[] RowVersion { get; private set; } = default!;

    private static readonly TimeSpan PendingChangeExpiry = TimeSpan.FromMinutes(15);

    private UserChange() {}

    public static UserChange Create(
        Guid userId,
        UserChangeType type,
        string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue))
            throw new DomainException("NewValue is required.");

        if (!Enum.IsDefined(type))
            throw new DomainException("Invalid type.");

        return new UserChange
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            NewValue = newValue.Trim(),
            Status = UserChangeStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiredAt = DateTime.UtcNow.Add(PendingChangeExpiry)
        };
    }

    /// <summary>
    /// Hết hạn theo thời gian NHƯNG status trong DB vẫn có thể là Pending
    /// (chưa có job quét để set Expired) — nên luôn check field này riêng,
    /// đừng chỉ dựa vào Status khi quyết định có cho confirm hay không.
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Có thể confirm được hay không — gộp cả 2 điều kiện trên để tránh
    /// caller quên check 1 trong 2 (đã từng là nguồn gốc bug ở review trước).
    /// </summary>
    public bool CanConfirm() => IsPending() && !IsExpired();

    public void MarkConfirmed()
    {
        EnsureTransition(UserChangeStatus.Confirmed);
        Status = UserChangeStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void MarkCancelled()
    {
        // Cancel là no-op an toàn nếu đã ở trạng thái cuối — tránh throw
        // khi CancelAllPendingAsync chạy trùng với 1 confirm vừa xảy ra.
        if (Status != UserChangeStatus.Pending)
            return;

        Status = UserChangeStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status != UserChangeStatus.Pending)
            return;

        Status = UserChangeStatus.Expired;
    }

    /// <summary>
    /// Chặn chuyển trạng thái không hợp lệ, vd Confirmed -> Confirmed lần 2,
    /// hoặc Cancelled -> Confirmed. State machine chỉ cho phép Pending -> *.
    /// </summary>
    private void EnsureTransition(UserChangeStatus target)
    {
        if (Status != UserChangeStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot transition UserChange {Id} from {Status} to {target}.");
    }
}