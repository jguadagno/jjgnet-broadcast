namespace JosephGuadagno.Broadcasting.Domain;

public static class Scopes
{
    /// <summary>
    /// Contains the scopes for the Engagement API
    /// </summary>
    public static class Engagements
    {
        public static readonly string Add = "Engagements.Add";
        public static readonly string All = "Engagements.All";
        public static readonly string Delete = "Engagements.Delete";
        public static readonly string List = "Engagements.List";
        public static readonly string Modify = "Engagements.Modify";
        public static readonly string View = "Engagements.View";
        
        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { Add, Add },
                { All, All },
                { Delete, Delete },
                { List, List },
                { Modify, Modify },
                { View, View }
            };
        }
    }

    /// <summary>
    /// Contains the scopes for the MessageTemplate API
    /// </summary>
    public static class MessageTemplates
    {
        public static readonly string All = "MessageTemplates.All";
        public static readonly string List = "MessageTemplates.List";
        public static readonly string Modify = "MessageTemplates.Modify";
        public static readonly string View = "MessageTemplates.View";

        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { All, All },
                { List, List },
                { Modify, Modify },
                { View, View }
            };
        }
    }

    /// <summary>
    /// Contains the scopes for the SocialMediaPlatform API
    /// </summary>
    public static class SocialMediaPlatforms
    {
        public static readonly string Add = "SocialMediaPlatforms.Add";
        public static readonly string All = "SocialMediaPlatforms.All";
        public static readonly string Delete = "SocialMediaPlatforms.Delete";
        public static readonly string List = "SocialMediaPlatforms.List";
        public static readonly string Modify = "SocialMediaPlatforms.Modify";
        public static readonly string View = "SocialMediaPlatforms.View";

        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { Add, Add },
                { All, All },
                { Delete, Delete },
                { List, List },
                { Modify, Modify },
                { View, View }
            };
        }
    }

    /// <summary>
    /// Contains the scopes for the UserPublisherSettings API
    /// </summary>
    public static class UserPublisherSettings
    {
        public static readonly string All = "UserPublisherSettings.All";
        public static readonly string Delete = "UserPublisherSettings.Delete";
        public static readonly string List = "UserPublisherSettings.List";
        public static readonly string Modify = "UserPublisherSettings.Modify";
        public static readonly string View = "UserPublisherSettings.View";

        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { All, All },
                { Delete, Delete },
                { List, List },
                { Modify, Modify },
                { View, View }
            };
        }
    }

    /// <summary>
    /// Contains the scopes for the Schedule API
    /// </summary>
    public static class Schedules
    {
        public static readonly string Add = "Schedules.Add";
        public static readonly string All = "Schedules.All";
        public static readonly string Delete = "Schedules.Delete";
        public static readonly string List = "Schedules.List";
        public static readonly string Modify = "Schedules.Modify";
        public static readonly string ScheduledToSend = "Schedules.ScheduledToSend";
        public static readonly string UpcomingScheduled = "Schedules.UpcomingScheduled";
        public static readonly string UnsentScheduled = "Schedules.UnsentScheduled";
        public static readonly string View = "Schedules.View";

        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { Add, Add },
                { All, All},
                { Delete, Delete },
                { List, List },
                { Modify, Modify },
                { ScheduledToSend, ScheduledToSend},
                { UpcomingScheduled, UpcomingScheduled},
                { UnsentScheduled, UnsentScheduled },
                { View, View }
            };
        }
    }

    /// <summary>
    /// Contains the scopes for the Talk API
    /// </summary>
    public static class Talks
    {
        public static readonly string Add = "Talks.Add";
        public static readonly string All = "Talks.All";
        public static readonly string Delete = "Talks.Delete";
        public static readonly string List = "Talks.List";
        public static readonly string Modify = "Talks.Modify";
        public static readonly string View = "Talks.View";

        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { Add, Add },
                { All, All},
                { Delete, Delete },
                { List, List },
                { Modify, Modify },
                { View, View }
            };
        }
    }

    public static class MicrosoftGraph
    {
        public static readonly string UserRead = "User.Read";
        public static readonly string Email = "email";
        public static readonly string OfflineAccess = "offline_access";
        public static readonly string OpenId = "openid";
        public static readonly string Profile = "profile";
        public static readonly string UserImpersonation = "user_impersonation";

        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { UserRead, UserRead },
                { Email, Email },
                { OfflineAccess, OfflineAccess },
                { OpenId, OpenId },
                { Profile, Profile },
                { UserImpersonation, UserImpersonation}
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

        foreach (var scope in MessageTemplates.ToDictionary())
        {
            allScopes.Add(scopeUrl + scope.Key, scope.Value);
        }

        foreach (var scope in SocialMediaPlatforms.ToDictionary())
        {
            allScopes.Add(scopeUrl + scope.Key, scope.Value);
        }

        foreach (var scope in UserPublisherSettings.ToDictionary())
        {
            allScopes.Add(scopeUrl + scope.Key, scope.Value);
        }

        return allScopes;
    }

    public static Dictionary<string, string> AllAccessToDictionary(string scopeUri)
    {
        var scopes = new Dictionary<string, string>
        {
            { scopeUri + Engagements.All, Engagements.All },
            { scopeUri + Talks.All, Talks.All },
            { scopeUri + Schedules.All, Schedules.All },
            { scopeUri + MessageTemplates.All, MessageTemplates.All },
            { scopeUri + SocialMediaPlatforms.All, SocialMediaPlatforms.All },
            { scopeUri + UserPublisherSettings.All, UserPublisherSettings.All }
        };

        return scopes;
    }
}
