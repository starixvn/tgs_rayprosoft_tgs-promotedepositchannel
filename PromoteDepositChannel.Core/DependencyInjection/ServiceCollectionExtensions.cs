using Tgs.Promotion.PromoteDepositChannel;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromoteDepositChannel(this IServiceCollection services)
		=> services
			.AddTransient<IPlayerService, PlayerService>()
			.AddSingleton<EventBus>()
			.AddSingleton<IEventSender>(sp => sp.GetRequiredService<EventBus>())
			.AddSingleton<BrandPromotionManager>();
}
