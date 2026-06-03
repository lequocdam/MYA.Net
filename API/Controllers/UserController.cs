using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BCrypt.Net;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(){
        var user = await _userService.GetCurrentUser(User);

        return Ok(new ApiResponse<UserResponseDto>
        {
            Success = true,
            Data = user
        });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
    {
        // lấy userId từ JWT
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(int.Parse(userId));

        if (user == null)
            return NotFound("User not found");

        // kiểm tra mật khẩu cũ
        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
            return BadRequest("Old password is incorrect");

        // kiểm tra confirm password
        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest("Passwords do not match");

        // hash mật khẩu mới
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully" });
    }
}