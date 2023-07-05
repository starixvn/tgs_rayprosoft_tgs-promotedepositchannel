namespace Tgs.Promotion.PromoteDepositChannel;

public interface ITgsRepository
{
	ValueTask SaveDepositEventAsync(DepositEvent depositEvent, CancellationToken cancellationToken = default);

	ValueTask<(decimal Amount, Guid? SnapshotId)> GetPromotionUsedAmountAsync(Guid promotionId, CancellationToken cancellationToken = default);
}
