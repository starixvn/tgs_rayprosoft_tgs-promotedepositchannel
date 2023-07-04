using Tgs.PlayerService.Client;
using Tgs.VipLevelService.Clients;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class PlayerService : IPlayerService
{
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IVipLevelServiceClient _vipLevelServiceClient;

	public PlayerService(
		IPlayerServiceClient playerServiceClient,
		IVipLevelServiceClient vipLevelServiceClient)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_vipLevelServiceClient = vipLevelServiceClient ?? throw new ArgumentNullException(nameof(vipLevelServiceClient));
	}

	public async ValueTask<DepositPlayer?> GetDepositPlayerAsync(string playerCode, CancellationToken cancellationToken = default)
	{
		var playerInfo = await _playerServiceClient.GetPlayerInfoAsync(
			playerCode,
			cancellationToken).ConfigureAwait(false);

		if (playerInfo is null)
			return null;

		var brandAllVipLevels = await _vipLevelServiceClient
			.GetBrandAllVipLevelsAsync(playerInfo.BrandId, cancellationToken)
			.ToDictionaryAsync(
				vip => vip.Id,
				vip => vip.Level,
				cancellationToken)
			.ConfigureAwait(false);

		var vipLevel = playerInfo.VipLevelId is not null
			&& brandAllVipLevels.TryGetValue(playerInfo.VipLevelId.Value, out var level)
			? level
			: 1;

		return new DepositPlayer(playerCode, vipLevel);
	}
}
