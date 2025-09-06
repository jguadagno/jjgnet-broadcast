using AutoMapper;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class MappingTests
{
    [Fact]
    public void MappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<Sql.MappingProfiles.BroadcastingProfile>();
        });

        try
        {
            // Throws an exception is something is bad
            configuration.AssertConfigurationIsValid();
            Assert.True(true);
        }
        catch (Exception)
        {
            Assert.True(false);
        }
    }
}