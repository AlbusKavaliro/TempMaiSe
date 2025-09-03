namespace AlbusKavaliro.TempMaiSe.Tests;

public static class StreamExtensions
{
    public static bool EqualsBuffer(this Stream stream, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(data);

        if (stream.Length != data.Length)
        {
            return false;
        }

        byte[] buffer = new byte[stream.Length];
        stream.Read(buffer, 0, buffer.Length);
        return buffer.SequenceEqual(data);
    }
}
