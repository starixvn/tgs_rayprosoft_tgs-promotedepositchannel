namespace Tgs.Promotion.PromoteDepositChannel;

public interface IStatisticsService
{
	ValueTask IncrementPromotionUsedAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);

	ValueTask DecrementPromotionUsedAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<decimal> GetPromotionUsedAmountAsync(Guid promotionId, CancellationToken cancellationToken = default);
}
