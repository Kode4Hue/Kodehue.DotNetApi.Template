using Application.Common.Outbox.DTOs;
using Application.Common.Outbox.Services;
using Application.Common.Results;
using Domain.Common.Outbox;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Common.Outbox.Services
{
    internal class OutboxService : IOutboxService
    {
        private readonly UserProfileDbContext dbContext;

        public OutboxService(UserProfileDbContext dbContext)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Result<OutboxMessageCreatedResponse>> AddEventAsync(
            NewOutboxMessage outboxMessage,
            CancellationToken cancellationToken = default)
        {
            if (outboxMessage is null)
            {
                var err = ErrorDto.CreateFromMessage("outboxMessage is null");
                return Result<OutboxMessageCreatedResponse>.CreateErrorResult(new[] { err }, ErrorType.ValidationError);
            }

            try
            {
                var entity = new OutboxEntity
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = outboxMessage.CreatedAtUtc,
                    LastUpdatedAtUtc = outboxMessage.CreatedAtUtc,
                    ExpiresAtUtc = outboxMessage.CreatedAtUtc.AddDays(1),
                    Type = outboxMessage.Type ?? string.Empty,
                    Payload = outboxMessage.Payload ?? string.Empty,
                    Status = "pending",
                    RetryCount = 0,
                    CorrelationId = !string.IsNullOrWhiteSpace(outboxMessage.CorrelationId) ?
                        Guid.Parse(outboxMessage.CorrelationId) : null,
                };

                dbContext.OutboxMessages.Add(entity);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                var response = new OutboxMessageCreatedResponse
                {
                    Id = entity.Id.ToString(),
                    CorrelationId = Guid.NewGuid().ToString()
                };

                return Result<OutboxMessageCreatedResponse>.CreateSuccessResult(response);
            }
            catch (Exception ex)
            {
                var error = ErrorDto.CreateFromMessage(ex.Message);
                return Result<OutboxMessageCreatedResponse>.CreateErrorResult(new[] { error }, ErrorType.ApplicationError);
            }
        }

        public async Task<NoContentResult> ClearPendingEventsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dispatched = await dbContext.OutboxMessages
                    .Where(x => x.Status.Equals("dispatched"))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (dispatched.Count > 0)
                {
                    dbContext.OutboxMessages.RemoveRange(dispatched);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }

                return NoContentResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                var error = ErrorDto.CreateFromMessage(ex.Message);
                return NoContentResult.CreateErrorResult(new List<ErrorDto> { error }, ErrorType.ApplicationError);
            }
        }

        public async Task<NoContentResult> DeleteEventAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(id, out var guid))
            {
                var err = ErrorDto.CreateFromMessage("Invalid id format");
                return NoContentResult.CreateErrorResult(new List<ErrorDto> { err }, ErrorType.ValidationError);
            }

            try
            {
                var entity = await dbContext.OutboxMessages.FindAsync(new object[] { guid }, cancellationToken).ConfigureAwait(false);
                if (entity == null)
                {
                    return NoContentResult.CreateSuccess();
                }

                dbContext.OutboxMessages.Remove(entity);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                return NoContentResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                var error = ErrorDto.CreateFromMessage(ex.Message);
                return NoContentResult.CreateErrorResult(new List<ErrorDto> { error }, ErrorType.ApplicationError);
            }
        }

        public async Task<OutboxMessage?> GetEventByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var entity = await dbContext.OutboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == guid, cancellationToken)
                .ConfigureAwait(false);

            if (entity == null)
                return null;

            return MapToDto(entity);
        }

        public async Task<List<OutboxMessage>> GetPendingEventsAsync(
            CancellationToken cancellationToken = default)
        {
            var entities = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(x => x.Status.Equals("pending"))
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return entities.Select(MapToDto).ToList();
        }

        public async Task<Result<OutboxMessage>> UpdateEventAsync(
            OutboxMessage outboxMessage,
            CancellationToken cancellationToken = default)
        {
            if (outboxMessage is null)
            {
                var err = ErrorDto.CreateFromMessage("outboxMessage is null");
                return Result<OutboxMessage>.CreateErrorResult(new[] { err }, ErrorType.ValidationError);
            }

            if (!Guid.TryParse(outboxMessage.Id, out var guid))
            {
                var err = ErrorDto.CreateFromMessage("Invalid outbox message id");
                return Result<OutboxMessage>.CreateErrorResult(new[] { err }, ErrorType.ValidationError);
            }

            try
            {
                var entity = await dbContext.OutboxMessages
                    .FirstOrDefaultAsync(x => x.Id == guid, cancellationToken)
                    .ConfigureAwait(false);

                if (entity == null)
                {
                    var err = ErrorDto.CreateFromMessage($"Outbox message '{outboxMessage.Id}' not found.");
                    return Result<OutboxMessage>.CreateErrorResult(new[] { err }, ErrorType.NotFoundError);
                }

                if (!string.IsNullOrWhiteSpace(outboxMessage.Type))
                    entity.Type = outboxMessage.Type;

                if (!string.IsNullOrWhiteSpace(outboxMessage.Payload))
                    entity.Payload = outboxMessage.Payload;

                entity.RetryCount = outboxMessage.RetryCount;

                if (outboxMessage.ProcessedOnUtc.HasValue)
                    entity.Status = "dispatched";

                dbContext.OutboxMessages.Update(entity);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                var updatedDto = MapToDto(entity);
                // copy over DTO fields that are not stored on entity
                updatedDto.CorrelationId = outboxMessage.CorrelationId ?? string.Empty;
                updatedDto.Error = outboxMessage.Error;
                updatedDto.LastUpdatedUtc = DateTime.UtcNow;
                updatedDto.ProcessedOnUtc = outboxMessage.ProcessedOnUtc;

                return Result<OutboxMessage>.CreateSuccessResult(updatedDto);
            }
            catch (Exception ex)
            {
                var error = ErrorDto.CreateFromMessage(ex.Message);
                return Result<OutboxMessage>.CreateErrorResult(new[] { error }, ErrorType.ApplicationError);
            }
        }

        private static OutboxMessage MapToDto(OutboxEntity e)
        {
            return new OutboxMessage
            {
                Id = e.Id.ToString(),
                Type = e.Type,
                Status = e.Status,
                Payload = e.Payload,
                RetryCount = e.RetryCount,
                CorrelationId = string.Empty,
                Error = null,
                CreatedOnUtc = e.CreatedAtUtc,
                LastUpdatedUtc = DateTime.UtcNow,
                ProcessedOnUtc = null,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(15)
            };
        }
    }
}
