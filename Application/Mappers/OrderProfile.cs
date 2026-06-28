using AutoMapper;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        

        CreateMap<CreateOrderDto, Order>();

        CreateMap<Item, ItemDto>();

        CreateMap<CreateItemDto, Item>()
            .ForMember(
                dest => dest.Id,
                opt => opt.MapFrom(_ => Guid.NewGuid()));
    }
}