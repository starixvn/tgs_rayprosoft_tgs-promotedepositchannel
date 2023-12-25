using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Promotion.Clients;
using Tgs.Promotion.PromoteDepositChannel;
using Tgs.SerialNumberGenerator;

namespace PromoteDepositChannel.Core.UnitTests;

public class BrandPromotionManagerTests
{
	[Fact]
	public async Task 更新所有Brand的活動列表()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new BrandPromotionManager(
			fakePromotionServiceClient,
			fakePaymentServiceClient,
			fakeServiceProvider,
			NullLogger<BrandPromotionManager>.Instance);

		// Act
		await sut.RenewAllBrandActivatedPromotionsAsync();

		// Assert
		_ = fakePromotionServiceClient.Received(1)
			.ListPromotionsByPromotionTypeAsync(Arg.Any<string>());
	}

	[Fact]
	public async Task 更新所有Brand的活動列表時_會從資料庫初始化UsedAmount()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeServiceProvider = new ServiceCollection()
			.AddSingleton(fakeStatisticsService)
			.AddSingleton<ILogger<BrandDepositEventHandler>>(NullLogger<BrandDepositEventHandler>.Instance)
			.AddSingleton(Substitute.For<IPlayerService>())
			.AddSingleton(Substitute.For<ITgsRepository>())
			.AddSingleton(Substitute.For<ISerialNumberGenerator>())
			.AddSingleton(Substitute.For<IEventSender>())
			.BuildServiceProvider();

		var sut = new BrandPromotionManager(
			fakePromotionServiceClient,
			fakePaymentServiceClient,
			fakeServiceProvider,
			NullLogger<BrandPromotionManager>.Instance);

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Any<string>())
			.Returns(new[]{
				new PromotionMainInfo
				{
					Id = Guid.NewGuid(),
					EventDuring = (DateTime.Now, DateTime.Now.AddDays(1D))
				}
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<BrandPromotionManager.PromotionRule>(Arg.Any<Guid>())
			.Returns(new BrandPromotionManager.PromotionRule(
				new PromoteDepositChannelSettings(
					Array.Empty<int>(),
					Array.Empty<Guid>(),
					0M,
					0M,
					0M,
					["SportsBook"])));

		// Act
		await sut.RenewAllBrandActivatedPromotionsAsync();

		// Assert
		_ = fakeStatisticsService.Received(1)
			.InitPromotionUsedAmountAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<DateTime>());
	}

	[Fact]
	public async Task 處理儲值事件()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePlayerService = Substitute.For<IPlayerService>();
		var fakeServiceProvider = new ServiceCollection()
			.AddSingleton(fakePlayerService)
			.AddSingleton(Substitute.For<ITgsRepository>())
			.AddSingleton(Substitute.For<ISerialNumberGenerator>())
			.AddSingleton<ILogger<BrandDepositEventHandler>>(NullLogger<BrandDepositEventHandler>.Instance)
			.AddSingleton(Substitute.For<IStatisticsService>())
			.AddSingleton(Substitute.For<IEventSender>())
			.BuildServiceProvider();

		var sut = new BrandPromotionManager(
			fakePromotionServiceClient,
			fakePaymentServiceClient,
			fakeServiceProvider, NullLogger<BrandPromotionManager>.Instance);

		var brandPayemntId = Guid.NewGuid();
		var channelId = Guid.NewGuid();
		var depositForm = new DepositFormInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			PlayerCode = "test1",
			Amount = 1000M,
			CreatedOn = DateTime.UtcNow,
			BrandPaymentId = brandPayemntId
		};

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Any<string>())
			.Returns(new[]
			{
				new PromotionMainInfo
				{
					Id = Guid.NewGuid(),
					BrandId = depositForm.BrandId,
					EventDuring = (DateTime.UtcNow.AddDays(-1D), DateTime.UtcNow.AddDays(1D)),
					PromotionType = "PromoteDepositChannel"
				}
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<BrandPromotionManager.PromotionRule>(Arg.Any<Guid>())
			.Returns(new BrandPromotionManager.PromotionRule(
				new PromoteDepositChannelSettings(
					Array.Empty<int>(),
					[channelId],
					0M,
					0M,
					0M,
					["SportsBook"])));

		_ = fakePaymentServiceClient.GetBrandPaymentsAsync(Arg.Is(depositForm.BrandId))
			.Returns(new[]
			{
				new BrandPaymentInfo
				{
					Id = brandPayemntId,
					ChannelId = channelId
				}
			}.ToAsyncEnumerable());

		await sut.RenewAllBrandActivatedPromotionsAsync();
		// Act
		await sut.ProcessDepositEventAsync(depositForm).WaitAsync(TimeSpan.FromSeconds(1D));

		// Assert
		_ = fakePlayerService.Received(1)
			.GetDepositPlayerAsync(Arg.Is(depositForm.PlayerCode));
	}

	[Fact]
	public async Task 處理儲值事件_如果Brand沒有活動會略過處理()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new BrandPromotionManager(
			fakePromotionServiceClient,
			fakePaymentServiceClient,
			fakeServiceProvider,
			NullLogger<BrandPromotionManager>.Instance);

		var depositForm = new DepositFormInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid(),
			PlayerCode = "test1",
			Amount = 1000M,
			CreatedOn = DateTime.UtcNow
		};

		await sut.RenewAllBrandActivatedPromotionsAsync();

		await sut.ProcessDepositEventAsync(depositForm);
	}
}
