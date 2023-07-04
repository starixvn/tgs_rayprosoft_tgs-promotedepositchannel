using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.PromoteDepositChannel;
using Tgs.Promotion.PromoteDepositChannel.Repositories;
using Tgs.Promotion.PromoteDepositChannel.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromoteDepositChannelRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbConfigure)
		=> services
			.AddTransient<ITgsRepository, TgsRepository>()
			.AddDbContext<TgsContext>(dbConfigure);
}
