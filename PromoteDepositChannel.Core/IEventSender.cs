namespace Tgs.Promotion.PromoteDepositChannel;

public interface IEventSender
{
	ValueTask PushEventAsync(FormEvent formEvent, CancellationToken cancellationToken = default);

	ValueTask TriggerUpdateUsedAmountAsync(Guid promotionId);
}
