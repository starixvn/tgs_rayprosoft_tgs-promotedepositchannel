using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.PromoteDepositChannel.MessageQueue;

internal class DepositFormStatusChangedHandler(
	IPaymentServiceClient paymentServiceClient,
	IEventSender eventSender,
	ILogger<DepositFormStatusChangedHandler> logger) : IMessageHandler
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
	private readonly IEventSender _eventSender = eventSender ?? throw new ArgumentNullException(nameof(eventSender));
	private readonly ILogger<DepositFormStatusChangedHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var formEvent = FormEventEx.Parser.ParseFrom(data.Span);
		var changedStatus = Enum.Parse<PaymentFlowState>(formEvent.Status);

		var depositForm = await _paymentServiceClient.GetDepositFormAsync(
			formEvent.FormId,
			cancellationToken).ConfigureAwait(false);

		if (depositForm is null)
		{
			_logger.LogDebug("Can't find deposit form {id} info.", formEvent.FormId.ToGuid());
			return;
		}

		_logger.LogDebug("DepositForm({changedStatus}, {status}, {activityId})", changedStatus, depositForm.Status, Activity.Current?.Id);

		if (changedStatus != depositForm.Status
			&& depositForm.Status == PaymentFlowState.Approved)
			await _eventSender.PushEventAsync(
				new FormEvent(
					depositForm,
					Activity.Current?.Id),
				cancellationToken).ConfigureAwait(false);
	}
}
