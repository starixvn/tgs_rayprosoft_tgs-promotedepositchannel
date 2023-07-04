namespace Tgs.Promotion.PromoteDepositChannel;

public interface ITgsRepository
{
	ValueTask SaveDepositEventAsync(DepositEvent depositEvent, CancellationToken cancellationToken = default);
}
