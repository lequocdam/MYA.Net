public class CreateOrdersDto
{
    public string FromAddressName { get; set; };
    public string FromAddressPhone { get; set; };
    public string FromAddressEmail { get; set; };
    public string FromAddressAddress { get; set; };

    public string ToAddressName { get; set; };
    public string ToAddressPhone { get; set; };
    public string ToAddressEmail { get; set; };
    public string ToAddressAddress { get; set; };

    public string Service { get; set; };
    public string Warehouse { get; set; };

    public string ItemImage { get; set; };
    public string ItemName { get; set; };
    public double ItemWeight { get; set; }
    public int    ItemQuantity { get; set; }
    public double ItemLength { get; set; }
    public double ItemWidth { get; set; }
    public double ItemHeight { get; set; }

    public int    RowNumber { get; set; }
}