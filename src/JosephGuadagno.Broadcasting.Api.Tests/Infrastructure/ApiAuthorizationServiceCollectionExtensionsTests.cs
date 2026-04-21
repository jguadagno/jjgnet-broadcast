using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Infrastructure;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Managers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Infrastructure;

public class ApiAuthorizationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBroadcastingApiAuthorization_RegistersSharedClaimsTransformation()
    {
        var services = CreateServices();

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        using var scope = serviceProvider.CreateScope();

        var transformation = scope.ServiceProvider.GetRequiredService<IClaimsTransformation>();

        transformation.Should().BeOfType<EntraClaimsTransformation>();
    }

    [Theory]
    [InlineData(AuthorizationPolicyNames.RequireSiteAdministrator, RoleNames.SiteAdministrator)]
    [InlineData(AuthorizationPolicyNames.RequireAdministrator, RoleNames.SiteAdministrator, RoleNames.Administrator)]
    [InlineData(AuthorizationPolicyNames.RequireContributor, RoleNames.SiteAdministrator, RoleNames.Administrator, RoleNames.Contributor)]
    [InlineData(AuthorizationPolicyNames.RequireViewer, RoleNames.SiteAdministrator, RoleNames.Administrator, RoleNames.Contributor, RoleNames.Viewer)]
    public void AddBroadcastingApiAuthorization_RegistersHierarchicalPolicies(
        string policyName,
        params string[] expectedRoles)
    {
        var services = CreateServices();

        using var serviceProvider = services.BuildServiceProvider();
        var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        var policy = authorizationOptions.GetPolicy(policyName);

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<RolesAuthorizationRequirement>()
            .Which.AllowedRoles.Should().BeEquivalentTo(expectedRoles);
    }

    [Fact]
    public async Task AddBroadcastingApiAuthorization_ClaimsTransformation_AddsRoleClaimsWithoutDroppingScopeClaims()
    {
        const string entraObjectId = "entra-oid-12345";
        const string scopeValue = "api://test-client/access_as_user";
        var approvedUser = new ApplicationUser
        {
            Id = 42,
            EntraObjectId = entraObjectId,
            DisplayName = "API Admin",
            Email = "api-admin@example.com",
            ApprovalStatus = nameof(ApprovalStatus.Approved),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var userApprovalManager = new Mock<IUserApprovalManager>();
        userApprovalManager
            .Setup(manager => manager.GetOrCreateUserAsync(entraObjectId, approvedUser.DisplayName, approvedUser.Email))
            .ReturnsAsync(approvedUser);
        userApprovalManager
            .Setup(manager => manager.GetUserRolesAsync(approvedUser.Id))
            .ReturnsAsync(
            [
                new Role
                {
                    Id = 7,
                    Name = RoleNames.SiteAdministrator,
                    Description = "Site administrator"
                }
            ]);

        var services = CreateServices(userApprovalManager);

        using var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        using var scope = serviceProvider.CreateScope();

        var transformation = scope.ServiceProvider.GetRequiredService<IClaimsTransformation>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ApplicationClaimTypes.EntraObjectId, entraObjectId),
            new Claim(ClaimTypes.Name, approvedUser.DisplayName),
            new Claim(ClaimTypes.Email, approvedUser.Email),
            new Claim("scp", scopeValue)
        ], "TestAuthentication"));

        var transformed = await transformation.TransformAsync(principal);

        transformed.FindFirst("scp")!.Value.Should().Be(scopeValue);
        transformed.FindFirst(ApplicationClaimTypes.ApprovalStatus)!.Value.Should().Be(nameof(ApprovalStatus.Approved));
        transformed.FindAll(ClaimTypes.Role).Select(claim => claim.Value)
            .Should().Contain(RoleNames.SiteAdministrator);
    }

    private static ServiceCollection CreateServices(Mock<IUserApprovalManager>? userApprovalManager = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => (userApprovalManager ?? CreateDefaultUserApprovalManager()).Object);
        services.AddBroadcastingApiAuthorization();

        return services;
    }

    private static Mock<IUserApprovalManager> CreateDefaultUserApprovalManager()
    {
        var userApprovalManager = new Mock<IUserApprovalManager>();
        userApprovalManager
            .Setup(manager => manager.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 1,
                EntraObjectId = "default-oid",
                DisplayName = "Default User",
                Email = "default@example.com",
                ApprovalStatus = nameof(ApprovalStatus.Pending),
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        userApprovalManager
            .Setup(manager => manager.GetUserRolesAsync(It.IsAny<int>()))
            .ReturnsAsync([]);

        return userApprovalManager;
    }
}
