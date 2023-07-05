using System.Data.HashFunction.MurmurHash;
using System.Runtime.InteropServices;
using Tgs.Payment.Clients;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class DepositEventHandler
{
	private static readonly IMurmurHash3 _Hasher = MurmurHash3Factory.Instance.Create();

	private readonly Promotion _promotion;
	private readonly IPlayerService _playerService;
	private readonly ITgsRepository _tgsRepository;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IStatisticsService _statisticsService;

	public EventRange EventRange => _promotion.EventRange;

	public Guid BrandId { get; }

	public DepositEventHandler(
		Guid brandId,
		Promotion promotion,
		IPlayerService playerService,
		ITgsRepository tgsRepository,
		ISerialNumberGenerator serialNumberGenerator,
		IStatisticsService statisticsService)
	{
		BrandId = brandId;
		_promotion = promotion;
		_playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
	}

	public async ValueTask OnDepositedAsync(DepositFormInfo formInfo, CancellationToken cancellationToken = default)
	{
		if (!_promotion.CheckBrandPaymentInSettings(formInfo.BrandPaymentId))
			return;

		var playerInfo = await _playerService.GetDepositPlayerAsync(
			formInfo.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		if (playerInfo is null || !_promotion.CheckVipLevelInRange(playerInfo.VipLevel))
			return;

		var depositAmount = formInfo.ActualAmount ?? formInfo.Amount;

		var (bonus, bucketAmount) = _promotion.ComputeBonus(depositAmount);

		var conditionHash = MemoryMarshal.AsRef<uint>(_Hasher.ComputeHash(
			formInfo.Id.ToByteArray(),
			cancellationToken).Hash);

		var distributePlanId = await _promotion.GetPromotionDistributePlanAsync(
			conditionHash,
			cancellationToken).ConfigureAwait(false);

		var depositEvent = new DepositEvent(
			await _serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false),
			formInfo.PlayerCode,
			formInfo.Id,
			bonus,
			bucketAmount,
			playerInfo.VipLevel,
			distributePlanId,
			_promotion.PromotionId,
			DateTime.Now);

		await _statisticsService.IncrementPromotionUsedAmountAsync(
			_promotion.PromotionId,
			bonus,
			cancellationToken).ConfigureAwait(false);

		await _tgsRepository.SaveDepositEventAsync(
			depositEvent,
			cancellationToken).ConfigureAwait(false);

		await _promotion.DistributePromotionBonusAsync(
			distributePlanId,
			formInfo.PlayerCode,
			bonus,
			bucketAmount,
			depositEvent.Id,
			cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask InitPromotionUsedAmountAsync(CancellationToken cancellationToken = default)
	{
		var usedAmount = await _statisticsService.GetPromotionUsedAmountAsync(
			_promotion.PromotionId,
			cancellationToken).ConfigureAwait(false);

		if (usedAmount is null)
		{
			var (amount, _) = await _tgsRepository.GetPromotionUsedAmountAsync(
				_promotion.PromotionId,
				cancellationToken).ConfigureAwait(false);

			await _statisticsService.InitPromotionUsedAmountAsync(
				_promotion.PromotionId,
				amount,
				EventRange.End.AddDays(1D),
				cancellationToken).ConfigureAwait(false);
		}
	}
}
