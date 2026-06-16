public class Order
{
    public Guid            Id                  { get; private set; }
    public string          Code                { get; private set; }
    public Guid            UserId              { get; private set; }
    public Guid            ServiceId           { get; private set; }
    public Guid            WarehouseId         { get; private set; }
    public AddressSnapshot FromAddressSnapshot { get; private set; }
    public AddressSnapshot ToAddressSnapshot   { get; private set; }
    public decimal         Cost                { get; private set; }
    public decimal         Fee                 { get; private set; }
    public decimal         Total               { get; private set; }
    public decimal?        CodAmount           { get; private set; }
    public OrderStatus     Status              { get; private set; }
    public DateTime        Date                { get; private set; }
    public string?         Note                { get; private set; }
    public List<Item>      Items               { get; private set; } = new();

    // Factory method — không dùng constructor public
    public static Order Create(
        Guid            userId,
        Guid            serviceId,
        Guid            warehouseId,
        AddressSnapshot fromSnapshot,
        AddressSnapshot toSnapshot,
        decimal         cost,
        decimal         fee,
        decimal         total,
        decimal?        codAmount,
        List<Item>      items)
    {
        return new Order
        {
            Id                  = Guid.NewGuid(),
            Code                = GenerateCode(),
            UserId              = userId,
            ServiceId           = serviceId,
            WarehouseId         = warehouseId,
            FromAddressSnapshot = fromSnapshot,
            ToAddressSnapshot   = toSnapshot,
            Cost                = cost,
            Fee                 = fee,
            Total               = total,
            CodAmount           = codAmount,
            Status              = OrderStatus.Pending,
            Date                = DateTime.UtcNow,
            Items               = items,
        };
    }

    public void Update(
        AddressSnapshot fromAddressSnapshot,
        AddressSnapshot toAddressSnapshot,
        Guid            serviceId,
        decimal         cost,
        decimal         fee,
        decimal         total)
    {
        FromAddressSnapshot = fromAddressSnapshot;
        ToAddressSnapshot   = toSntoAddressSnapshotapshot;
        ServiceId           = serviceId;
        Cost                = cost;
        Fee                 = fee;
        Total               = total;
    }

    public void UpdateItems(List<UpdateItemDto> updatedItems)
    {
        Items.RemoveAll(i => updatedItems.All(ui => ui.Id != i.Id));

        foreach (var updateItem in updatedItems)
        {
            var existedItem = Items.FirstOrDefault(i => i.Id == updateItem.Id);

            if (existedItem is null)
            {
                Items.Add(new Item
                {
                    Id       = Guid.NewGuid(),
                    Name     = updateItem.Name,
                    Quantity = updateItem.Quantity,
                    Weight   = updateItem.Weight,
                    Length   = updateItem.Length,
                    Width    = updateItem.Width,
                    Height   = updateItem.Height,
                });
            }
            else
            {
                existedItem.Name     = updateItem.Name;
                existedItem.Quantity = updateItem.Quantity;
                existedItem.Weight   = updateItem.Weight;
                existedItem.Length   = updateItem.Length;
                existedItem.Width    = updateItem.Width;
                existedItem.Height   = updateItem.Height;
            }
        }
    }

    private static string GenerateCode() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
}