using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class TransactionEngineTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public TransactionEngineTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Ledger_PostsClassifiedEntries_AndCorrectsThemWithoutDeletion()
    {
        var client = await AuthenticatedClient("ledger-engine@landwealth.test");
        var categories = (await client.GetFromJsonAsync<List<CategoryItem>>("/api/categories"))!;
        var salary = categories.Single(category => category.Name == "Salary & Professional Income").Id;
        var household = categories.Single(category => category.Name == "Household Expense").Id;
        var purchase = categories.Single(category => category.Name == "Property Purchase").Id;

        var bankId = await CreateAccount(client, "HDFC Salary", "BankAccount", 10000m, "4821");
        var cashId = await CreateAccount(client, "Petty cash", "CashAccount", 500m, null);

        var unbalanced = await client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = "2026-06-01",
            transactionType = "Income",
            amount = 100,
            description = "Unbalanced",
            lines = new object[]
            {
                new { lineType = "Debit", amount = 100, accountId = bankId },
                new { lineType = "Credit", amount = 40, categoryId = salary }
            }
        });
        unbalanced.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var wrongWay = await Post(client, "Income", 100m, "Wrong way", null, ("Credit", 100m, bankId, (Guid?)null), ("Debit", 100m, null, salary));
        wrongWay.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await Post(client, "Income", 4000m, "Salary", null, ("Debit", 4000m, bankId, null), ("Credit", 4000m, null, salary)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        var expense = await Post(client, "Expense", 1500m, "Groceries", null, ("Debit", 1500m, null, household), ("Credit", 1500m, bankId, null));
        expense.StatusCode.Should().Be(HttpStatusCode.Created);
        var expenseId = (await expense.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await Post(client, "Transfer", 1000m, "Cash withdrawal", null, ("Debit", 1000m, cashId, null), ("Credit", 1000m, bankId, null)))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var propertyId = await CreateProperty(client);
        (await Post(client, "PropertyPurchase", 2000m, "Missing property", null, ("Debit", 2000m, null, purchase), ("Credit", 2000m, bankId, null)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Post(client, "PropertyPurchase", 2000m, "Chitnahalli", propertyId, ("Debit", 2000m, null, purchase), ("Credit", 2000m, bankId, null)))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var bank = await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}");
        var cash = await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{cashId}");
        bank!.CurrentBalance.Should().Be(9500m);
        cash!.CurrentBalance.Should().Be(1500m);

        var reversed = await client.PostAsJsonAsync($"/api/transactions/{expenseId}/reverse", new { description = "Wrong shop" });
        reversed.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance.Should().Be(11000m);

        var original = await client.GetFromJsonAsync<TransactionItem>($"/api/transactions/{expenseId}");
        original!.Status.Should().Be("Reversed");
        var again = await client.PostAsJsonAsync($"/api/transactions/{expenseId}/reverse", new { description = "Again" });
        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var deleted = await client.DeleteAsync($"/api/transactions/{expenseId}");
        deleted.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await client.GetAsync($"/api/transactions/{expenseId}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var missingReason = await client.PostAsJsonAsync("/api/transactions/adjustments", new
        {
            transactionDate = "2026-06-02",
            amount = 250,
            description = "Correction",
            reason = "",
            lines = new object[]
            {
                new { lineType = "Debit", amount = 250, accountId = bankId },
                new { lineType = "Credit", amount = 250, categoryId = salary }
            }
        });
        missingReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var adjustment = await client.PostAsJsonAsync("/api/transactions/adjustments", new
        {
            transactionDate = "2026-06-02",
            amount = 250,
            description = "Opening correction",
            reason = "The passbook opening balance was short by 250.",
            lines = new object[]
            {
                new { lineType = "Debit", amount = 250, accountId = bankId },
                new { lineType = "Credit", amount = 250, categoryId = salary }
            }
        });
        adjustment.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.GetFromJsonAsync<AccountItem>($"/api/accounts/{bankId}"))!.CurrentBalance.Should().Be(11250m);

        var filtered = await client.GetFromJsonAsync<List<TransactionItem>>($"/api/transactions?accountId={bankId}&from=2026-06-01&to=2026-06-01");
        filtered!.Should().NotBeEmpty();
        filtered.Should().OnlyContain(item => item.TransactionDate == "2026-06-01");

        var intruder = await AuthenticatedClient("ledger-other@landwealth.test");
        (await intruder.GetAsync($"/api/transactions/{expenseId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private static async Task<Guid> CreateAccount(HttpClient client, string name, string accountType, decimal openingBalance, string? lastFourDigits)
    {
        var response = await client.PostAsJsonAsync("/api/accounts", new { name, accountType, openingBalance, lastFourDigits, institution = "HDFC Bank" });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateProperty(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            name = "Chitnahalli Farm",
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> Post(
        HttpClient client,
        string transactionType,
        decimal amount,
        string description,
        Guid? propertyId,
        params (string LineType, decimal Amount, Guid? AccountId, Guid? CategoryId)[] lines)
    {
        return client.PostAsJsonAsync("/api/transactions", new
        {
            transactionDate = "2026-06-01",
            transactionType,
            amount,
            description,
            propertyId,
            lines = lines.Select(line => new
            {
                lineType = line.LineType,
                amount = line.Amount,
                accountId = line.AccountId,
                categoryId = line.CategoryId,
                propertyId
            }).ToArray()
        });
    }

    private sealed record CategoryItem(Guid Id, string Name);
    private sealed record AccountItem(decimal CurrentBalance);
    private sealed record TransactionItem(string Status, string TransactionDate);
}
