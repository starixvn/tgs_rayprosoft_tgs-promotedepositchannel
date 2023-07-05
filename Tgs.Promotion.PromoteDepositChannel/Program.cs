using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Promotion.PromoteDepositChannel;
using Tgs.Promotion.PromoteDepositChannel.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
			fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
				"MachineName",
				() => Environment.MachineName));
		});
	});

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfiguration => natsConfiguration
		.ConfigQueueOptions((options, sp) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();

			options.Url = config.GetConnectionString("Nats")!;
		})
		.AddHandler(
			ProcessorTypes.DepositFormStatusChangedHandlerType,
			"tgs.payment.deposit.status_ex",
			"tgs.promotion.promote-deposit-channel"))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddSingleton(sp => ActivatorUtilities.CreateInstance<RedisConnectionManager>(
		sp,
		builder.Configuration.GetConnectionString("Redis")!))
	.AddPromotionClientServices((promotionOptions, _) => promotionOptions.Url =
		builder.Configuration.GetValue<Uri>("PromoteDepositChannel:PromotionServiceUrl")!)
	.AddPaymentServiceClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(http => http.BaseAddress =
			builder.Configuration.GetValue<Uri>("PromoteDepositChannel:PaymentServiceUrl")))
	.AddPlayerServiceClient((playerOptions, _) => playerOptions.BaseUrl =
		builder.Configuration.GetValue<Uri>("PromoteDepositChannel:PlayerServiceUrl")!)
	.AddVipLevelServiceClient(httpBuilder => httpBuilder
		.ConfigureHttpClient(http => http.BaseAddress =
			builder.Configuration.GetValue<Uri>("PromoteDepositChannel:VipLevelServiceUrl")))
	.AddPromoteDepositChannel()
	.AddPromoteDepositChannelRepositories(
		(_, dbBuilder) => dbBuilder
			.UseNpgsql("name=Tgs"),
		sp => sp.GetRequiredService<RedisConnectionManager>().Database);

builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(resourceBuilder => resourceBuilder
		.AddEnvironmentVariableDetector())
	.WithTracing(tracerBuilder => tracerBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/healthz")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !new[] { "html", "ico", "png", "jpg" }.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddHttpClientInstrumentation()
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterBuilder => meterBuilder
		.AddRuntimeInstrumentation()
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddOtlpExporter())
	.Services
	.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Run();
