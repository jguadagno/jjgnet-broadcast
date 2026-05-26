using System.Threading;
using System.Threading.Tasks;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

/// <summary>
/// Manages the <c>IsOnboarded</c> flag on <c>ApplicationUser</c>.
/// Recomputes and persists onboarding completeness whenever the user's
/// collectors, publishers, or message templates change.
/// </summary>
public interface IOnboardingManager
{
    /// <summary>
    /// Recomputes the onboarding status for the specified user, updates the database,
    /// and evicts the claims cache so the next request picks up the updated flag.
    /// </summary>
    /// <param name="entraOid">The Microsoft Entra object ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecalculateAsync(string entraOid, CancellationToken cancellationToken = default);
}
