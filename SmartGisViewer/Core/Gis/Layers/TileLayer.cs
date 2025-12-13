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
        private int TileSize => _tileSource.TileSize;

        /// <summary>
        /// 世界坐标基准 zoom（固定）
        /// 世界单位 = BaseZoom 下的像素
        /// </summary>
        private const int BaseZoom = 6;

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

        // =====================================================
        // Render
        // =====================================================
        public void Render(
            ViewportState viewport,
            DrawingContext context,
            Rect viewBounds,
            Point viewCenterScreen)
        {
            int tileZoom = Math.Max(viewport.TileZoom, BaseZoom);

            // 屏幕 → 世界（世界单位 = BaseZoom 像素）
            var worldTopLeft =
                viewport.ScreenToWorld(viewBounds.TopLeft, viewCenterScreen);
            var worldBottomRight =
                viewport.ScreenToWorld(viewBounds.BottomRight, viewCenterScreen);

            int minBX = (int)Math.Floor(worldTopLeft.X / TileSize);
            int maxBX = (int)Math.Floor(worldBottomRight.X / TileSize);
            int minBY = (int)Math.Floor(worldTopLeft.Y / TileSize);
            int maxBY = (int)Math.Floor(worldBottomRight.Y / TileSize);

            for (int bx = minBX; bx <= maxBX; bx++)
            {
                for (int by = minBY; by <= maxBY; by++)
                {
                    DrawBaseTile(
                        viewport,
                        context,
                        bx,
                        by,
                        tileZoom,
                        viewCenterScreen);
                }
            }
        }

        // =====================================================
        // BaseZoom 世界瓦片
        // =====================================================
        private void DrawBaseTile(
            ViewportState viewport,
            DrawingContext context,
            int baseX,
            int baseY,
            int tileZoom,
            Point viewCenterScreen)
        {
            // 世界 → 屏幕
            var worldTopLeft = new Point(baseX * TileSize, baseY * TileSize);
            var screenTopLeft =
                viewport.WorldToScreen(worldTopLeft, viewCenterScreen);

            double screenSize = TileSize * viewport.Zoom;

            var baseRect = new Rect(
                screenTopLeft,
                new Size(screenSize, screenSize));

            int delta = tileZoom - BaseZoom;
            int factor = 1 << delta;

            double subSize = screenSize / factor;

            for (int dx = 0; dx < factor; dx++)
            {
                for (int dy = 0; dy < factor; dy++)
                {
                    int realX = baseX * factor + dx;
                    int realY = baseY * factor + dy;

                    var rect = new Rect(
                        baseRect.X + dx * subSize,
                        baseRect.Y + dy * subSize,
                        subSize,
                        subSize);

                    DrawRealTile(context, realX, realY, tileZoom, rect);
                }
            }
        }

        // =====================================================
        // 真实瓦片
        // =====================================================
        private void DrawRealTile(
            DrawingContext context,
            int x,
            int y,
            int zoom,
            Rect rect)
        {
            var key = new TileKey(x, y, zoom);

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
                var uri = _tileSource.GetTileUri(x, y, zoom);
                _ = LoadTileAsync(key, uri);
            }

            // 占位
            context.FillRectangle(
                new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)), rect);
        }

        // =====================================================
        // 下载
        // =====================================================
        private async System.Threading.Tasks.Task LoadTileAsync(
            TileKey key,
            Uri uri)
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
