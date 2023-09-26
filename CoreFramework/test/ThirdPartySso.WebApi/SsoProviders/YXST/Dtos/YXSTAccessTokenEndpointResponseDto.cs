using System.Text.Json.Serialization;

namespace ThirdPartySso.WebApi.SsoProviders.YXST
{
    public class YXSTAccessTokenEndpointResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("resultMessage")]
        public string ResultMessage { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires")]
        public int Expires { get; set; }
    }
}
