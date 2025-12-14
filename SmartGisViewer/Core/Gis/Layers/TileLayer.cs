using System;
using System.Collections.Generic;
using System.Net.Http;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SmartGisViewer.Core.Gis.Tiles;
using SmartGisViewer.Core.Gis.Viewport;

namespace SmartGisViewer.Core.Gis.Layers
{
    public class TileLayer : IGisLayer
    {
        public string Name => "Tile Layer";
        public bool IsVisible { get; set; } = true;

        private readonly ITileSource _tileSource;
        private int TileSize => _tileSource.TileSize; // 256

        private readonly Dictionary<TileKey, Bitmap> _cache = new();
        private readonly HashSet<TileKey> _loading = new();

        private static readonly HttpClient _http = CreateHttpClient();

        public event Action? TileUpdated;

        private static HttpClient CreateHttpClient()
        {
            var c = new HttpClient();
            c.DefaultRequestHeaders.UserAgent.ParseAdd("SmartGisViewer/1.0");
            return c;
        }

        public TileLayer(ITileSource tileSource)
        {
            _tileSource = tileSource;
        }

        public void Render(
            ViewportState viewport,
            DrawingContext context,
            Rect viewBounds,
            Point viewCenterScreen)
        {
            int z = viewport.TileZoom;
            double scale = viewport.PixelsPerWorldUnit;

            // 在 Zoom=0 世界里，一个 z 层瓦片占多大（世界单位）
            // 世界总宽 256，被切成 2^z 份
            double worldTileSize = ViewportState.WorldSize0 / (1 << z);

            // 当前屏幕覆盖的世界范围（Zoom=0 世界）
            var worldTopLeft = viewport.ScreenToWorld(viewBounds.TopLeft, viewCenterScreen);
            var worldBottomRight = viewport.ScreenToWorld(viewBounds.BottomRight, viewCenterScreen);

            int minX = (int)Math.Floor(worldTopLeft.X / worldTileSize);
            int maxX = (int)Math.Floor(worldBottomRight.X / worldTileSize);
            int minY = (int)Math.Floor(worldTopLeft.Y / worldTileSize);
            int maxY = (int)Math.Floor(worldBottomRight.Y / worldTileSize);

            // y 不环绕：夹到合法范围
            int maxIndex = (1 << z) - 1;
            minY = Math.Max(minY, 0);
            maxY = Math.Min(maxY, maxIndex);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // x 环绕（世界左右循环）
                    int wrappedX = x % (1 << z);
                    if (wrappedX < 0) wrappedX += (1 << z);

                    DrawTile(context, viewport, wrappedX, y, z, worldTileSize, scale, viewCenterScreen);
                }
            }
        }

        private void DrawTile(
            DrawingContext context,
            ViewportState viewport,
            int x,
            int y,
            int z,
            double worldTileSize,
            double scale,
            Point viewCenterScreen)
        {
            // 瓦片在 Zoom=0 世界里的左上角
            var worldPos = new Point(x * worldTileSize, y * worldTileSize);
            var screenPos = viewport.WorldToScreen(worldPos, viewCenterScreen);

            var rect = new Rect(
                screenPos,
                new Size(worldTileSize * scale, worldTileSize * scale));

            var key = new TileKey(x, y, z);

            if (_cache.TryGetValue(key, out var bmp))
            {
                context.DrawImage(
                    bmp,
                    new Rect(0, 0, bmp.PixelSize.Width, bmp.PixelSize.Height),
                    rect);
                return;
            }

            if (!_loading.Contains(key))
            {
                _loading.Add(key);
                var uri = _tileSource.GetTileUri(x, y, z);
                _ = LoadTileAsync(key, uri);
            }

            context.FillRectangle(new SolidColorBrush(Color.FromArgb(255, 45, 45, 45)), rect);
        }

        private async System.Threading.Tasks.Task LoadTileAsync(TileKey key, Uri uri)
        {
            try
            {
                Console.WriteLine($"GET {uri}");
                var s = await _http.GetStreamAsync(uri);
                using var ms = new System.IO.MemoryStream();
                await s.CopyToAsync(ms);
                ms.Position = 0;
                _cache[key] = new Bitmap(ms);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _loading.Remove(key);
                TileUpdated?.Invoke();
            }
        }
    }
}
