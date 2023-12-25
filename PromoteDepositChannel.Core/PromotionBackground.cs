using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class PromotionBackground(
	EventBus eventBus,
	BrandPromotionManager brandPromotionManager,
	ILogger<PromotionBackground> logger) : BackgroundService
{
	private readonly EventBus _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
	private readonly BrandPromotionManager _brandPromotionManager = brandPromotionManager ?? throw new ArgumentNullException(nameof(brandPromotionManager));

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
			try
			{
				await _brandPromotionManager.RenewAllBrandActivatedPromotionsAsync(
					cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Renew activated promotions occur error.");
			}
		} while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
	}
}
