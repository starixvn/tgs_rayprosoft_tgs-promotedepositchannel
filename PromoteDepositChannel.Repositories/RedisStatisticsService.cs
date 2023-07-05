using StackExchange.Redis;

namespace Tgs.Promotion.PromoteDepositChannel.Repositories;

internal class RedisStatisticsService : IStatisticsService
{
	private static readonly RedisKey _KeyPrefix = new("Tgs:Promotion:PromoteDepositChannel");

	private readonly IDatabase _redisDatabase;

	public RedisStatisticsService(IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask IncrementPromotionUsedAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default)
	{
		if (amount < 0M)
			throw new ArgumentException($"{nameof(amount)} needs to be positive.");

		var key = _KeyPrefix.Append($":UsedAmount:{promotionId}");

		_ = await _redisDatabase.StringIncrementAsync(
			key,
			(double)amount).ConfigureAwait(false);
	}

	public async ValueTask DecrementPromotionUsedAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default)
	{
		if (amount < 0M)
			throw new ArgumentException($"{nameof(amount)} needs to be positive.");

		var key = _KeyPrefix.Append($":UsedAmount:{promotionId}");

		await _redisDatabase.StringDecrementAsync(
			key,
			(double)amount).ConfigureAwait(false);
	}

	public async ValueTask<decimal?> GetPromotionUsedAmountAsync(Guid promotionId, CancellationToken cancellationToken = default)
	{
		var key = _KeyPrefix.Append($":UsedAmount:{promotionId}");

		var amount = await _redisDatabase.StringGetAsync(key).ConfigureAwait(false);

		return amount.HasValue ? (decimal)(double)amount : null;
	}

	public async ValueTask InitPromotionUsedAmountAsync(
		Guid promotionId,
		decimal amount,
		DateTime expireTime,
		CancellationToken cancellationToken = default)
	{
		var key = _KeyPrefix.Append($":UsedAmount:{promotionId}");

		await _redisDatabase.StringSetAsync(
			key,
			(double)amount,
			expiry: expireTime - DateTime.Now,
			when: When.NotExists).ConfigureAwait(false);
	}
}
