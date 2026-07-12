using CarvedRock.IntegrationTests.Utils;

namespace CarvedRock.IntegrationTests;
public class WebAppTests(AppFixture fixture) : IClassFixture<AppFixture>
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    
    [Fact]
    public async Task GetHomePageReturnsOkStatusCode()
    {
        // Arrange
        //var cancellationToken = TestContext.Current.CancellationToken;
        //var appHost = await DistributedApplicationTestingBuilder.
        //    CreateAsync<Projects.CarvedRock_Aspire_AppHost>(cancellationToken);
        //appHost.Services.AddLogging(logging =>
        //{
        //    logging.SetMinimumLevel(LogLevel.Debug);
        //    // Override the logging filters from the app's configuration
        //    logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
        //    logging.AddFilter("Aspire.", LogLevel.Debug);
        //    // To output logs to the xUnit.net ITestOutputHelper,
        //    // consider adding a package from https://www.nuget.org/packages?q=xunit+logging
        //});
        //appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        //{
        //    clientBuilder.AddStandardResilienceHandler();
        //});

        //await using var app = await appHost.BuildAsync(cancellationToken)
        //    .WaitAsync(DefaultTimeout, cancellationToken);
        //await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        var httpClient = fixture.App.CreateHttpClient("webapp");
        await fixture.App.ResourceNotifications.WaitForResourceHealthyAsync("webapp", fixture.CancelToken)
            .WaitAsync(_defaultTimeout, fixture.CancelToken);
        var response = await httpClient.GetAsync("/", fixture.CancelToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
