using MediatR;
using Microsoft.AspNetCore.Http;

public record ImportOrdersCommand(
    IFormFile File,
    Guid UserId) : IRequest<BatchResultDTO>;