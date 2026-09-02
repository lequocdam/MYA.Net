using AutoMapper;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, CreateResponse>();
        CreateMap<User, UpdateResponse>();
        CreateMap<User, UserResponse>();
    }
}