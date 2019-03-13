using System;

namespace LocusPocusBot
{
    public class DatabaseConfiguration
    {
        public string ConnectionString { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.ConnectionString))
            {
                throw new Exception("DatabaseConfiguration.ConnectionString must be a non-empty string");
            }
        }
    }
}
