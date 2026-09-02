public sealed class UserCountSpecification : Specification<User>
{
    public UserCountSpecification(
        UserQueryParams query,
        ICurrentUser currentUser)
    {
        Query.AsNoTracking();

        ApplyPermission(currentUser);

        ApplyFilter(query);
    }


    private void ApplyFilter(
        UserQueryParams query)
    {

        if(!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword =
                query.Keyword.Trim();


            Query.Where(x =>
                x.Name.Contains(keyword));
        }


        if(query.Status.HasValue)
        {
            Query.Where(x =>
                x.Status == query.Status);
        }
    }
}