using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class EventBus : IEventSender
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<EventBus> _logger;

	private readonly Channel<FormEvent> _channel = Channel.CreateUnbounded<FormEvent>(new UnboundedChannelOptions
	{
		SingleReader = true
	});

	private readonly Subject<Guid> _usedAmountSubject = new();

	public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask PushEventAsync(FormEvent formEvent, CancellationToken cancellationToken = default)
		=> _channel.Writer.WriteAsync(formEvent, cancellationToken);

	public ValueTask TriggerUpdateUsedAmountAsync(Guid promotionId)
	{
		_usedAmountSubject.OnNext(promotionId);
		return ValueTask.CompletedTask;
	}

	public async Task HandleEventLoopAsync(CancellationToken cancellationToken = default)
	{
		var reader = _channel.Reader;
		while (await reader
			.WaitToReadAsync(cancellationToken)
			.ConfigureAwait(false))
		{
			if (reader.TryRead(out var evt))
				try
				{
					using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

					await using var scope = _serviceProvider.CreateAsyncScope();

					var promotionManager = scope.ServiceProvider.GetRequiredService<BrandPromotionManager>();

					await promotionManager.ProcessDepositEventAsync(
						evt.FormInfo,
						cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Process command occur errors.");
				}
		}
	}

	public Task HandleUpdateUsedAmountLoopAsync(CancellationToken cancellationToken = default)
		=> _usedAmountSubject
			.GroupByUntil(
				id => id,
				g => Observable.Timer(TimeSpan.FromMinutes(5D)))
			.SelectMany(g =>
			{
				var flag = 1;

				return g.Where(_ => flag == 1)
					.Do(_ => Interlocked.Exchange(ref flag, 0))
					.Select(id => Observable
						.FromAsync(async () =>
						{
							await default(CreateThreadAwaiter);

							Interlocked.Exchange(ref flag, 1);
						})
						.SelectMany(_ => Observable.FromAsync(async () =>
						{
							await default(CreateThreadAwaiter);

							await using var scope = _serviceProvider.CreateAsyncScope();
							var tgsRepository = scope.ServiceProvider.GetRequiredService<ITgsRepository>();
							var promotionServiceClient = scope.ServiceProvider.GetRequiredService<IPromotionServiceClient>();

							var (amount, snapshotId) = await tgsRepository.GetPromotionUsedAmountAsync(
								id,
								cancellationToken).ConfigureAwait(false);

							await promotionServiceClient.UpdatePromotionUsedAmountAsync(
								id,
								amount,
								snapshotId,
								cancellationToken).ConfigureAwait(false);
						}))
						.Catch((Exception ex) =>
						{
							_logger.LogError(ex, "Process UsedAmount {id} occur error!", id);

							return Observable.Empty<Unit>();
						}))
					.Concat();
			})
			.ToTask(cancellationToken);

	private readonly struct CreateThreadAwaiter : INotifyCompletion
	{
		public bool IsCompleted => false;

		public CreateThreadAwaiter GetAwaiter() => this;

		public void GetResult()
		{
		}

		public void OnCompleted(Action continuation) => _ = Task.Run(continuation);
	}
}
