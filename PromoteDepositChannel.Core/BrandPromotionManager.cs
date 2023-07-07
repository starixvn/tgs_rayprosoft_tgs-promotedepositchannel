using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Promotion.Clients;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class BrandPromotionManager
{
	private static readonly ActivitySource _ActivitySource = new($"{typeof(BrandPromotionManager).Namespace}.{nameof(BrandPromotionManager)}");
	private static readonly string _PromotionType = "PromoteDepositChannel";
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<BrandPromotionManager> _logger;
	private readonly ReplaySubject<Dictionary<Guid, BrandDepositEventHandler>> _brandDepositHandlerObservable = new(1);

	public BrandPromotionManager(
		IPromotionServiceClient promotionServiceClient,
		IPaymentServiceClient paymentServiceClient,
		IServiceProvider serviceProvider,
		ILogger<BrandPromotionManager> logger)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task ProcessDepositEventAsync(DepositFormInfo formInfo, CancellationToken cancellationToken = default)
	{
		using var activity = _ActivitySource.StartActivity($"{nameof(BrandPromotionManager)}.{nameof(ProcessDepositEventAsync)}");

		var handlers = await _brandDepositHandlerObservable.ToAsyncEnumerable()
			.FirstAsync(cancellationToken)
			.ConfigureAwait(false);

		if (handlers.TryGetValue(formInfo.BrandId, out var handler))
			await handler.OnDepositedAsync(
				formInfo,
				cancellationToken).ConfigureAwait(false);
	}

	public async Task RenewAllBrandActivatedPromotionsAsync(CancellationToken cancellationToken = default)
	{
		using var activity = _ActivitySource.StartActivity($"{nameof(BrandPromotionManager)}.{nameof(RenewAllBrandActivatedPromotionsAsync)}");

		var dict = await CreateAllBrandActivatedPromotionsAsync(
			 cancellationToken).ConfigureAwait(false);

		foreach (var brandHandler in dict.Values)
			await brandHandler.InitPromotionsUsedAmountAsync(
				  cancellationToken).ConfigureAwait(false);

		_brandDepositHandlerObservable.OnNext(dict);
	}

	private ValueTask<Dictionary<Guid, BrandDepositEventHandler>> CreateAllBrandActivatedPromotionsAsync(CancellationToken cancellationToken = default)
		=> _promotionServiceClient.ListPromotionsByPromotionTypeAsync(_PromotionType, cancellationToken)
			.Where(info => new EventRange(
				info.EventDuring.From.ToUniversalTime().AddHours(-1D),
				info.EventDuring.To.ToUniversalTime().AddHours(1D)).InTheRange(DateTime.UtcNow))
			.Select(info => _promotionServiceClient
				.GetPromotionRuleAsync<PromotionRule>(info.Id, cancellationToken)
				.AsTask()
				.ToAsyncEnumerable()
				.Where(rule => rule is not null)
				.Select(rule => (Info: info, Settings: rule!.CompaignDetail)))
			.Merge()
			.GroupBy(tup => tup.Info.BrandId)
			.SelectMany(g => _paymentServiceClient
				.GetBrandPaymentsAsync(g.Key, cancellationToken)
				.ToDictionaryAsync(bp => bp.Id, bp => bp.ChannelId, cancellationToken)
				.AsTask()
				.ToAsyncEnumerable()
				.Select(bpDict => ActivatorUtilities.CreateInstance<BrandDepositEventHandler>(
					_serviceProvider,
					g.Key,
					g.Select(tup => ActivatorUtilities.CreateInstance<DepositEventHandler>(
						_serviceProvider,
						tup.Info.BrandId,
						new Promotion(
							tup.Info.Id,
							tup.Info.EventDuring,
							tup.Settings,
							bpDict,
							_promotionServiceClient)))
						.ToEnumerable())))
			.ToDictionaryAsync(brandHandler => brandHandler.BrandId, cancellationToken);

	internal record PromotionRule(PromoteDepositChannelSettings CompaignDetail);
}
