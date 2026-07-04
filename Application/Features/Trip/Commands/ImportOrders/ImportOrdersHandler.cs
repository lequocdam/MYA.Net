using ClosedXML.Excel;
using MediatR;
using Microsoft.Extensions.Logging;

public sealed class CreateOrdersFromExcelHandler(
    IServiceRepository serviceRepository,
    IProvinceRepository provinceRepository,
    IOrderCreator orderCreator,
    ILogger<CreateOrdersFromExcelHandler> logger)
    : IRequestHandler<CreateOrdersFromExcelCommand, OrderListDto>
{
    public async Task<OrderListDto> Handle(
        ImportCommand request,
        CancellationToken ct)
    {
        using var stream = request.File.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        var sheet = workbook.Worksheet("Orders")
            ?? throw new BadRequestException("Orders sheet not found");

        var rows = sheet.RowsUsed()
            .Skip(1)
            .Select(row => new ExcelRowDto(
                FromName: row.Cell(1).GetString().Trim(),
                FromPhone: row.Cell(2).GetString().Trim(),
                FromProvince: row.Cell(3).GetString().Trim(),
                FromDistrict: row.Cell(4).GetString().Trim(),
                FromWard: row.Cell(5).GetString().Trim(),
                FromStreet: row.Cell(6).GetString().Trim(),
                ToName: row.Cell(7).GetString().Trim(),
                ToPhone: row.Cell(8).GetString().Trim(),
                ToProvince: row.Cell(9).GetString().Trim(),
                ToDistrict: row.Cell(10).GetString().Trim(),
                ToWard: row.Cell(11).GetString().Trim(),
                ToStreet: row.Cell(12).GetString().Trim(),
                ServiceName: row.Cell(13).GetString().Trim(),
                ItemImage: row.Cell(14).GetString().Trim(),
                ItemName: row.Cell(15).GetString().Trim(),
                ItemWeight: row.Cell(16).GetValue<double>(),
                ItemQuantity: row.Cell(17).GetValue<int>(),
                ItemLength: row.Cell(18).GetValue<double>(),
                ItemWidth: row.Cell(19).GetValue<double>(),
                ItemHeight: row.Cell(20).GetValue<double>(),
                RowNumber: row.RowNumber()))
            .ToList();

        if (!rows.Any())
            throw new BadRequestException("File không có dữ liệu");

        var serviceNames = rows
            .Select(x => x.ServiceName)
            .Distinct()
            .ToList();

        var services = await serviceRepository.Query()
            .Where(x => serviceNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, ct);

        var provinceNames = rows
            .SelectMany(x => new[]
            {
                x.FromProvince,
                x.ToProvince
            })
            .Distinct()
            .ToList();

        var provinces = await provinceRepository.Query()
            .Where(x => provinceNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, ct);

        var groups = rows
            .GroupBy(x => new
            {
                x.FromPhone,
                x.ToPhone,
                x.ServiceName
            });

        var success = new List<OrderDto>();
        var errors = new List<BatchErrorDto>();

        foreach (var group in groups)
        {
            try
            {
                var first = group.First();

                if (!services.TryGetValue(first.ServiceName, out var service))
                    throw new BadRequestException($"Service '{first.ServiceName}' không tồn tại");

                if (!provinces.TryGetValue(first.FromProvince, out var fromProvince))
                    throw new BadRequestException($"Province '{first.FromProvince}' không tồn tại");

                if (!provinces.TryGetValue(first.ToProvince, out var toProvince))
                    throw new BadRequestException($"Province '{first.ToProvince}' không tồn tại");

                var from = new AddressRaw(
                    first.FromName,
                    first.FromPhone,
                    fromProvince.Code,
                    first.FromDistrict,
                    first.FromWard,
                    first.FromStreet);

                var to = new AddressRaw(
                    first.ToName,
                    first.ToPhone,
                    toProvince.Code,
                    first.ToDistrict,
                    first.ToWard,
                    first.ToStreet);

                var items = group
                    .Select(x => Item.Create(
                        x.ItemName,
                        x.ItemQuantity,
                        x.ItemWeight,
                        x.ItemLength,
                        x.ItemWidth,
                        x.ItemHeight))
                    .ToList();

                var orderId = await orderCreator.CreateAsync(
                    request.UserId,
                    service.Id,
                    from,
                    to,
                    items,
                    null,
                    ct);

                success.Add(new OrderDto
                {
                    Id = orderId
                });
            }
            catch (Exception ex)
            {
                errors.Add(new BatchErrorDto(
                    group.Select(x => x.RowNumber).ToList(),
                    ex.Message));

                logger.LogWarning(ex,
                    "Create order from excel failed. Rows={Rows}",
                    string.Join(",", group.Select(x => x.RowNumber)));
            }
        }

        return new OrderListDto(success, errors);
    }
}