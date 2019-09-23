namespace b0wter.CouchDb.Tests.Integration

module Configuration =
    open Microsoft.Extensions.Configuration
    
    let getConfigurationRoot () =
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional = false)
            .AddEnvironmentVariables().Build()

