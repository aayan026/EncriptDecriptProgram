using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main()
    {
        Console.Write("Fayl yolunu daxil et: ");
        string path = Console.ReadLine();

        if (!File.Exists(path))
        {
            Console.WriteLine("Fayl tapılmadı.");
            return;
        }

        Console.WriteLine("1 - Encrypt");
        Console.WriteLine("2 - Decrypt");
        int choice = int.Parse(Console.ReadLine());

        byte key = 0xAA;
        string backupPath = path + ".bak";
        File.Copy(path, backupPath, true); 

        Console.WriteLine("Başlamaq üçün Enter, dayandırmaq üçün 'c' bas.");
        Console.ReadLine();

        long total = new FileInfo(path).Length;
        long processed = 0;
        bool cancelled = false;

        CancellationTokenSource cts = new CancellationTokenSource();
        Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.C)
                {
                    cts.Cancel();
                    break;
                }
                Thread.Sleep(50);
            }
        });

        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
        {
            byte[] buffer = new byte[1024];
            int read;
            while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                int subBlock = 10; // kiçik hissələrə bölünmüş yazma
                for (int i = 0; i < read; i += subBlock)
                {
                    int chunk = Math.Min(subBlock, read - i);

                    for (int j = 0; j < chunk; j++)
                        buffer[i + j] ^= key;

                    fs.Seek(i, SeekOrigin.Current);
                    fs.Write(buffer, i, chunk);

                    processed += chunk;
                    DrawProgressBar((double)processed / total, 50);

                    Thread.Sleep(200); 
                    if (cts.Token.IsCancellationRequested)
                    {
                        cancelled = true;
                        break;
                    }
                }

                if (cancelled) break;
            }
        }

        if (cancelled)
        {
            Console.WriteLine("\nİş dayandırıldı, fayl bərpa olunur...");
            File.Copy(backupPath, path, true);
            DrawProgressBar(0, 50);
        }
        else
        {
            Console.WriteLine("\nTamamlandı!");
            File.Delete(backupPath);
        }
    }

    static void DrawProgressBar(double progress, int size)
    {
        int filled = (int)(progress * size);
        Console.CursorLeft = 0;
        Console.Write("[");
        Console.Write(new string('█', filled));
        Console.Write(new string('-', size - filled));
        Console.Write($"] {progress:P0}");
    }
}
