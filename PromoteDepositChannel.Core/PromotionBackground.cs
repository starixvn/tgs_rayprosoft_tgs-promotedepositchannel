using Microsoft.Extensions.Hosting;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class PromotionBackground : BackgroundService
{
	private readonly EventBus _eventBus;

	public PromotionBackground(EventBus eventBus)
	{
		_eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
		=> Task.WhenAll(
			_eventBus.HandleEventLoopAsync(stoppingToken),
			_eventBus.HandleUpdateUsedAmountLoopAsync(stoppingToken));
}
