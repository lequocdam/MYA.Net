using Ardalis.Specification;
using UserModule.Entities;
using UserModule.Enums;

public class UserFilterSpecification : Specification<User>
{
    public UserFilterSpecification(UserQueryParams query)
    {
        Query.AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();

            Query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Email.Contains(keyword) ||
                x.Phone.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.RoleCode))
        {
            Query.Where(x =>
                x.UserRoles.Any(r =>
                    r.Role.Code == query.RoleCode));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && 
            Enum.TryParse<UserStatus>(query.Status, true, out var status))
        {
            Query.Where(x => x.Status == status);
        }

    }

    private void ApplySorting(UserQueryParams query)
    {
        var isDesc =
            query.SortDir?.Equals(
                "desc",
                StringComparison.OrdinalIgnoreCase)
            == true;


        switch(query.SortBy?.ToLower())
        {
            case "fullname":
                if(isDesc)
                    Query.OrderByDescending(x=>x.FullName);
                else
                    Query.OrderBy(x=>x.FullName);
                break;


            case "email":
                if(isDesc)
                    Query.OrderByDescending(x=>x.Email);
                else
                    Query.OrderBy(x=>x.Email);
                break;


            case "createdat":
                if(isDesc)
                    Query.OrderByDescending(x=>x.CreatedAt);
                else
                    Query.OrderBy(x=>x.CreatedAt);
                break;


            default:
                Query.OrderByDescending(x=>x.CreatedAt);
                break;
        }
    }
}