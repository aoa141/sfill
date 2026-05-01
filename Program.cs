using System.Security.Cryptography;

namespace SFill;

class Program
{
    private const int BufferSize = 1024 * 1024; // 1 MB buffer
    private const string FillFileName = "sfill.tmp";

    static int Main(string[] args)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), FillFileName);

        Console.WriteLine($"sfill - Filling free space with random data");
        Console.WriteLine($"Target: {filePath}");
        Console.WriteLine("Press Ctrl+C to abort");
        Console.WriteLine();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\nAborting...");
            CleanupFile(filePath);
            Environment.Exit(1);
        };

        try
        {
            FillDrive(filePath);
            CleanupFile(filePath);
            Console.WriteLine("\nDrive filled successfully. Temporary file removed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\nError: {ex.Message}");
            CleanupFile(filePath);
            return 1;
        }
    }

    private static void FillDrive(string filePath)
    {
        var buffer = new byte[BufferSize];
        long totalWritten = 0;

        using var file = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.WriteThrough);

        while (true)
        {
            RandomNumberGenerator.Fill(buffer);

            try
            {
                file.Write(buffer);
                totalWritten += BufferSize;

                if (totalWritten % (100 * BufferSize) == 0)
                {
                    Console.Write($"\rWritten: {FormatBytes(totalWritten)}.           ");
                }
            }
            catch (IOException ex) when (IsDiskFullException(ex))
            {
                Console.WriteLine($"\rWritten: {FormatBytes(totalWritten)} - Disk full");
                break;
            }
        }
    }

    private static bool IsDiskFullException(IOException ex)
    {
        const int ERROR_DISK_FULL = unchecked((int)0x80070070);
        const int ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);

        return ex.HResult == ERROR_DISK_FULL || ex.HResult == ERROR_HANDLE_DISK_FULL;
    }

    private static void CleanupFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F2} {suffixes[suffixIndex]}";
    }
}
