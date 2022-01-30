using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Domain.Interfaces;

public interface IEventPublisher
{
    public bool PublishEvents(string topicUrl, string topicKey, string subject, IReadOnlyCollection<SourceData> sourceDataItems);

    public Task<bool> PublishEventsAsync(string topicUrl, string topicKey, string subject,
        IReadOnlyCollection<SourceData> sourceDataItems);
}