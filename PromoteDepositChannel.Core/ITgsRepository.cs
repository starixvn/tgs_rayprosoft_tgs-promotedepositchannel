namespace Tgs.Promotion.PromoteDepositChannel;

public interface ITgsRepository
{
	ValueTask<bool> CheckDepositEventHasProcessedAsync(string playerCode, Guid planId, CancellationToken cancellationToken = default);

	ValueTask SaveDepositEventAsync(DepositEvent depositEvent, CancellationToken cancellationToken = default);

	ValueTask<(decimal Amount, Guid? SnapshotId)> GetPromotionUsedAmountAsync(Guid promotionId, CancellationToken cancellationToken = default);
}
