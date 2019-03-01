using NetEscapades.Configuration.Validation;

namespace LocusPocusBot
{
    public class BotConfiguration : IValidatable
    {
        public string BotToken { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.BotToken))
            {
                throw new SettingsValidationException(nameof(BotConfiguration), nameof(this.BotToken), "must be a non-empty string");
            }
        }
    }
}
