using Tgs.Promotion.Clients;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class Promotion
{
	private readonly IReadOnlyDictionary<Guid, Guid> _brandPaymentDict;
	private readonly IPromotionServiceClient _promotionServiceClient;

	public Guid PromotionId { get; }

	public EventRange EventRange { get; }

	public PromoteDepositChannelSettings Settings { get; }

	public Promotion(
		Guid promotionId,
		EventRange eventRange,
		PromoteDepositChannelSettings settings,
		IReadOnlyDictionary<Guid, Guid> brandPaymentDict,
		IPromotionServiceClient promotionServiceClient)
	{
		PromotionId = promotionId;
		EventRange = eventRange;
		Settings = settings ?? throw new ArgumentNullException(nameof(settings));
		_brandPaymentDict = brandPaymentDict ?? throw new ArgumentNullException(nameof(brandPaymentDict));
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	}

	public (decimal Bonus, decimal BucketAmount) ComputeBonus(decimal depositAmount)
	{
		var bonus = Math.Floor(depositAmount * Settings.Percentage);
		if (bonus > Settings.BonusAmountLimit)
			bonus = Settings.BonusAmountLimit;

		var bucketAmount = (depositAmount + bonus) * Settings.TurnoverMultiple;

		return (bonus, bucketAmount);
	}

	public async ValueTask<Guid> GetPromotionDistributePlanAsync(
		long conditionHash,
		CancellationToken cancellationToken = default)
	{
		var plan = await _promotionServiceClient.GetPromotionDistributePlanAsync(
			PromotionId,
			conditionHash,
			cancellationToken).ConfigureAwait(false);

		return plan.Id;
	}

	public async ValueTask DistributePromotionBonusAsync(
		Guid planId,
		string playerCode,
		decimal bonus,
		decimal bucketAmount,
		Guid referenceId,
		CancellationToken cancellationToken = default)
		=> await _promotionServiceClient.DistributePromotionBonusAsync(
			planId,
			playerCode,
			bonus,
			bucketAmount,
			null,
			referenceId,
			cancellationToken).ConfigureAwait(false);

	public bool CheckBrandPaymentInSettings(Guid brandPaymentId)
		=> _brandPaymentDict.TryGetValue(brandPaymentId, out var channelId)
			&& Settings.ChannelIds.Contains(channelId);

	public bool CheckVipLevelInRange(int level)
		=> Settings.VipLevels.Contains(level);
}
