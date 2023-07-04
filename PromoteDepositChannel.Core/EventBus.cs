using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tgs.Promotion.PromoteDepositChannel;

internal class EventBus : IEventSender
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<EventBus> _logger;

	private readonly Channel<FormEvent> _channel = Channel.CreateUnbounded<FormEvent>(new UnboundedChannelOptions
	{
		SingleReader = true
	});

	public EventBus(IServiceProvider serviceProvider, ILogger<EventBus> logger)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask PushEventAsync(FormEvent formEvent, CancellationToken cancellationToken = default)
		=> _channel.Writer.WriteAsync(formEvent, cancellationToken);

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
}
