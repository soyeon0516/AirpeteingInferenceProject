namespace inferenceclinet.Models;

public class CheckErrorMetaData
{
    public string? memo { get; set; } = null;
    public int[]? box { get; set; }
    public float trust { get; set; }
    public string? product { get; set; }
    public DateTime timestamp { get; set; }
}
