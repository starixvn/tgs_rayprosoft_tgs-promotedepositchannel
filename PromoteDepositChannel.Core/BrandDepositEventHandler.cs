using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class BrandDepositEventHandler
{
	private static readonly ActivitySource _ActivitySource = new($"{typeof(BrandDepositEventHandler).Namespace}.{nameof(BrandDepositEventHandler)}");

	private readonly IEnumerable<DepositEventHandler> _eventHandlers;
	private readonly ILogger<BrandDepositEventHandler> _logger;

	public Guid BrandId { get; }

	public BrandDepositEventHandler(
		Guid brandId,
		IEnumerable<DepositEventHandler> eventHandlers,
		ILogger<BrandDepositEventHandler> logger)
	{
		BrandId = brandId;
		_eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask OnDepositedAsync(
		DepositFormInfo depositForm,
		CancellationToken cancellationToken = default)
	{
		using var activity = _ActivitySource.StartActivity($"{nameof(BrandDepositEventHandler)}.{nameof(OnDepositedAsync)}");

		foreach (var handler in _eventHandlers)
			try
			{
				if (handler.EventRange.InTheRange(depositForm.CreatedOn.UtcDateTime))
					await handler.OnDepositedAsync(
						depositForm,
						cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Brand({brandId}) Handle event occur error.", BrandId);
			}
	}

	public async ValueTask InitPromotionsUsedAmountAsync(CancellationToken cancellationToken = default)
	{
		using var activity = _ActivitySource.StartActivity($"{nameof(BrandDepositEventHandler)}.{nameof(InitPromotionsUsedAmountAsync)}");

		foreach (var handler in _eventHandlers)
			await handler.InitPromotionUsedAmountAsync(
				cancellationToken).ConfigureAwait(false);
	}
}
