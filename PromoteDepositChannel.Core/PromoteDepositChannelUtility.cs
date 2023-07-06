using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.PromoteDepositChannel;

internal static class PromoteDepositChannelUtility
{
	public static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		Converters = {
			new JsonStringEnumConverter()
		}
	};
}
