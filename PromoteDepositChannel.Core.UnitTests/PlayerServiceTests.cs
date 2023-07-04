using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.PromoteDepositChannel;
using Tgs.VipLevelService.Clients;

namespace PromoteDepositChannel.Core.UnitTests;

public class PlayerServiceTests
{
	[Fact]
	public async Task 取得玩家VipLevel()
	{
		// Arrange
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new PlayerService(
			fakePlayerServiceClient,
			fakeVipLevelServiceClient);

		var playerCode = "test1";
		var brandId = Guid.NewGuid();
		var level2Id = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				VipLevelId = level2Id
			});

		_ = fakeVipLevelServiceClient.GetBrandAllVipLevelsAsync(Arg.Is(brandId))
			.Returns(new[]
			{
				new VipLevelInfo(
					Guid.NewGuid(),
					1,
					brandId,
					0M,
					0M,
					0M,
					0M,
					false,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M),
				new VipLevelInfo(
					level2Id,
					2,
					brandId,
					0M,
					0M,
					0M,
					0M,
					false,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M),
				new VipLevelInfo(
					Guid.NewGuid(),
					3,
					brandId,
					0M,
					0M,
					0M,
					0M,
					false,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M)
			}.ToAsyncEnumerable());

		// Act
		var actual = await sut.GetDepositPlayerAsync(playerCode);

		// Assert
		Assert.Equal(2, actual!.VipLevel);
	}

	[Fact]
	public async Task 取得玩家VipLevel_如果玩家資料上沒有VipLevelId_則為等級一()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new PlayerService(
			fakePlayerServiceClient,
			fakeVipLevelServiceClient);

		var playerCode = "test1";
		var brandId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId
			});

		_ = fakeVipLevelServiceClient.GetBrandAllVipLevelsAsync(Arg.Is(brandId))
			.Returns(new[]
			{
				new VipLevelInfo(
					Guid.NewGuid(),
					1,
					brandId,
					0M,
					0M,
					0M,
					0M,
					false,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M,
					0M)
			}.ToAsyncEnumerable());

		// Act
		var actual = await sut.GetDepositPlayerAsync(playerCode);

		// Assert
		Assert.Equal(1, actual!.VipLevel);
	}

	[Fact]
	public async Task 取得玩家VipLevel_如果找不到玩家就回傳Null()
	{
		// Arrange
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new PlayerService(
			fakePlayerServiceClient,
			fakeVipLevelServiceClient);

		var playerCode = "test1";

		// Act
		var actual = await sut.GetDepositPlayerAsync(playerCode);

		// Assert
		Assert.Null(actual);
	}
}
