using Microsoft.Extensions.Hosting;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class PromotionBackground : BackgroundService
{
	private readonly EventBus _eventBus;
	private readonly BrandPromotionManager _brandPromotionManager;

	public PromotionBackground(
		EventBus eventBus,
		BrandPromotionManager brandPromotionManager)
	{
		_eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
		_brandPromotionManager = brandPromotionManager ?? throw new ArgumentNullException(nameof(brandPromotionManager));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
		=> Task.WhenAll(
			_eventBus.HandleEventLoopAsync(stoppingToken),
			_eventBus.HandleUpdateUsedAmountLoopAsync(stoppingToken),
			ScheduleRenewPromotionsAsync(stoppingToken),
			_brandPromotionManager.HandlePreviousPromotionsUsedAmountTriggersAsync(stoppingToken));

	private async Task ScheduleRenewPromotionsAsync(CancellationToken cancellationToken = default)
	{
		var timer = new PeriodicTimer(TimeSpan.FromMinutes(5D));

		do
		{
			await _brandPromotionManager.RenewAllBrandActivatedPromotionsAsync(
				cancellationToken).ConfigureAwait(false);
		} while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
	}
}
