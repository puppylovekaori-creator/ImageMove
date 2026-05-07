using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageMove
{
    internal sealed class ThumbnailCacheManager : IDisposable
    {
        private const int DefaultMemoryEntryLimit = 384;
        private const long DefaultMemoryBytesLimit = 256L * 1024L * 1024L;

        private readonly string cacheRootPath;
        private readonly object sync = new object();
        private readonly Dictionary<string, ThumbnailCacheEntry> memoryEntries = new Dictionary<string, ThumbnailCacheEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, LinkedListNode<string>> memoryNodes = new Dictionary<string, LinkedListNode<string>>(StringComparer.Ordinal);
        private readonly LinkedList<string> lruKeys = new LinkedList<string>();
        private long cachedBytes;
        private bool disposed;

        internal ThumbnailCacheManager(string cacheRootPath)
        {
            this.cacheRootPath = string.IsNullOrWhiteSpace(cacheRootPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageMove", "thumbnail_cache")
                : cacheRootPath;
            Directory.CreateDirectory(this.cacheRootPath);
        }

        internal string CacheRootPath => cacheRootPath;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            lock (sync)
            {
                foreach (ThumbnailCacheEntry entry in memoryEntries.Values)
                {
                    entry.Bitmap.Dispose();
                }

                memoryEntries.Clear();
                memoryNodes.Clear();
                lruKeys.Clear();
                cachedBytes = 0;
            }
        }

        internal void Clear()
        {
            ThrowIfDisposed();

            lock (sync)
            {
                foreach (ThumbnailCacheEntry entry in memoryEntries.Values)
                {
                    entry.Bitmap.Dispose();
                }

                memoryEntries.Clear();
                memoryNodes.Clear();
                lruKeys.Clear();
                cachedBytes = 0;
            }

            if (Directory.Exists(cacheRootPath))
            {
                try
                {
                    Directory.Delete(cacheRootPath, true);
                }
                catch
                {
                    // ロック中のキャッシュがある場合は無視する
                }
            }

            Directory.CreateDirectory(cacheRootPath);
        }

        internal async Task<ThumbnailLoadResult> GetThumbnailAsync(GridReviewItemRecord item, int thumbnailBoxSize, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (item == null || string.IsNullOrWhiteSpace(item.CurrentPath))
            {
                return ThumbnailLoadResult.Empty;
            }

            string fullPath = item.CurrentPath;
            if (!File.Exists(fullPath))
            {
                return ThumbnailLoadResult.Empty;
            }

            FileInfo fileInfo = new FileInfo(fullPath);
            string cacheKey = BuildCacheKey(fullPath, fileInfo.Length, fileInfo.LastWriteTimeUtc, thumbnailBoxSize);

            if (TryGetMemoryCache(cacheKey, out Bitmap cachedBitmap))
            {
                return new ThumbnailLoadResult(new Bitmap(cachedBitmap), Size.Empty, true);
            }

            string diskPath = BuildDiskCachePath(cacheKey);
            if (File.Exists(diskPath))
            {
                Bitmap bitmapFromDisk = await Task.Run(() => LoadBitmapCopy(diskPath), cancellationToken).ConfigureAwait(false);
                try
                {
                    AddMemoryCache(cacheKey, bitmapFromDisk);
                    return new ThumbnailLoadResult(new Bitmap(bitmapFromDisk), Size.Empty, true);
                }
                finally
                {
                    bitmapFromDisk.Dispose();
                }
            }

            return await Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sourceImage = Image.FromStream(fileStream, false, false))
                    {
                        Size sourceSize = sourceImage.Size;
                        Bitmap thumbnail = RenderThumbnail(sourceImage, thumbnailBoxSize);
                        try
                        {
                            SaveThumbnailToDiskSafe(diskPath, thumbnail);
                            AddMemoryCache(cacheKey, thumbnail);
                            return new ThumbnailLoadResult(new Bitmap(thumbnail), sourceSize, false);
                        }
                        finally
                        {
                            thumbnail.Dispose();
                        }
                    }
                },
                cancellationToken).ConfigureAwait(false);
        }

        private static Bitmap RenderThumbnail(Image sourceImage, int thumbnailBoxSize)
        {
            int safeSize = Math.Max(64, thumbnailBoxSize);
            var bitmap = new Bitmap(safeSize, safeSize, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(248, 248, 248));
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                Size drawSize = CalculateFitSize(sourceImage.Size, safeSize, safeSize);
                int drawX = (safeSize - drawSize.Width) / 2;
                int drawY = (safeSize - drawSize.Height) / 2;
                graphics.DrawImage(sourceImage, new Rectangle(drawX, drawY, drawSize.Width, drawSize.Height));
                graphics.DrawRectangle(Pens.Gainsboro, 0, 0, safeSize - 1, safeSize - 1);
            }

            return bitmap;
        }

        private static Size CalculateFitSize(Size sourceSize, int maxWidth, int maxHeight)
        {
            if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
            {
                return new Size(Math.Max(1, maxWidth), Math.Max(1, maxHeight));
            }

            double ratio = Math.Min((double)maxWidth / sourceSize.Width, (double)maxHeight / sourceSize.Height);
            int width = Math.Max(1, (int)Math.Round(sourceSize.Width * ratio));
            int height = Math.Max(1, (int)Math.Round(sourceSize.Height * ratio));
            return new Size(width, height);
        }

        private static Bitmap LoadBitmapCopy(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var image = Image.FromStream(fileStream, false, false))
            {
                return new Bitmap(image);
            }
        }

        private static string BuildCacheKey(string path, long fileLength, DateTime lastWriteTimeUtc, int thumbnailBoxSize)
        {
            string raw = string.Concat(
                path.Trim().ToLowerInvariant(),
                "|",
                fileLength,
                "|",
                lastWriteTimeUtc.Ticks,
                "|",
                thumbnailBoxSize);

            using (var sha1 = SHA1.Create())
            {
                byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (byte currentByte in bytes)
                {
                    builder.Append(currentByte.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private string BuildDiskCachePath(string cacheKey)
        {
            string level1 = cacheKey.Substring(0, 2);
            string level2 = cacheKey.Substring(2, 2);
            string directory = Path.Combine(cacheRootPath, level1, level2);
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, cacheKey + ".png");
        }

        private void SaveThumbnailToDiskSafe(string path, Bitmap bitmap)
        {
            try
            {
                bitmap.Save(path, ImageFormat.Png);
            }
            catch
            {
                // キャッシュ保存失敗は処理続行
            }
        }

        private bool TryGetMemoryCache(string cacheKey, out Bitmap bitmap)
        {
            lock (sync)
            {
                if (!memoryEntries.TryGetValue(cacheKey, out ThumbnailCacheEntry entry))
                {
                    bitmap = null;
                    return false;
                }

                TouchMemoryNode(cacheKey);
                bitmap = entry.Bitmap;
                return true;
            }
        }

        private void AddMemoryCache(string cacheKey, Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return;
            }

            lock (sync)
            {
                if (memoryEntries.ContainsKey(cacheKey))
                {
                    TouchMemoryNode(cacheKey);
                    return;
                }

                var node = lruKeys.AddLast(cacheKey);
                memoryNodes[cacheKey] = node;
                memoryEntries[cacheKey] = new ThumbnailCacheEntry(new Bitmap(bitmap), EstimateBitmapBytes(bitmap));
                cachedBytes += memoryEntries[cacheKey].EstimatedBytes;
                TrimMemoryCacheIfNeeded();
            }
        }

        private void TouchMemoryNode(string cacheKey)
        {
            if (!memoryNodes.TryGetValue(cacheKey, out LinkedListNode<string> node) || ReferenceEquals(node, lruKeys.Last))
            {
                return;
            }

            lruKeys.Remove(node);
            lruKeys.AddLast(node);
        }

        private void TrimMemoryCacheIfNeeded()
        {
            while (memoryEntries.Count > DefaultMemoryEntryLimit || cachedBytes > DefaultMemoryBytesLimit)
            {
                LinkedListNode<string> node = lruKeys.First;
                if (node == null)
                {
                    break;
                }

                string cacheKey = node.Value;
                lruKeys.RemoveFirst();
                memoryNodes.Remove(cacheKey);
                if (!memoryEntries.TryGetValue(cacheKey, out ThumbnailCacheEntry entry))
                {
                    continue;
                }

                memoryEntries.Remove(cacheKey);
                cachedBytes = Math.Max(0, cachedBytes - entry.EstimatedBytes);
                entry.Bitmap.Dispose();
            }
        }

        private static long EstimateBitmapBytes(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return 0;
            }

            return Math.Max(1L, (long)bitmap.Width * bitmap.Height * 4L);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ThumbnailCacheManager));
            }
        }
    }

    internal sealed class ThumbnailLoadResult
    {
        internal static readonly ThumbnailLoadResult Empty = new ThumbnailLoadResult(null, Size.Empty, false);

        internal ThumbnailLoadResult(Bitmap thumbnail, Size pixelSize, bool loadedFromCache)
        {
            Thumbnail = thumbnail;
            PixelSize = pixelSize;
            LoadedFromCache = loadedFromCache;
        }

        internal Bitmap Thumbnail { get; }

        internal Size PixelSize { get; }

        internal bool LoadedFromCache { get; }
    }

    internal sealed class ThumbnailCacheEntry
    {
        internal ThumbnailCacheEntry(Bitmap bitmap, long estimatedBytes)
        {
            Bitmap = bitmap;
            EstimatedBytes = estimatedBytes;
        }

        internal Bitmap Bitmap { get; }

        internal long EstimatedBytes { get; }
    }
}
