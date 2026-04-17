using AutoMapper;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Api.Tests.Helpers;

/// <summary>
/// Provides a single, lazily-initialized <see cref="IMapper"/> instance for the entire
/// test assembly.
///
/// <para>
/// Each test class previously created its own <c>MapperConfiguration</c> in a
/// <c>static readonly</c> field.  Because xUnit runs test classes in parallel,
/// four static field initializers could call
/// <c>new MapperConfiguration(cfg => cfg.AddProfile&lt;ApiBroadcastingProfile&gt;())</c>
/// simultaneously.  AutoMapper 16 maintains an internal, per-profile-type static
/// registry; concurrent initialization of the same profile from separate
/// <see cref="MapperConfiguration"/> instances races against that registry and
/// produces intermittent test failures.
/// </para>
/// <para>
/// Sharing one instance removes the race entirely: the CLR guarantees that a
/// <c>static readonly</c> field is initialized exactly once, and AutoMapper mappers
/// are fully immutable and thread-safe after creation.
/// </para>
/// </summary>
public static class ApiTestMapper
{
    /// <summary>
    /// The shared mapper instance configured with <c>ApiBroadcastingProfile</c>.
    /// </summary>
    public static readonly IMapper Instance = new MapperConfiguration(
        cfg => cfg.AddProfile<JosephGuadagno.Broadcasting.Api.MappingProfiles.ApiBroadcastingProfile>(),
        new LoggerFactory())
        .CreateMapper();
}
