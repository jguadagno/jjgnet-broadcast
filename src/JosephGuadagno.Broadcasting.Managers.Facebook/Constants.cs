using System.ComponentModel;

namespace JosephGuadagno.Broadcasting.Managers.Facebook;

public static class Constants
{
    public enum TokenTypes
    {
        [Description("short-lived")]
        ShortLived,
        [Description("long-lived")]
        LongLived,
        [Description("page")]
        Page
    }
}