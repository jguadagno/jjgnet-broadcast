namespace JosephGuadagno.Broadcasting.Domain;

public static class Scopes
{
    public static class MicrosoftGraph
    {
        public static readonly string UserRead = "User.Read";
        public static readonly string Email = "email";
        public static readonly string OpenId = "openid";
        public static readonly string Profile = "profile";
        public static readonly string OfflineAccess = "offline_access";
        public static readonly string UserImpersonation = "user_impersonation";
        
        public static Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { UserRead, UserRead },
                { Email, Email },
                { OpenId, OpenId },
                { Profile, Profile },
                { OfflineAccess, OfflineAccess },
                { UserImpersonation, UserImpersonation }
            };
        }
    }
}
