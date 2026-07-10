using AutoMapper;

public class AddressProfile : Profile
{
    public AddressProfile()
    {
        CreateMap<UpdateCommand, UpdateRequest>();

        CreateMap<Address, CreateCommand>();

        CreateMap<Contact, CreateCommand>();

        CreateMap<CreateItemDto, Item>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(_ => Guid.NewGuid()));
            }
}