namespace Tgs.Promotion.PromoteDepositChannel;
public record PromoteDepositChannelSettings(
	IEnumerable<int> VipLevels,
	IEnumerable<Guid> ChannelIds,
	decimal Percentage,
	decimal BonusAmountLimit,
	decimal TurnoverMultiple,
	IEnumerable<string>? BucketTypes);
