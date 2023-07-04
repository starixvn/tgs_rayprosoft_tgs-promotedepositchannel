using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;

namespace Tgs.Promotion.PromoteDepositChannel.MessageQueue;

internal class DepositFormStatusChangedHandler : IMessageHandler
{
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly IEventSender _eventSender;

	public DepositFormStatusChangedHandler(IPaymentServiceClient paymentServiceClient, IEventSender eventSender)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_eventSender = eventSender ?? throw new ArgumentNullException(nameof(eventSender));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var formEvent = FormEventEx.Parser.ParseFrom(data.Span);
		var changedStatus = Enum.Parse<PaymentFlowState>(formEvent.Status);

		var depositForm = await _paymentServiceClient.GetDepositFormAsync(
			formEvent.FormId,
			cancellationToken).ConfigureAwait(false);

		if (depositForm is null)
			return;

		if (changedStatus != depositForm.Status
			&& depositForm.Status == PaymentFlowState.Approved)
			await _eventSender.PushEventAsync(
				new FormEvent(
					depositForm),
				cancellationToken).ConfigureAwait(false);
	}
}
