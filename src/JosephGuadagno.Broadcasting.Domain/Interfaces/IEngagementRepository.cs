using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEngagementRepository : IDataRepository<Engagement>
{
    public Task<bool> AddTalkToEngagementAsync(Engagement engagement, Talk talk);
    public Task<bool> AddTalkToEngagementAsync(int engagementId, Talk talk);
    public Task<bool> SaveTalkAsync(Talk talk);
    public Task<bool> RemoveTalkFromEngagementAsync(int talkId);
    public Task<bool> RemoveTalkFromEngagementAsync(Talk talk);
    public Task<Talk> GetTalkAsync(int talkId);
}