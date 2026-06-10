using ClosedXML.Excel;
using MediatR;
using Microsoft.Extensions.Logging;

public class ImportOrdersHandler(
    IMediator mediator,
    ILogger<ImportOrdersHandler> logger)
    : IRequestHandler<ImportOrdersCommand, BatchResultDTO>
{
    public async Task<BatchResultDTO> Handle(
        ImportOrdersCommand request,
        CancellationToken ct)
    {
        using var stream = request.File.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet("Orders")
            ?? throw new BadRequestException(
                "Sheet 'Orders' không tồn tại trong file Excel");

        var rawRows = sheet.RowsUsed()
            .Skip(1)
            .Select(row => new
            {
                SenderName = row.Cell(1).GetString().Trim(),
                SenderPhone = row.Cell(2).GetString().Trim(),
                SenderAddress = row.Cell(3).GetString().Trim(),

                ReceiverName = row.Cell(4).GetString().Trim(),
                ReceiverPhone = row.Cell(5).GetString().Trim(),
                ReceiverAddress = row.Cell(6).GetString().Trim(),

                Category = row.Cell(7).GetString().Trim(),

                ItemName = row.Cell(8).GetString().Trim(),
                ItemWeight = row.Cell(9).GetValue<decimal>(),
                ItemQty = row.Cell(10).GetValue<int>(),

                ItemLength = row.Cell(11).GetValue<decimal>(),
                ItemWidth = row.Cell(12).GetValue<decimal>(),
                ItemHeight = row.Cell(13).GetValue<decimal>(),

                RowNumber = row.RowNumber()
            })
            .ToList();

        var groups = rawRows
            .GroupBy(x => new
            {
                x.SenderPhone,
                x.ReceiverPhone,
                x.Category
            });

        var created = new List<OrderDto>();
        var errors = new List<BatchErrorDTO>();

        foreach (var group in groups)
        {
            var first = group.First();

            try
            {
                var dto = new CreateOrderDto
                {
                    Sender = new AddressInputDTO
                    {
                        Name = first.SenderName,
                        Phone = first.SenderPhone,
                        Address = first.SenderAddress
                    },

                    Receiver = new AddressInputDTO
                    {
                        Name = first.ReceiverName,
                        Phone = first.ReceiverPhone,
                        Address = first.ReceiverAddress
                    },

                    Category = first.Category,

                    Items = group.Select(x => new ItemInputDTO
                    {
                        Name = x.ItemName,
                        Weight = x.ItemWeight,
                        Quantity = x.ItemQty,

                        Length = x.ItemLength,
                        Width = x.ItemWidth,
                        Height = x.ItemHeight
                    }).ToList()
                };

                var order =
                    await mediator.Send(
                        new CreateOrderCommand(
                            dto,
                            request.UserId),
                        ct);

                created.Add(order);
            }
            catch (Exception ex)
            {
                errors.Add(new BatchErrorDTO
                {
                    Rows = group
                        .Select(x => x.RowNumber)
                        .ToList(),

                    Reason = ex.Message
                });

                logger.LogWarning(
                    ex,
                    "Import order failed. Rows={Rows}",
                    string.Join(",",
                        group.Select(x => x.RowNumber)));
            }
        }

        return new BatchResultDTO
        {
            Created = created,
            Errors = errors
        };
    }
}