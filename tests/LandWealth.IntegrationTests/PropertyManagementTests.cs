using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class PropertyManagementTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public PropertyManagementTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LifecycleParcelsAndOwnership_FollowThePropertyRules()
    {
        var client = await AuthenticatedClient("parcels@landwealth.test");
        var created = await client.PostAsJsonAsync("/api/properties", new
        {
            name = "Chitnahalli 5-Acre Farm",
            propertyType = "AgriculturalLand",
            state = "Karnataka",
            village = "Chitnahalli",
            primarySurveyNumber = "42"
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var propertyId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/status", new { status = "Held" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/status", new { status = "Purchased" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var parcelResponse = await client.PostAsJsonAsync($"/api/properties/{propertyId}/parcels", new
        {
            surveyNumber = "42",
            subdivisionNumber = "1",
            extent = 3,
            extentUnit = "Acres",
            ownershipPercentage = 100,
            boundaryDescription = "North: road"
        });
        parcelResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parcelId = (await parcelResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var split = await client.PostAsJsonAsync($"/api/properties/{propertyId}/parcels/{parcelId}/subdivide", new
        {
            splitExtent = 1,
            remainderSubdivision = "1A",
            splitSubdivision = "1B"
        });
        split.StatusCode.Should().Be(HttpStatusCode.Created);

        var details = await client.GetFromJsonAsync<JsonElement>($"/api/properties/{propertyId}");
        details.GetProperty("status").GetString().Should().Be("Purchased");
        details.GetProperty("nextStatuses").EnumerateArray().Select(item => item.GetString())
            .Should().Contain(new[] { "Held", "Archived" });
        details.GetProperty("parcels").GetArrayLength().Should().Be(3);

        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/owners", new
        {
            ownerName = "Ashwath",
            ownershipPercentage = 60,
            ownershipType = "TenancyInCommon",
            startDate = "2024-01-01"
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        var overShare = await client.PostAsJsonAsync($"/api/properties/{propertyId}/owners", new
        {
            ownerName = "Sibling",
            ownershipPercentage = 50,
            ownershipType = "TenancyInCommon",
            startDate = "2024-01-01"
        });
        overShare.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var withOwners = await client.GetFromJsonAsync<JsonElement>($"/api/properties/{propertyId}");
        withOwners.GetProperty("activeOwnershipPercentage").GetDecimal().Should().Be(60);
        var ownerId = withOwners.GetProperty("owners").EnumerateArray().First().GetProperty("id").GetGuid();
        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/owners/{ownerId}/transfer", new { endDate = "2024-06-01" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.PostAsJsonAsync($"/api/properties/{propertyId}/owners", new
        {
            ownerName = "Sibling",
            ownershipPercentage = 50,
            ownershipType = "TenancyInCommon",
            startDate = "2024-06-02"
        })).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<HttpClient> AuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1!",
            fullName = "Parcel Owner"
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await register.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return client;
    }
}
