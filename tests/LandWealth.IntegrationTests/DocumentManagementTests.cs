using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace LandWealth.IntegrationTests;

public class DocumentManagementTests : IClassFixture<LandWealthApiFactory>
{
    private readonly LandWealthApiFactory _factory;

    public DocumentManagementTests(LandWealthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Documents_StoreById_AndKeepMetadataOffTheFilePath()
    {
        var owner = await AuthenticatedClient("documents@landwealth.test");
        var propertyId = await CreateProperty(owner, "Deed Farm");
        var otherPropertyId = await CreateProperty(owner, "Other Farm");
        var parcel = await owner.PostAsJsonAsync($"/api/properties/{otherPropertyId}/parcels", new
        {
            surveyNumber = "99",
            extent = 1,
            extentUnit = "Acres",
            ownershipPercentage = 100
        });
        var otherParcelId = (await parcel.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var mismatched = new MultipartFormDataContent();
        var png = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 });
        png.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        mismatched.Add(png, "file", "pretend.pdf");
        mismatched.Add(new StringContent("SaleDeed"), "documentType");
        (await owner.PostAsync($"/api/properties/{propertyId}/documents", mismatched)).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var wrongParcel = PdfContent("deed.pdf");
        wrongParcel.Add(new StringContent(otherParcelId.ToString()), "parcelId");
        (await owner.PostAsync($"/api/properties/{propertyId}/documents", wrongParcel)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var upload = PdfContent("sale-deed.pdf");
        upload.Add(new StringContent("SR-42"), "documentNumber");
        upload.Add(new StringContent("2026-04-01"), "issueDate");
        upload.Add(new StringContent("Registered sale deed"), "notes");
        var uploaded = await owner.PostAsync($"/api/properties/{propertyId}/documents", upload);
        uploaded.StatusCode.Should().Be(HttpStatusCode.Created);
        var documentId = (await uploaded.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var listJson = await (await owner.GetAsync($"/api/properties/{propertyId}/documents")).Content.ReadAsStringAsync();
        listJson.Should().Contain("sale-deed.pdf");
        listJson.Should().Contain("SR-42");
        listJson.Should().NotContain("storageKey");
        listJson.Should().NotContain("storage");

        var download = await owner.GetAsync($"/api/documents/{documentId}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        download.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var body = await download.Content.ReadAsByteArrayAsync();
        body.Should().StartWith("%PDF"u8.ToArray());

        var intruder = await AuthenticatedClient("documents-other@landwealth.test");
        (await intruder.GetAsync($"/api/properties/{propertyId}/documents")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.GetAsync($"/api/documents/{documentId}/download")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static MultipartFormDataContent PdfContent(string fileName)
    {
        var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent("%PDF-1.4\n1 0 obj\n<<>>\nendobj\n"u8.ToArray());
        bytes.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(bytes, "file", fileName);
        content.Add(new StringContent("SaleDeed"), "documentType");
        return content;
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

    private static async Task<Guid> CreateProperty(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/properties", new
        {
            name,
            propertyType = "AgriculturalLand",
            state = "Karnataka"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}
