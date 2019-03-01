/*
 * 
 * This file is a modified version of the file available in the aspnet/Extensions repository:
 * https://github.com/aspnet/Extensions/blob/9bc79b2f25a3724376d7af19617c33749a30ea3a/src/Logging/Logging.Console/src/ConsoleLoggerOptionsSetup.cs
 *
 * The original file is distributed under the Apache 2.0 license.
 *
 * This file is distributed under the license of this repository.
 * 
 */
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace CustomConsoleLogger
{
    internal class CustomConsoleLoggerOptionsSetup : ConfigureFromConfigurationOptions<ConsoleLoggerOptions>
    {
        public CustomConsoleLoggerOptionsSetup(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
            : base(providerConfiguration.Configuration)
        {
        }
    }
}
