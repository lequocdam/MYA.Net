public class FilterValidator : AbstractValidator<FilterDTO>
{
    public FilterValidator()
    {
        RuleFor(x => x.Page).NotNull()
            .WithMessage("Số trang không được trống!");

        RuleFor(x => x.PageSize).NotNull()
            .WithMessage("Số lượng trong một trang không được trống!");
    }
}
