namespace Tgs.Promotion.PromoteDepositChannel;
public record DepositEvent(
	Guid Id,
	string PlayerCode,
	Guid FormId,
	decimal Bonus,
	decimal BucketAmount,
	int VipLevel,
	Guid DistributePlanId,
	DateTime CreatedOn);
