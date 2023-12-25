using System.Text.Json;
using Google.Protobuf;
using Tgs.Promotion.Clients.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.PromoteDepositChannel.MessageQueue;

internal class PromoteDepositChannelSettingProcessor : IMessageProcessor
{
	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = JsonSerializer.Deserialize<PromoteDepositChannelSettings>(
			request.Settings,
			PromoteDepositChannelUtility.SerializerOptions)
			?? throw new InvalidOperationException("Settings is null");

		var response = new CampaignDetailResponse
		{
			CampaignDetail = JsonSerializer.Serialize(settings, PromoteDepositChannelUtility.SerializerOptions)
		};

		return ValueTask.FromResult((ReadOnlyMemory<byte>)response.ToByteArray());
	}
}
