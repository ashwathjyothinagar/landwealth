using FluentAssertions;
using LandWealth.Application.Common;
using LandWealth.Domain.Exceptions;

namespace LandWealth.UnitTests;

public class DocumentFileTests
{
    [Fact]
    public void Signatures_AcceptJpegAndRejectAPdfDeclaredAsPng()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
        FileSignatures.Matches("image/jpeg", jpeg).Should().BeTrue();
        FileSignatures.Matches("image/png", "%PDF-1.4"u8).Should().BeFalse();
    }

    [Fact]
    public void FileName_RejectsAPath()
    {
        DocumentFiles.RequireSafeFileName("sale-deed.pdf").Should().Be("sale-deed.pdf");
        var act = () => DocumentFiles.RequireSafeFileName(@"..\secret.pdf");
        act.Should().Throw<DomainException>();
    }
}
