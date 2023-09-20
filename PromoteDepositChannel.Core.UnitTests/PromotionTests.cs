using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.PromoteDepositChannel;

namespace PromoteDepositChannel.Core.UnitTests;

public class PromotionTests
{
	[Fact]
	public async Task 取得DistributePlanId()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new Promotion(
			Guid.NewGuid(),
			(DateTime.UtcNow, DateTime.UtcNow),
			new PromoteDepositChannelSettings(
				Array.Empty<int>(),
				Array.Empty<Guid>(),
				0M,
				0M,
				0M,
				"SportsBook"),
			new Dictionary<Guid, Guid>(),
			fakePromotionServiceClient);

		var expected = Guid.NewGuid();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Is(sut.PromotionId), Arg.Is(0L))
			.Returns(new PromotionDistributePlanInfo(
				expected,
				Guid.NewGuid(),
				sut.PromotionId,
				0L));

		// Act
		var actual = await sut.GetPromotionDistributePlanAsync(0L);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task 派發活動獎勵()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new Promotion(
			Guid.NewGuid(),
			(DateTime.UtcNow, DateTime.UtcNow),
			new PromoteDepositChannelSettings(
				Array.Empty<int>(),
				Array.Empty<Guid>(),
				0M,
				0M,
				0M,
				"SportsBook"),
			new Dictionary<Guid, Guid>(),
			fakePromotionServiceClient);

		var distributePlanId = Guid.NewGuid();
		var playerCode = "test1";

		// Act
		await sut.DistributePromotionBonusAsync(
			distributePlanId,
			playerCode,
			100M,
			1000M,
			Guid.NewGuid());

		// Assert
		_ = fakePromotionServiceClient.Received(1)
			.DistributePromotionBonusAsync(
				Arg.Is(distributePlanId),
				Arg.Is(playerCode),
				Arg.Is(100M),
				Arg.Is(1000M),
				Arg.Is(default(string)),
				Arg.Any<Guid>());
	}

	[Fact]
	public void 計算活動獎勵()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new Promotion(
			Guid.NewGuid(),
			(DateTime.UtcNow, DateTime.UtcNow),
			new PromoteDepositChannelSettings(
				Array.Empty<int>(),
				Array.Empty<Guid>(),
				10M,
				1000M,
				10M,
				"SportsBook"),
			new Dictionary<Guid, Guid>(),
			fakePromotionServiceClient);

		// Act
		var (actualBonus, actualBucketAmount) = sut.ComputeBonus(1000M);

		// Assert
		Assert.Equal(100M, actualBonus);
		Assert.Equal(11000M, actualBucketAmount);
	}

	[Fact]
	public void 計算活動獎勵_如果獎金超過限制金額以限制金額計算()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new Promotion(
			Guid.NewGuid(),
			(DateTime.UtcNow, DateTime.UtcNow),
			new PromoteDepositChannelSettings(
				Array.Empty<int>(),
				Array.Empty<Guid>(),
				10M,
				50M,
				10M,
				"SportsBook"),
			new Dictionary<Guid, Guid>(),
			fakePromotionServiceClient);

		// Act
		var (actualBonus, actualBucketAmount) = sut.ComputeBonus(1000M);

		// Assert
		Assert.Equal(50M, actualBonus);
		Assert.Equal(10500M, actualBucketAmount);
	}
}
