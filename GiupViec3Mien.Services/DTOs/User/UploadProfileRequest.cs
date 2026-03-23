using System;
using System.Text.Json.Serialization;

namespace GiupViec3Mien.Services.DTOs.User;

public class UploadProfileRequest
{
    [JsonPropertyName("imgurl")]
    public string ImgUrl { get; set; } = string.Empty;
}
