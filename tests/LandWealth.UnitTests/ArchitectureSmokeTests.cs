using FluentAssertions;
using Xunit;

namespace LandWealth.UnitTests;

public class ArchitectureSmokeTests
{
    [Fact]
    public void DomainAssembly_ShouldNotReference_ApplicationOrInfrastructure()
    {
        var domainAssembly = typeof(LandWealth.Domain.AssemblyReference).Assembly;
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        referencedAssemblies.Should().NotContain(a => a.Name == "LandWealth.Application");
        referencedAssemblies.Should().NotContain(a => a.Name == "LandWealth.Infrastructure");
        referencedAssemblies.Should().NotContain(a => a.Name == "LandWealth.Api");
    }

    [Fact]
    public void ApplicationAssembly_ShouldNotReference_InfrastructureOrApi()
    {
        var applicationAssembly = typeof(LandWealth.Application.AssemblyReference).Assembly;
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies();

        referencedAssemblies.Should().NotContain(a => a.Name == "LandWealth.Infrastructure");
        referencedAssemblies.Should().NotContain(a => a.Name == "LandWealth.Api");
    }
}
