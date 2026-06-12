using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace LoteriaMexicanaApp.UI
{
    public static class ImageCache
    {
        private static readonly Dictionary<int, Image> _cache = new Dictionary<int, Image>();
        private static readonly string _folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CardImagesOriginal");
        private static readonly HttpClient _httpClient = new HttpClient();

        public static event Action? ImageDownloaded;
        public static bool IsDownloading { get; private set; }

        static ImageCache()
        {
            try
            {
                if (!Directory.Exists(_folderPath))
                {
                    Directory.CreateDirectory(_folderPath);
                }
                PreloadFromDisk();
            }
            catch
            {
                // Fallback or ignore directory errors
            }
        }

        private static void PreloadFromDisk()
        {
            for (int i = 1; i <= 54; i++)
            {
                string filePath = Path.Combine(_folderPath, $"{i}.jpg");
                if (File.Exists(filePath))
                {
                    try
                      {
                        // Using FileStream and Image.FromStream prevents GDI+ from locking the file on disk
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            _cache[i] = Image.FromStream(stream);
                        }
                    }
                    catch
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the cached image for the given card ID, or null if not yet available.
        /// </summary>
        public static Image? GetCardImage(int id)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(id, out var img))
                {
                    return img;
                }
            }
            return null;
        }

        /// <summary>
        /// Downloads missing card images in the background.
        /// </summary>
        public static void StartDownloadBackground()
        {
            if (IsDownloading) return;
            IsDownloading = true;

            Task.Run(async () =>
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                for (int i = 1; i <= 54; i++)
                {
                    string filePath = Path.Combine(_folderPath, $"{i}.jpg");
                    
                    lock (_cache)
                    {
                        if (_cache.ContainsKey(i)) continue;
                    }

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                            {
                                var img = Image.FromStream(stream);
                                lock (_cache)
                                {
                                    _cache[i] = img;
                                }
                            }
                            ImageDownloaded?.Invoke();
                            continue;
                        }
                        catch
                        {
                            try { File.Delete(filePath); } catch { }
                        }
                    }

                    // Download classic Don Clemente Jacques cards from fcopaveltorres/LoteriaMexicana
                    string url = $"https://raw.githubusercontent.com/fcopaveltorres/LoteriaMexicana/master/cartas/{i}.jpg";
                    try
                    {
                        byte[] data = await _httpClient.GetByteArrayAsync(url);
                        await File.WriteAllBytesAsync(filePath, data);

                        using (var ms = new MemoryStream(data))
                        {
                            var img = Image.FromStream(ms);
                            lock (_cache)
                            {
                                _cache[i] = img;
                            }
                        }

                        // Trigger redraw event on UI thread
                        ImageDownloaded?.Invoke();
                    }
                    catch
                    {
                        // Ignore individual download errors, can retry later
                    }
                }
                IsDownloading = false;
            });
        }
    }
}
