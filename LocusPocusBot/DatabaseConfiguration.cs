using NetEscapades.Configuration.Validation;

namespace LocusPocusBot
{
    public class DatabaseConfiguration : IValidatable
    {
        public string ConnectionString { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.ConnectionString))
            {
                throw new SettingsValidationException(nameof(DatabaseConfiguration), nameof(this.ConnectionString), "must be a non-empty string");
            }
        }
    }
}
