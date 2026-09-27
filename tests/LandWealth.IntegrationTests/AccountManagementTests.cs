using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class AccountManagementTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public AccountManagementTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Accounts_MaskNumbers_AndKeepTheRunningBalanceConsistent()
    {
        var client = await AuthenticatedClient("accounts@landwealth.test");
        var categories = await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories");
        var salary = categories!.Single(category => category.Name == "Salary & Professional Income").Id;
        var household = categories.Single(category => category.Name == "Household Expense").Id;

        var fullNumber = await client.PostAsJsonAsync("/api/accounts", new
        {
            name = "Raw PAN",
            accountType = "BankAccount",
            openingBalance = 1000,
            lastFourDigits = "1234567890124821"
        });
        fullNumber.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var bankId = await CreateAccount(client, "HDFC Salary", "BankAccount", 10000m, "4821", "HDFC Bank");
        var cashId = await CreateAccount(client, "Petty cash", "CashAccount", 2000m, null, null);
        var cardId = await CreateAccount(client, "ICICI Card", "CreditCard", 0m, "9012", "ICICI Bank");

        var bank = await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}");
        bank!.MaskedAccountNumber.Should().Be("•••• 4821");
        bank.OpeningBalance.Should().Be(10000m);
        bank.CurrentBalance.Should().Be(10000m);
        bank.MaskedAccountNumber.Should().NotContain("1234567890124821");

        (await Post(client, "Income", 4000m, "Salary", salary, ("Debit", 4000m, bankId), ("Credit", 4000m, (Guid?)null)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await Post(client, "Expense", 1500m, "Groceries", household, ("Debit", 1500m, null), ("Credit", 1500m, bankId)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await Post(client, "Expense", 800m, "Card spend", household, ("Debit", 800m, null), ("Credit", 800m, cardId)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await Post(client, "LiabilityPayment", 300m, "Card payment", null, ("Debit", 300m, cardId), ("Credit", 300m, bankId)))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var statement = await client.GetFromJsonAsync<StatementItem>($"/api/accounts/{bankId}/statement");
        statement!.Activity.Should().HaveCount(3);
        statement.Activity[^1].Balance.Should().Be(12200m);
        statement.Account.CurrentBalance.Should().Be(statement.Activity[^1].Balance);

        var card = await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{cardId}");
        card!.CurrentBalance.Should().Be(500m);
        card.MaskedAccountNumber.Should().Be("•••• 9012");

        var cash = await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{cashId}");
        cash!.CurrentBalance.Should().Be(2000m);
        cash.MaskedAccountNumber.Should().BeNull();

        var closed = await client.PutAsJsonAsync($"/api/accounts/{bankId}", new
        {
            name = "HDFC Salary",
            institution = "HDFC Bank",
            isActive = false,
            notes = "Moved to another bank"
        });
        closed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rejected = await Post(client, "Income", 100m, "After close", salary, ("Debit", 100m, bankId), ("Credit", 100m, null));
        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var intruder = await AuthenticatedClient("accounts-other@landwealth.test");
        (await intruder.GetAsync($"/api/accounts/{bankId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.GetAsync($"/api/accounts/{bankId}/statement")).StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private static async Task<Guid> CreateAccount(
        HttpClient client, string name, string accountType, decimal openingBalance, string? lastFourDigits, string? institution)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new
        {
            name,
            accountType,
            openingBalance,
            institution,
            lastFourDigits
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> Post(
        HttpClient client,
        string transactionType,
        decimal amount,
        string description,
        Guid? categoryId,
        params (string LineType, decimal Amount, Guid? AccountId)[] lines)
    {
        return client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = "2026-06-01",
            transactionType,
            amount,
            description,
            lines = lines.Select(line => new
            {
                lineType = line.LineType,
                amount = line.Amount,
                accountId = line.AccountId,
                categoryId
            }).ToArray()
        });
    }

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record AccountItem(decimal OpeningBalance, decimal CurrentBalance, string? MaskedAccountNumber);
    private sealed record StatementItem(AccountItem Account, List<ActivityItem> Activity);
    private sealed record ActivityItem(decimal Balance);
}
