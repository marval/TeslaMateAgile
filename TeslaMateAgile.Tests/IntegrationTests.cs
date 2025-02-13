﻿using GraphQL.Client.Abstractions;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TeslaMateAgile.Data.Options;
using TeslaMateAgile.Services;
using TeslaMateAgile.Services.Interfaces;

namespace TeslaMateAgile.Tests;

public class IntegrationTests
{
    private const string IntegrationTest = "Integration test";

    [Ignore(IntegrationTest)]
    [Test]
    public async Task IntegrationTests_Tibber()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>();

        var config = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddTransient<IGraphQLJsonSerializer, SystemTextJsonSerializer>();
        services.Configure<TibberOptions>(config.GetSection("Tibber"));
        services.AddHttpClient<IPriceDataService, TibberService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TibberOptions>>().Value;
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.AccessToken);
        });

        var priceDataService = services.BuildServiceProvider().GetRequiredService<IPriceDataService>();

        var from = DateTimeOffset.Parse("2020-01-01T00:25:00+00:00");
        var to = DateTimeOffset.Parse("2020-01-01T15:00:00+00:00");

        var priceData = await priceDataService.GetPriceData(from, to);

        Assert.LessOrEqual(priceData.Min(x => x.ValidFrom), from);
        Assert.GreaterOrEqual(priceData.Max(x => x.ValidTo), to);
    }

    [Ignore(IntegrationTest)]
    [Test]
    public async Task IntegrationTests_Awattar()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>();

        var config = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddOptions<AwattarOptions>()
                        .Bind(config.GetSection("Awattar"))
                        .ValidateDataAnnotations();
        services.AddHttpClient<IPriceDataService, AwattarService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AwattarOptions>>().Value;
            var baseUrl = options.BaseUrl;
            if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
            client.BaseAddress = new Uri(baseUrl);
        });

        var priceDataService = services.BuildServiceProvider().GetRequiredService<IPriceDataService>();

        var from = DateTimeOffset.Parse("2020-01-01T00:25:00+00:00");
        var to = DateTimeOffset.Parse("2020-01-01T15:55:00+00:00");

        var priceData = await priceDataService.GetPriceData(from, to);

        Assert.LessOrEqual(priceData.Min(x => x.ValidFrom), from);
        Assert.GreaterOrEqual(priceData.Max(x => x.ValidTo), to);
    }

    [Ignore(IntegrationTest)]
    [Test]
    public async Task IntegrationTests_Energinet()
    {
        var configBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Energinet:BaseUrl"] = "https://api.energidataservice.dk/dataset/",
                ["Energinet:Region"] = "DK1",
                ["Energinet:Currency"] = "DKK",
                ["Energinet:VAT"] = "1.25",
                ["Energinet:FixedPrices:TimeZone"] = "Europe/London",
                ["Energinet:FixedPrices:Prices:0"] = "00:00-12:00=0.25",
                ["Energinet:FixedPrices:Prices:1"] = "12:00-00:00=0.50"
            });

        var config = configBuilder.Build();

        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddOptions<EnerginetOptions>()
                        .Bind(config.GetSection("Energinet"))
                        .ValidateDataAnnotations();
        services.AddHttpClient<IPriceDataService, EnerginetService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EnerginetOptions>>().Value;
            var baseUrl = options.BaseUrl;
            if (!baseUrl.EndsWith("/")) { baseUrl += "/"; }
            client.BaseAddress = new Uri(baseUrl);
        });
        var priceDataService = services.BuildServiceProvider().GetRequiredService<IPriceDataService>();

        var from = new DateTimeOffset(2022, 2, 20, 0, 0, 0, new TimeSpan(1, 0, 0));
        var to = new DateTimeOffset(2022, 2, 20, 23, 59, 0, new TimeSpan(1, 0, 0));

        var priceData = await priceDataService.GetPriceData(from, to);

        foreach (var price in priceData)
        {
            Console.WriteLine($"{price.ValidFrom} - {price.ValidTo} - {price.Value}");
        }

        Assert.LessOrEqual(priceData.Min(x => x.ValidFrom), from);
        Assert.GreaterOrEqual(priceData.Max(x => x.ValidTo), to);
    }
}
