using AutoMapper;

public class AddressProfile : Profile
{
    public AddressProfile()
    {
        CreateMap<Address, AddressDto>();
        

        CreateMap<AddressDto, Address>();

        CreateMap<Item, ItemDto>();

        CreateMap<CreateItemDto, Item>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(_ => Guid.NewGuid()));
            }
}