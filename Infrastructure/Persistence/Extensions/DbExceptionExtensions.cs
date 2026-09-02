public static class DbExceptionExtensions
{
    private static readonly int[] UniqueViolationErrorCodes = { 2601, 2627 };

    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx && UniqueViolationErrorCodes.Contains(sqlEx.Number);
    }
}