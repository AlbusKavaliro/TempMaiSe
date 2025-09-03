namespace AlbusKavaliro.TempMaiSe.Models;

public class Attachment
{
    public required string FileName { get; set; }

    public required string MediaType { get; set; } = "application/octet-stream";

#pragma warning disable CA1819 // Properties should not return arrays
    public required byte[] Data { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
}
