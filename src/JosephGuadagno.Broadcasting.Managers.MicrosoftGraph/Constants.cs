using System.ComponentModel;

namespace JosephGuadagno.Broadcasting.Managers.MicrosoftGraph;

public static class Constants
{
    public enum TokenTypes
    {
        [Description("web-client-secret")]
        WebClientSecret,
        [Description("api-client-secret")]
        ApiClientSecret
    }
}