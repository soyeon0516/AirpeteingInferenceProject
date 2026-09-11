using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MainServer;

public sealed class Client
{
    public int ClientId { get; init; }

    public bool Processing { get; set; }

    public string? ProductName { get; set; }

    public string? SucessRate { get; set; }
}

public sealed class RequestMessage
{
    [JsonPropertyName("type")]
    [Required]
    public string Type { get; init; } = "request";

    [JsonPropertyName("client")]
    [Range(1, int.MaxValue)]
    public int Client { get; init; }

    [JsonPropertyName("filename")]
    [Required]
    public string Filename { get; init; } = string.Empty;

    [JsonPropertyName("filelastnumber")]
    [Range(0, int.MaxValue)]
    public int Filelastnumber { get; init; }

    [JsonPropertyName("filelength")]
    [Range(1, long.MaxValue)]
    public long Filelength { get; init; }

    [JsonPropertyName("filedata")]
    [Required]
    public string Filedata { get; init; } = string.Empty;
}

public sealed class ResponseMessage
{
    [JsonPropertyName("ClientId")]
    [Range(1, int.MaxValue)]
    public int ClientId { get; init; }

    [JsonPropertyName("ProductName")]
    [Required]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("SucessRate")]
    [Required]
    public string SucessRate { get; init; } = string.Empty;

    // 최고 신뢰도 검출의 confidence (0~1). 미검출 시 null.
    [JsonPropertyName("Confidence")]
    public double? Confidence { get; init; }

    // 최고 신뢰도 검출의 바운딩박스 [x1, y1, x2, y2] (원본 이미지 픽셀 기준). 미검출 시 null.
    [JsonPropertyName("Box")]
    public double[]? Box { get; init; }
}

public sealed class MainResponseMessage
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "MainResponse";

    [JsonPropertyName("ClientId")]
    public int ClientId { get; init; }

    [JsonPropertyName("ProductName")]
    public string ProductName { get; init; } = string.Empty;

    [JsonPropertyName("SucessRate")]
    public string SucessRate { get; init; } = string.Empty;

    [JsonPropertyName("Confidence")]
    public double? Confidence { get; init; }

    [JsonPropertyName("Box")]
    public double[]? Box { get; init; }
}

public sealed class LoginClientMessage
{
    [JsonPropertyName("type")]
    [Required]
    public string Type { get; init; } = "login";

    [JsonPropertyName("id")]
    [Required]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("hashpassword")]
    [Required]
    public string HashPassword { get; init; } = string.Empty;
}

public sealed class LoginResponseMessage
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "loginResponse";

    [JsonPropertyName("HashPassword")]
    public string HashPassword { get; init; } = string.Empty;

    [JsonPropertyName("ID")]
    public string Id { get; init; } = string.Empty;
}

public sealed class DatabaseOptions
{
    public string Host { get; init; } = "127.0.0.1";

    public uint Port { get; init; } = 3307;

    public string Database { get; init; } = "inference_db";

    public string User { get; init; } = "inference_user";

    public string Password { get; init; } = string.Empty;
}


