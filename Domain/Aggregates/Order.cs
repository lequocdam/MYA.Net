public sealed class OrderAggregate : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public Guid ServiceId { get; private set; }

    public string Code { get; private set; };

    public decimal CodAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime Date { get; private set; }

    public Address From { get; private set; }

    public Address To { get; private set; }

    public Price Price { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order(){}

    public static Order Create(
        Guid userId,
        Guid warehouseId,
        Guid serviceId,
        Guid fromId,
        Guid toId,
        Address from,
        Address to,
        decimal codAmount,
        Price price,
        IEnumerable<OrderItem> items)
    {
        OrderPolicy.ValidateCreate(items);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WarehouseId = warehouseId,
            ServiceId = serviceId,
            FromId = fromId,
            ToId = toId,
            From = From,
            To = To,
            Code = GenerateCode(),
            CodAmount = codAmount,
            Status = OrderStatus.PENDING,
            Date = DateTime.UtcNow,
            Price = price,
        };

        order._items.AddRange(items);

        order.AddDomainEvent(
            new OrderCreatedDomainEvent(order.Id));

        return order;
    }

    public void Update(Order order)
    {
        if (ServiceId == serviceId) return;

        OrderPolicy.ValidateUpdate(this.Status);

        ServiceId = serviceId;
        
        
        Price = await quoteService.GetAsync(serviceId);
        
        AddDomainEvent(new OrderServiceChangedDomainEvent(Id, ServiceId, Price));
    }

    public void ChangeCodAmount(decimal codAmount, IQuoteService quoteService)
    {
        OrderPolicy.ValidateUpdate(this.Status, this.CodAmount);

        CodAmount = codAmount;

        Price = await quoteService.GetAsync(codAmount);

        AddDomainEvent(new OrderCodAmoundChangedDomainEvent(Id, CodAmount, Price));
    }

    public void UpdateItems(IEnumerable<OrderItem> items, IQuoteService quoteService)
    {
        OrderPolicy.ValidateUpdate(this.Status, this.CodAmount);

        OrderPolicy.ValidateCreate(items);

        _items.Clear();
        _items.AddRange(items);

        // Hàng hóa đổi (khối lượng đổi) -> Tính lại giá tiền mới
        Price = await quoteService.GetAsync(items);

        AddDomainEvent(new OrderItemsUpdatedDomainEvent(Id, _items));
    }
}