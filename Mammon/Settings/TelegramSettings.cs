namespace Mammon.Settings
{
	public class TelegramSettings : BaseSettings
	{
		public required string BotToken { get; set; }
		public int ChatId { get; set; }
	}
}