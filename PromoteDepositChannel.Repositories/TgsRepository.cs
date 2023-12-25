using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.PromoteDepositChannel.Repositories.Entities;

namespace Tgs.Promotion.PromoteDepositChannel.Repositories;

internal class TgsRepository(TgsContext dbContext) : ITgsRepository
{
	private readonly TgsContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

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
			CreatedOn = depositEvent.CreatedOn.ToUniversalTime(),
		}, cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(
			   cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<(decimal Amount, Guid? SnapshotId)> GetPromotionUsedAmountAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
	{
		var rows = await _dbContext.PromotionPromoteDepositChannels
			.Where(p => p.PromotionId == promotionId)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var aggregate = rows.OrderBy(row => row.Id)
			.Aggregate(
				new UsedAmountAccmulate
				{
					Amount = 0M,
					SnapshotId = default
				},
				(accmulate, row) =>
				{
					accmulate.Amount += row.Bonus;
					accmulate.SnapshotId = row.Id;

					return accmulate;
				});

		return aggregate is not null
			? (aggregate.Amount, aggregate.SnapshotId)
			: (0M, null);
	}

	public async ValueTask<bool> CheckDepositEventHasProcessedAsync(string playerCode, Guid planId, CancellationToken cancellationToken = default)
		=> await _dbContext.PromotionPromoteDepositChannels
			.AnyAsync(p => p.PlayerCode == playerCode
				&& p.DistributePlanId == planId, cancellationToken)
			.ConfigureAwait(false);

	private class UsedAmountAccmulate
	{
		public decimal Amount { get; set; }

		public Guid? SnapshotId { get; set; }
	}
}
