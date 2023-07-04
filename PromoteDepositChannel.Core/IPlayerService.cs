namespace Tgs.Promotion.PromoteDepositChannel;

public interface IPlayerService
{
	ValueTask<DepositPlayer?> GetDepositPlayerAsync(string playerCode, CancellationToken cancellationToken = default);
}
