using System.Reflection;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.Storage;
using Mammon.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mammon
{
	internal class Program
	{
		private static async Task Main(string[] args)
		{
			var configuration = CreateConfigurationBuilder(args).Build();

			var builder = Host.CreateApplicationBuilder();
			builder.Configuration.AddConfiguration(configuration);

			builder.Logging.AddConsole();
			builder.Services.AddHangfire();
			builder.Services.AddScoped<IKufarFetcher, KufarFetcher>();
			builder.Services.InjectSettings(configuration);

			var app = builder.Build();
			app.RegisterRecurrentJobs();

			await app.RunAsync();
		}

		private static IConfigurationBuilder CreateConfigurationBuilder(string[] args)
		{
			var builder = new ConfigurationBuilder()
			              .SetBasePath(AppContext.BaseDirectory)
			              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			              .AddEnvironmentVariables();

			return builder;
		}
	}

	public static class Extensions
	{
		public static void RegisterRecurrentJobs(this IHost app)
		{
			var services = app.Services;

			var logger = services.GetRequiredService<ILogger<Program>>();
			var recurringJobManager = services.GetRequiredService<IRecurringJobManager>();
			recurringJobManager.AddOrUpdate<IKufarFetcher>("Kufar fetch", x => x.Fetch(), "*/10 * * * * *");

			logger.LogInformation("Registered recurrent jobs: {Join}", string.Join(", ", JobStorage.Current.GetConnection().GetRecurringJobs().Select(j => j.Id)));
		}

		public static void AddHangfire(this IServiceCollection services)
		{
			services.AddHangfire(opts => {
				opts.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
				    .UseSimpleAssemblyNameTypeSerializer()
				    .UseRecommendedSerializerSettings()
				    .UseMemoryStorage();
			});
			services.AddHangfireServer();
		}

		public static void InjectSettings(this IServiceCollection services, IConfiguration configuration)
		{
			var pType = typeof(BaseSettings);
			var children = Assembly.GetExecutingAssembly().GetTypes()
			                       .Where(t => t.IsClass && t != pType && pType.IsAssignableFrom(t));
			
			foreach (var setting in children)
			{
				var method = typeof(OptionsConfigurationServiceCollectionExtensions)
				             .GetMethod("Configure", [typeof(IServiceCollection), typeof(IConfiguration)])
				             ?.MakeGenericMethod(setting);
				method?.Invoke(services, [services, configuration.GetSection(setting.Name)]);
				using var loggerFactory = LoggerFactory.Create(builder => {
					builder.AddConsole();
				});
				var logger = loggerFactory.CreateLogger<Program>();
				logger.LogInformation("Injected settings for {Name}", setting.Name);
			}
		}
	}
}