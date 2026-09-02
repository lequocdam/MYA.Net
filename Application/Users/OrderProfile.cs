using AutoMapper;

public class OrderProfile : Profile
{
    public Profile()
    {
        CreateMap<Order, OrderDto>();
        
        CreateMap<CreateOrderDto, Order>();

        CreateMap<Item, ItemDto>();

        CreateMap<CreateItemCommand, Item>();
    }
}