using System.Net;
using System.Text;
using FinanceTracker.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinanceTracker.IntegrationTests;

public class FxRateServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public int Calls { get; private set; }

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(_responder(request));
        }
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    [Fact]
    public async Task ParsesRates_AndCachesSubsequentCalls()
    {
        // Unique base so the service's static cache doesn't collide with other tests.
        var handler = new StubHandler(_ => Json("{\"base\":\"AUD\",\"date\":\"2026-05-20\",\"rates\":{\"EUR\":0.61,\"INR\":55.2}}"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
        var svc = new FrankfurterFxService(http, NullLogger<FrankfurterFxService>.Instance);

        var first = await svc.GetRatesAsync("AUD");
        first.Base.Should().Be("AUD");
        first.Rates["INR"].Should().Be(55.2m);

        var second = await svc.GetRatesAsync("AUD");
        second.Rates["EUR"].Should().Be(0.61m);
        handler.Calls.Should().Be(1); // second call served from cache
    }

    [Fact]
    public async Task OnHttpFailure_ReturnsIdentityFallback()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.frankfurter.app/") };
        var svc = new FrankfurterFxService(http, NullLogger<FrankfurterFxService>.Instance);

        var rates = await svc.GetRatesAsync("NZD"); // unique base, not cached
        rates.Base.Should().Be("NZD");
        rates.Rates.Should().ContainKey("NZD").WhoseValue.Should().Be(1m);
    }
}
