using AutoMapper;

namespace JosephGuadagno.Broadcasting.Web.Tests;

public class MappingTests
{
    [Fact]
    public void MappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.WebMappingProfile>();
        });

        try
        {
            // Throws an exception is something is bad
            configuration.AssertConfigurationIsValid();
            Assert.True(true);
        }
        catch (Exception ex)
        {
            Assert.True(false);
        }
    }
}
