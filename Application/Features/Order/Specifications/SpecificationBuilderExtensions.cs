public static class SpecificationBuilderExtensions
{
    public static ISpecificationBuilder<Order> ApplyPermission(
        this ISpecificationBuilder<Order> builder,
        CurrentUser currentUser)
    {
        if (currentUser.Role == Role.ADMIN)
        {
            return builder;
        }

        if (currentUser.Role == Role.MANAGER)
        {
            builder.Where(x => x.WarehouseId == currentUser.WarehouseId);
        }

        if (currentUser.Role == Role.USER)
        {
            builder.Where(x => x.UserId == currentUser.userId);
        }

        return builder;
    }

    public static ISpecificationBuilder<Order> ApplyFilter(
        this ISpecificationBuilder<Order> builder,
        FilterrRequest filter)
    {
        if (filter.Status.HasValue)
        {
            builder.Where(x => x.Status == filter.Status);
        }

        return builder;
    }

    public static ISpecificationBuilder<Order> ApplyPage(
        this ISpecificationBuilder<Order> builder,
        FilterRequest filter)
    {
        return builder
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize);
    }

    public static ISpecificationBuilder<Order> ApplySort(
        this ISpecificationBuilder<Order> builder,
        FilterRequest filter)
    {
        builder.OrderByDescending(x => x.CreatedAt);

        return builder;
    }
}