using ExpectedObjects;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Promotion.Clients;
using Tgs.Promotion.PromoteDepositChannel;
using Tgs.SerialNumberGenerator;

namespace PromoteDepositChannel.Core.UnitTests;

public class DepositEventHandlerTests
{
	[Fact]
	public async Task 處理儲值事件()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerService = Substitute.For<IPlayerService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var brandPaymentId = Guid.NewGuid();
		var channelId = Guid.NewGuid();

		var promotion = new Promotion(
			Guid.NewGuid(),
			(DateTime.Now, DateTime.Now),
			new PromoteDepositChannelSettings(
				new[] { 1 },
				new[] { channelId },
				.1M,
				1000M,
				10M),
			new Dictionary<Guid, Guid>
			{
				[brandPaymentId] = channelId
			},
			fakePromotionServiceClient);
		var sut = new DepositEventHandler(
			Guid.NewGuid(),
			promotion,
			fakePlayerService,
			fakeTgsRepository,
			fakeSerialNumberGenerator,
			fakeStatisticsService);

		var depositForm = new DepositFormInfo
		{
			Id = Guid.NewGuid(),
			ActualAmount = 1000M,
			Amount = 1500M,
			PlayerCode = "test1",
			BrandPaymentId = brandPaymentId
		};

		_ = fakePlayerService.GetDepositPlayerAsync(Arg.Is(depositForm.PlayerCode))
			.Returns(new DepositPlayer(depositForm.PlayerCode, 1));

		var planId = Guid.NewGuid();
		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(
			Arg.Is(promotion.PromotionId),
			Arg.Any<long>())
			.Returns(new PromotionDistributePlanInfo(
				planId,
				Guid.NewGuid(),
				promotion.PromotionId,
				0L));

		var eventId = Guid.NewGuid();
		fakeSerialNumberGenerator.GenerateAsync()
			.Returns(eventId);

		var expectedDepositEvent = new DepositEvent(
			eventId,
			depositForm.PlayerCode,
			depositForm.Id,
			100M,
			11000M,
			1,
			planId,
			promotion.PromotionId,
			DateTime.Now).ToExpectedObject(ctx =>
			{
				_ = ctx.IgnoreEqualsOverride();
				ctx.Member(d => d.CreatedOn)
					.UsesComparison(Expect.Ignored());
			});

		// Act
		await sut.OnDepositedAsync(depositForm);

		// Assert
		_ = fakeStatisticsService.Received(1)
			.IncrementPromotionUsedAmountAsync(Arg.Is(promotion.PromotionId), Arg.Is(100M));
		_ = fakeTgsRepository.Received(1)
			.SaveDepositEventAsync(Arg.Is<DepositEvent>(data => expectedDepositEvent.Matches(data)));
		_ = fakePromotionServiceClient.Received(1)
			.DistributePromotionBonusAsync(
				Arg.Is(planId),
				Arg.Is(depositForm.PlayerCode),
				Arg.Is(100M),
				Arg.Is(11000M),
				Arg.Is(default(string)),
				Arg.Is(eventId));
	}

	[Fact]
	public async Task 處理儲值事件_如果存款用的Channel不在清單內不處理()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerService = Substitute.For<IPlayerService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var brandPaymentId = Guid.NewGuid();
		var channelId = Guid.NewGuid();

		var promotion = new Promotion(
			Guid.NewGuid(),
			(DateTime.Now, DateTime.Now),
			new PromoteDepositChannelSettings(
				new[] { 1 },
				new[] { channelId },
				.1M,
				1000M,
				10M),
			new Dictionary<Guid, Guid>
			{
				[brandPaymentId] = channelId
			},
			fakePromotionServiceClient);
		var sut = new DepositEventHandler(
			Guid.NewGuid(),
			promotion,
			fakePlayerService,
			fakeTgsRepository,
			fakeSerialNumberGenerator,
			fakeStatisticsService);

		var depositForm = new DepositFormInfo
		{
			Id = Guid.NewGuid(),
			ActualAmount = 1000M,
			Amount = 1500M,
			PlayerCode = "test1",
			BrandPaymentId = Guid.NewGuid()
		};

		// Act
		await sut.OnDepositedAsync(depositForm);

		// Assert
		_ = fakeStatisticsService.Received(0)
			.IncrementPromotionUsedAmountAsync(Arg.Any<Guid>(), Arg.Any<decimal>());
		_ = fakeTgsRepository.Received(0)
			.SaveDepositEventAsync(Arg.Any<DepositEvent>());
		_ = fakePromotionServiceClient.Received(0)
			.DistributePromotionBonusAsync(
				Arg.Any<Guid>(),
				Arg.Is(depositForm.PlayerCode),
				Arg.Is(100M),
				Arg.Is(11000M),
				Arg.Is(default(string)),
				Arg.Any<Guid?>());
	}

	[Fact]
	public async Task 處理儲值事件_如果玩家VipLevel不符合條件不處理()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerService = Substitute.For<IPlayerService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var brandPaymentId = Guid.NewGuid();
		var channelId = Guid.NewGuid();

		var promotion = new Promotion(
			Guid.NewGuid(),
			(DateTime.Now, DateTime.Now),
			new PromoteDepositChannelSettings(
				new[] { 2 },
				new[] { channelId },
				.1M,
				1000M,
				10M),
			new Dictionary<Guid, Guid>
			{
				[brandPaymentId] = channelId
			},
			fakePromotionServiceClient);
		var sut = new DepositEventHandler(
			Guid.NewGuid(),
			promotion,
			fakePlayerService,
			fakeTgsRepository,
			fakeSerialNumberGenerator,
			fakeStatisticsService);

		var depositForm = new DepositFormInfo
		{
			Id = Guid.NewGuid(),
			ActualAmount = 1000M,
			Amount = 1500M,
			PlayerCode = "test1",
			BrandPaymentId = brandPaymentId
		};

		_ = fakePlayerService.GetDepositPlayerAsync(Arg.Is(depositForm.PlayerCode))
			.Returns(new DepositPlayer(depositForm.PlayerCode, 1));

		// Act
		await sut.OnDepositedAsync(depositForm);

		// Assert
		_ = fakeStatisticsService.Received(0)
			.IncrementPromotionUsedAmountAsync(Arg.Any<Guid>(), Arg.Any<decimal>());
		_ = fakeTgsRepository.Received(0)
			.SaveDepositEventAsync(Arg.Any<DepositEvent>());
		_ = fakePromotionServiceClient.Received(0)
			.DistributePromotionBonusAsync(
				Arg.Any<Guid>(),
				Arg.Is(depositForm.PlayerCode),
				Arg.Is(100M),
				Arg.Is(11000M),
				Arg.Is(default(string)),
				Arg.Any<Guid?>());
	}

	[Fact]
	public async Task 處理儲值事件_如果事件已經處理過就不再處理()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerService = Substitute.For<IPlayerService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var brandPaymentId = Guid.NewGuid();
		var channelId = Guid.NewGuid();

		var promotion = new Promotion(
			Guid.NewGuid(),
			(DateTime.Now, DateTime.Now),
			new PromoteDepositChannelSettings(
				new[] { 1 },
				new[] { channelId },
				.1M,
				1000M,
				10M),
			new Dictionary<Guid, Guid>
			{
				[brandPaymentId] = channelId
			},
			fakePromotionServiceClient);
		var sut = new DepositEventHandler(
			Guid.NewGuid(),
			promotion,
			fakePlayerService,
			fakeTgsRepository,
			fakeSerialNumberGenerator,
			fakeStatisticsService);

		var depositForm = new DepositFormInfo
		{
			Id = Guid.NewGuid(),
			ActualAmount = 1000M,
			Amount = 1500M,
			PlayerCode = "test1",
			BrandPaymentId = brandPaymentId
		};

		_ = fakePlayerService.GetDepositPlayerAsync(Arg.Is(depositForm.PlayerCode))
			.Returns(new DepositPlayer(depositForm.PlayerCode, 1));

		var planId = Guid.NewGuid();
		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(
			Arg.Is(promotion.PromotionId),
			Arg.Any<long>())
			.Returns(new PromotionDistributePlanInfo(
				planId,
				Guid.NewGuid(),
				promotion.PromotionId,
				0L));

		_ = fakeTgsRepository.CheckDepositEventHasProcessedAsync(
			Arg.Is(depositForm.PlayerCode),
			Arg.Is(planId))
			.Returns(true);

		// Act
		await sut.OnDepositedAsync(depositForm);

		// Assert
		_ = fakeStatisticsService.Received(0)
			.IncrementPromotionUsedAmountAsync(Arg.Is(promotion.PromotionId), Arg.Is(100M));
		_ = fakeTgsRepository.Received(0)
			.SaveDepositEventAsync(Arg.Any<DepositEvent>());
		_ = fakePromotionServiceClient.Received(0)
			.DistributePromotionBonusAsync(
				Arg.Any<Guid>(),
				Arg.Any<string>(),
				Arg.Any<decimal>(),
				Arg.Any<decimal>(),
				Arg.Any<string>(),
				Arg.Any<Guid>());
	}
}
