using JosephGuadagno.Broadcasting.Domain;

namespace JosephGuadagno.Broadcasting.Twitter.Models
{
    public class ScheduledTweet: ScheduledSocialPost
    {
        public ScheduledTweet()
        {
            PartitionKey = "Twitter";
        }
    }
}