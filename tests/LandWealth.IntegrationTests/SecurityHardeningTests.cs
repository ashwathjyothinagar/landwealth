using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandWealth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LandWealth.IntegrationTests;

public class SecurityHardeningTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public SecurityHardeningTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuditLog_RecordsFinancialChanges_AndHidesThemFromOtherUsers()
    {
        var owner = await AuthenticatedClient("audit-owner@landwealth.test");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var categories = (await owner.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!;
        var purchase = categories.Single(category => category.Name == "Property Purchase").Id;
        var bank = await owner.PostAsJsonAsync("/api/accounts", new
        {
            name = "HDFC",
            accountType = "BankAccount",
            openingBalance = 500000,
            lastFourDigits = "4821",
            institution = "HDFC Bank"
        });
        bank.StatusCode.Should().Be(HttpStatusCode.Created);
        var accountId = (await bank.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var property = await owner.PostAsJsonAsync("/api/properties", new
        {
            name = "Audit Farm",
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        property.StatusCode.Should().Be(HttpStatusCode.Created);
        var propertyId = (await property.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var posted = await owner.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = today,
            transactionType = "PropertyPurchase",
            amount = 200000,
            description = "Purchase",
            propertyId,
            lines = new object[]
            {
                new { lineType = "Debit", amount = 200000, propertyId, categoryId = purchase },
                new { lineType = "Credit", amount = 200000, accountId }
            }
        });
        posted.StatusCode.Should().Be(HttpStatusCode.Created);
        var transactionId = (await posted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var reversed = await owner.PostAsJsonAsync($"/api/transactions/{transactionId}/reverse", new { description = "Reverse the purchase" });
        reversed.StatusCode.Should().Be(HttpStatusCode.Created);

        (await owner.PostAsJsonAsync($"/api/properties/{propertyId}/valuations", new
        {
            valuationDate = today,
            estimatedValue = 350000,
            valuationSource = "LocalMarketSurvey"
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await owner.PostAsJsonAsync("/api/assets", new
        {
            name = "Gold Sovereigns",
            assetType = "Gold",
            acquisitionCost = 20000,
            estimatedValue = 25000
        })).StatusCode.Should().Be(HttpStatusCode.Created);
        (await owner.PostAsJsonAsync("/api/liabilities", new
        {
            name = "Land loan",
            liabilityType = "LandLoan",
            principalAmount = 40000,
            lender = "Canara"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var logs = await owner.GetFromJsonAsync<List<AuditItem>>("/api/audit-logs");
        logs!.Should().Contain(log => log.EntityName == "Transaction" && log.Action == "Insert" && log.Summary == "Purchase");
        logs.Should().Contain(log => log.EntityName == "Transaction" && log.Action == "Reversal" && log.Summary == "Reverse the purchase");
        logs.Should().Contain(log => log.EntityName == "Account" && log.Action == "Insert" && log.Summary == "HDFC");
        logs.Should().Contain(log => log.EntityName == "PropertyValuation" && log.Action == "Insert");
        logs.Should().Contain(log => log.EntityName == "Asset" && log.Action == "Insert" && log.Summary == "Gold Sovereigns");
        logs.Should().Contain(log => log.EntityName == "Liability" && log.Action == "Insert" && log.Summary == "Land loan");
        var body = await owner.GetStringAsync("/api/audit-logs");
        body.Should().NotContain("oldValues");
        body.Should().NotContain("newValues");
        body.Should().NotContain("password");

        (await owner.DeleteAsync("/api/audit-logs")).StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

        var intruder = await AuthenticatedClient("audit-intruder@landwealth.test");
        (await intruder.GetAsync($"/api/accounts/{accountId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.GetAsync($"/api/transactions/{transactionId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.GetAsync($"/api/properties/{propertyId}/valuations")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.PutAsJsonAsync($"/api/accounts/{accountId}", new { name = "Stolen", isActive = true })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.PostAsJsonAsync($"/api/transactions/{transactionId}/reverse", new { description = "Steal the reversal" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.PostAsJsonAsync("/api/reminders", new
        {
            title = "Pay property tax",
            dueDate = today,
            priority = "Low",
            propertyId
        })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        var hidden = await intruder.GetFromJsonAsync<List<AuditItem>>("/api/audit-logs");
        hidden!.Should().NotContain(log => log.EntityId == transactionId.ToString());

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await database.AuditLogs.IgnoreQueryFilters().FirstAsync();
        database.Remove(row);
        var act = () => database.SaveChanges();
        act.Should().Throw<InvalidOperationException>().WithMessage("*append-only*");

        var health = await owner.GetAsync("/api/health");
        health.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        health.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact]
    public async Task TamperedToken_IsRejected()
    {
        var owner = await AuthenticatedClient("tamper@landwealth.test");
        var token = owner.DefaultRequestHeaders.Authorization!.Parameter!;
        var broken = token[..^1] + (token[^1] == 'a' ? "b" : "a");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", broken);
        (await client.GetAsync("/api/audit-logs")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _factory.CreateClient().GetAsync("/api/audit-logs")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> AuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1!",
            fullName = "Test User"
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await register.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return client;
    }

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record AuditItem(string EntityName, string EntityId, string Action, string? Summary);
}

public class AuthRateLimitTests : IClassFixture<RateLimitedApiFactory>
{
    private readonly RateLimitedApiFactory _factory;

    public AuthRateLimitTests(RateLimitedApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_IsRejectedAfterTheAuthLimit()
    {
        var client = _factory.CreateClient();
        HttpStatusCode last = HttpStatusCode.OK;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "missing@landwealth.test",
                password = "Password1!"
            });
            last = response.StatusCode;
        }

        last.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
