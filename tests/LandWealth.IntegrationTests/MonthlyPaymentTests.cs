using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class MonthlyPaymentTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public MonthlyPaymentTests(LandWealthApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Month_ShowsPaidDate_AndRemainingPayments()
    {
        var client = await AuthenticatedClient("monthly-payments@landwealth.test");
        var household = (await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!
            .Single(category => category.Name == "Household Expense").Id;
        var bankId = await CreateAccount(client);

        var emi = await client.PostAsJsonAsync("/api/monthly-payments", new
        {
            name = "Home loan",
            kind = "Emi",
            amount = 10000,
            dueDay = 5,
            accountId = bankId,
            categoryId = household,
            startsOn = "2026-01-01",
            totalInstallments = 3
        });
        emi.StatusCode.Should().Be(HttpStatusCode.Created);
        var emiId = (await emi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await client.PostAsJsonAsync("/api/monthly-payments", new
        {
            name = "Electricity",
            kind = "Bill",
            amount = 1800,
            dueDay = 12,
            accountId = bankId,
            categoryId = household,
            startsOn = "2026-01-01"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var before = await client.GetFromJsonAsync<MonthBoard>("/api/monthly-payments/month?year=2026&month=1");
        before!.RemainingCount.Should().Be(2);
        before.PaidCount.Should().Be(0);
        before.Transactions.Should().BeEmpty();

        var recorded = await client.PostAsJsonAsync($"/api/monthly-payments/{emiId}/payments", new
        {
            year = 2026,
            month = 1,
            paidOn = "2026-01-05",
            amount = 10000
        });
        recorded.StatusCode.Should().Be(HttpStatusCode.Created);

        var after = await client.GetFromJsonAsync<MonthBoard>("/api/monthly-payments/month?year=2026&month=1");
        after!.PaidCount.Should().Be(1);
        after.RemainingCount.Should().Be(1);
        after.Items.Single(item => item.Name == "Home loan").PaidOn.Should().Be("2026-01-05");
        after.Items.Single(item => item.Name == "Home loan").InstallmentsRemaining.Should().Be(2);
        after.Transactions.Should().ContainSingle();
        after.Transactions[0].PaidOn.Should().Be("2026-01-05");
        after.Transactions[0].Description.Should().Be("Home loan 2026-01");

        (await client.PostAsJsonAsync($"/api/monthly-payments/{emiId}/payments", new
        {
            year = 2026,
            month = 1,
            paidOn = "2026-01-06",
            amount = 10000
        })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var finished = await client.GetFromJsonAsync<MonthBoard>("/api/monthly-payments/month?year=2026&month=4");
        finished!.Items.Should().NotContain(item => item.Name == "Home loan");
        finished.Items.Should().Contain(item => item.Name == "Electricity");

        (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance.Should().Be(0m);

        var other = await AuthenticatedClient("monthly-other@landwealth.test");
        (await other.GetFromJsonAsync<MonthBoard>("/api/monthly-payments/month?year=2026&month=1"))!.DueCount.Should().Be(0);
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

    private static async Task<Guid> CreateAccount(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            name = "Salary account",
            accountType = "BankAccount",
            openingBalance = 10000,
            lastFourDigits = "4821",
            institution = "HDFC Bank"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record AccountItem(decimal CurrentBalance);
    private sealed record MonthItem(string Name, string? PaidOn, int? InstallmentsRemaining);
    private sealed record MonthTransaction(string PaidOn, string Description);
    private sealed record MonthBoard(int PaidCount, int RemainingCount, int DueCount, List<MonthItem> Items, List<MonthTransaction> Transactions);
}
