public class Order
{
    private Guid            Id                  { get; private set; }
    private string          Code                { get; private set; }
    private Guid            UserId              { get; private set; }
    private Guid            ServiceId           { get; private set; }
    private Guid            WarehouseId         { get; private set; }
    private AddressSnapshot FromAddressSnapshot { get; private set; }
    private AddressSnapshot ToAddressSnapshot   { get; private set; }
    private decimal         Cost                { get; private set; }
    private decimal         Fee                 { get; private set; }
    private decimal         Total               { get; private set; }
    private decimal?        CodAmount           { get; private set; }
    private OrderStatus     Status              { get; private set; }
    private DateTime        Date                { get; private set; }
    private string?         Note                { get; private set; }
    private List<Item>      Items               { get; private set; } = new();

    public static Order Create(
        string              code,
        AddressSnapshot     fromAddressSnapshot,
        AddressSnapshot     toAddressSnapshot,
        Guid                serviceId,
        Guid                warehouseId,
        Guid                userId,
        List<CreateItemDto> items
        decimal             cost,
        decimal             fee,
        decimal             codFee,
        decimal             total)
    {
        return new Order
        {
            Id                  = Guid.NewGuid(),
            Code                = code,
            Status              = OrderStatus.PENDING,
            Date                = DateTime.UtcNow,
            FromAddressSnapshot = fromAddressSnapshot,
            ToAddressSnapshot   = toAddressSnapshot,
            ServiceId           = serviceId,
            WarehouseId         = warehouseId,
            UserId              = userId,
            Items               = items
            .Select(i => new Item
            {
                Id       = Guid.NewGuid(),
                Name     = i.Name,
                Quantity = i.Quantity,
                Weight   = i.Weight,
                Length   = i.Length,
                Width    = i.Width,
                Height   = i.Height
            }).ToList();
            Cost                = cost,
            Fee                 = fee,
            Cod                 = cod,
            Total               = total,
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