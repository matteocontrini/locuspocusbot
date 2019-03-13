using System;

namespace LocusPocusBot
{
    public class BotConfiguration
    {
        public string BotToken { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.BotToken))
            {
                throw new Exception("BotConfiguration.BotToken must be a non-empty string");
            }
        }
    }
}
