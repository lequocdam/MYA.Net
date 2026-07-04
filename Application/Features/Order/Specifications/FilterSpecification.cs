public sealed class FilterSpecification : Specification<Order>
{
    public FilterSpecification(
        CurrentUser currentUser,
        FilterRequest filter)
    {
        Query
            .ApplyPermission(currentUser)
            .ApplyFilter(filter)
            .ApplySearch(filter)
            .ApplySorting(filter)
            .ApplyPaging(filter);

        Query.AsNoTracking();
    }
}