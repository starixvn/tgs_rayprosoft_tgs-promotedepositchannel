using Tgs.Promotion.PromoteDepositChannel.Repositories.Entities;

namespace Tgs.Promotion.PromoteDepositChannel.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _dbContext;

	public TgsRepository(TgsContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public async ValueTask SaveDepositEventAsync(DepositEvent depositEvent, CancellationToken cancellationToken = default)
	{
		_ = await _dbContext.PromotionPromoteDepositChannels.AddAsync(new PromotionPromoteDepositChannel
		{
			Id = depositEvent.Id,
			PlayerCode = depositEvent.PlayerCode,
			Bonus = depositEvent.Bonus,
			BucketAmount = depositEvent.BucketAmount,
			FormId = depositEvent.FormId,
			VipLevel = depositEvent.VipLevel,
			PromotionId = depositEvent.PromotionId,
			DistributePlanId = depositEvent.DistributePlanId,
			CreatedOn = depositEvent.CreatedOn,
		}, cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(
			   cancellationToken).ConfigureAwait(false);
	}
}
