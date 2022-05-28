using System.Collections.Generic;
using System.Linq;

namespace JosephGuadagno.Broadcasting.Domain;

public static class Scopes
{
    /// <summary>
    /// Contains the scopes for the Engagement API
    /// </summary>
    public static class Engagements
    {
        public static readonly string List = "Engagements.List";
        public static readonly string View = "Engagements.View";
        public static readonly string Delete = "Engagements.Delete";
        public static readonly string Modify = "Engagements.Modify";
        public static readonly string Add = "Engagements.Add";
        public static readonly string All = "Engagements.All";
        
        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { List, List },
                { View, View },
                { Delete, Delete },
                { Modify, Modify },
                { Add, Add },
                { All, All }
            };
        }
    }
    /// <summary>
    /// Contains the scopes for the Schedule API
    /// </summary>
    public static class Schedules
    {
        public static readonly string List = "Schedules.List";
        public static readonly string View = "Schedules.View";
        public static readonly string Delete = "Schedules.Delete";
        public static readonly string Modify = "Schedules.Modify";
        public static readonly string Add = "Schedules.Add";
        public static readonly string UnsentScheduled = "Schedules.UnsentScheduled";
        public static readonly string ScheduledToSend = "Schedules.ScheduledToSend";
        public static readonly string UpcomingScheduled = "Schedules.UpcomingScheduled";
        public static readonly string All = "Schedules.All";
        
        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { List, List },
                { View, View },
                { Delete, Delete },
                { Modify, Modify },
                { Add, Add },
                { UnsentScheduled, UnsentScheduled },
                { ScheduledToSend, ScheduledToSend},
                { UpcomingScheduled, UpcomingScheduled},
                { All, All}
            };
        }
    }
    /// <summary>
    /// Contains the scopes for the Talk API
    /// </summary>
    public static class Talks
    {
        public static readonly string List = "Talks.List";
        public static readonly string View = "Talks.View";
        public static readonly string Delete = "Talks.Delete";
        public static readonly string Modify = "Talks.Modify";
        public static readonly string Add = "Talks.Add";
        public static readonly string All = "Talks.All";

        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { List, List },
                { View, View },
                { Delete, Delete },
                { Modify, Modify },
                { Add, Add },
                { All, All}
            };
        }
    }

    public static Dictionary<string, string> ToDictionary(string scopeUrl)
    {
        var allScopes = new Dictionary<string, string>();
        
        foreach (var scope in Engagements.ToDictionary())
        {
            allScopes.Add(scopeUrl + scope.Key, scope.Value);
        }

        foreach (var scope in Talks.ToDictionary())
        {
            allScopes.Add(scopeUrl + scope.Key, scope.Value);
        }
        
        foreach (var scope in Schedules.ToDictionary())
        {
            allScopes.Add(scopeUrl + scope.Key, scope.Value);
        }

        return allScopes;
    }

    public static Dictionary<string, string> AllAccessToDictionary(string scopeUri)
    {
        return new Dictionary<string, string>
        {
            { scopeUri + Engagements.All, Engagements.All },
            { scopeUri + Talks.All, Talks.All },
            { scopeUri + Schedules.All, Schedules.All }
        };
    }
}