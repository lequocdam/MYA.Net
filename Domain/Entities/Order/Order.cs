public class Order
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public DateTime Date { get; set; }
    public OrderStatus Status { get; set; }
    public decimal? Cod { get; set; }
    public decimal Cost { get; set; }
    public decimal Fee { get; set; }
    public decimal Total { get; set; }
    public Guid ServiceId { get; set; }
    public Guid UserId { get; set; }
    public Guid WarehouseId { get; set; }
    public List<Item> Items { get; set; } = new();

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