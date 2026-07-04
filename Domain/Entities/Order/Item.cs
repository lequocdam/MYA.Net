public class Item
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public int Quantity { get; private set; }

    public decimal Weight { get; private set; }

    public decimal Length { get; private set; }

    public decimal Width { get; private set; }

    public decimal Height { get; private set; }

    public static Item Create(ItemData data)
    {
        return new Item
        {
            Id = Guid.NewGuid(),
            Name = data.Name,
            Quantity = data.Quantity,
            Weight = data.Weight,
            Length = data.Length,
            Width = data.Width,
            Height = data.Height
        };
    }
}