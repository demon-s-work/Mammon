using Mammon.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mammon
{
	public class KufarFetcher(ILogger<KufarFetcher> logger, IOptions<KufarSettings> settings) : IKufarFetcher
	{
		public void Fetch()
		{
			logger.LogInformation($"Base url: {settings.Value.BaseApiUrl}");
		}
	}
}