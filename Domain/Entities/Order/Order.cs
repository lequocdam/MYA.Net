public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string Code { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid FromAddressId { get; private set; }
    public Guid ToAddressId { get; private set; }
    public DateTime Date { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Cost { get; private set; }
    public decimal Fee { get; private set; }
    public decimal Cod { get; private set; }
    public decimal Total { get; private set; }
    public List<Item> Items { get; private set; }

    public static Order Create(
        Guid serviceId,
        Guid userId,
        Guid warehouseId,
        Quote quote,
        List<Item> items)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Code = GenerateCode(),
            Date = DateTime.UtcNow,
            Status = OrderStatus.PENDING,
            Cost = quote.Cost,
            Fee = quote.Fee,
            Cod = quote.Cod,
            Total = quote.Total,
            ServiceId = serviceId,
            FromAddressId
            Items = items,
            UserId = userId,
            WarehouseId = warehouseId,
        };
    }

    public void Update(
        Guid fromAddressId,
        Guid toAddressId,
        AddressSnapshot fromAddressSnapshot,
        AddressSnapshot toAddressSnapshot,
        Guid serviceId,
        Guid userId,
        decimal cost,
        decimal fee,
        decimal total)
    {
        FromAddressId = fromAddressId,
        ToAddressId = toAddressId,
        FromAddressSnapshot = fromAddressSnapshot;
        ToAddressSnapshot = toSntoAddressSnapshotapshot;
        ServiceId = serviceId;
        UserId = userId,
        Cost = cost;
        Fee = fee;
        Total = total;
    }

    public void UpdateItems(List<UpdateItemDto> items)
    {
        Items.RemoveAll(i => items.All(ui => ui.Id != i.Id));

        foreach (var item in items)
        {
            var existedItem = Items.FirstOrDefault(i => i.Id == item.Id);

            if (existedItem is null)
            {
                Items.Add(new Item
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    Quantity = item.Quantity,
                    Weight = item.Weight,
                    Length = item.Length,
                    Width = item.Width,
                    Height = item.Height,
                });
            }
            else
            {
                existedItem.Name = item.Name;
                existedItem.Quantity = item.Quantity;
                existedItem.Weight = item.Weight;
                existedItem.Length = item.Length;
                existedItem.Width = item.Width;
                existedItem.Height = item.Height;
            }
        }
    }
}