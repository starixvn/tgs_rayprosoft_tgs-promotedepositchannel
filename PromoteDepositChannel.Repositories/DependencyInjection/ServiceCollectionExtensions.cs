using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tgs.Promotion.PromoteDepositChannel;
using Tgs.Promotion.PromoteDepositChannel.Repositories;
using Tgs.Promotion.PromoteDepositChannel.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromoteDepositChannelRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbConfigure,
		Func<IServiceProvider, IDatabase> redisDatabaseFactory)
		=> services
			.AddTransient<ITgsRepository, TgsRepository>()
			.AddTransient<IStatisticsService>(sp => ActivatorUtilities.CreateInstance<RedisStatisticsService>(
				sp,
				redisDatabaseFactory(sp)))
			.AddDbContext<TgsContext>(dbConfigure);
}
