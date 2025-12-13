using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using SmartGisViewer.Core.Gis.Viewport;
using SmartGisViewer.Core.Gis.Tiles;
using System.Globalization;
using System.Net.Http;
using Avalonia.Media.Imaging;

namespace SmartGisViewer.Core.Gis.Layers
{
    /// <summary>
    /// 瓦片图层（稳定版）
    /// 目标：
    /// 1. 正确铺满屏幕
    /// 2. 缩放 / 平移完全正确
    /// 3. 为后续 HTTP Tile 打好结构
    /// </summary>
    public class TileLayer : IGisLayer
    {
        public string Name => "Tile Layer";
        public bool IsVisible { get; set; } = true;

        //private const int TileSize = 256;
        private int TileSize => _tileSource.TileSize;

        private readonly int _zoom = 6;

        private readonly ITileSource _tileSource;
        
        private readonly Dictionary<TileKey, Bitmap> _tileCache = new();
        private readonly HashSet<TileKey> _loadingTiles = new();

        private static readonly HttpClient _http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "SmartGisViewer/1.0 (contact: your_email@example.com)");
            return client;
        }


        public event Action? TileUpdated;


        private readonly Pen _borderPen =
            new Pen(new SolidColorBrush(Color.FromArgb(180, 30, 144, 255)), 1);

        public TileLayer(ITileSource tileSource)
        {
            _tileSource = tileSource;
        }

        public void Render(
            ViewportState viewport,
            DrawingContext context,
            Rect viewBounds)
        {
            // 1️⃣ 当前视口对应的世界坐标范围
            var worldTopLeft = viewport.ScreenToWorld(viewBounds.TopLeft);
            var worldBottomRight = viewport.ScreenToWorld(viewBounds.BottomRight);

            int minTileX = (int)Math.Floor(worldTopLeft.X / TileSize);
            int maxTileX = (int)Math.Floor(worldBottomRight.X / TileSize);
            int minTileY = (int)Math.Floor(worldTopLeft.Y / TileSize);
            int maxTileY = (int)Math.Floor(worldBottomRight.Y / TileSize);

            // 2️⃣ 逐 tile 绘制
            for (int x = minTileX; x <= maxTileX; x++)
            {
                for (int y = minTileY; y <= maxTileY; y++)
                {
                    DrawRealTile(viewport, context, x, y);
                }
            }
        }

        // =========================
        // 单瓦片绘制（Fake Tile）
        // =========================

        private void DrawFakeTile(
            ViewportState viewport,
            DrawingContext context,
            int tileX,
            int tileY)
        {
            // 世界坐标 → 屏幕坐标
            var worldTopLeft = new Point(
                tileX * TileSize,
                tileY * TileSize);

            var screenTopLeft = viewport.WorldToScreen(worldTopLeft);
            var screenSize = TileSize * viewport.Scale;

            var rect = new Rect(
                screenTopLeft,
                new Size(screenSize, screenSize));

            // 背景颜色（奇偶区分，方便观察）
            var fillBrush = new SolidColorBrush(
                ((tileX + tileY) & 1) == 0
                    ? Color.FromArgb(255, 45, 45, 45)
                    : Color.FromArgb(255, 65, 65, 65));

            // 1️⃣ 填充
            context.FillRectangle(fillBrush, rect);

            // 2️⃣ 边框
            context.DrawRectangle(null, _borderPen, rect);

            // 3️⃣ 调试文字
            DrawTileLabel(context, rect, tileX, tileY);
        }
        

        // =========================
        // 瓦片绘制（Real Tile）
        // =========================
        private void DrawRealTile(
            ViewportState viewport,
            DrawingContext context,
            int tileX,
            int tileY)
        {
            var key = new TileKey(tileX, tileY, _zoom);

            // 1️⃣ 计算屏幕 rect（完全复用你现在的逻辑）
            var worldTopLeft = new Point(tileX * TileSize, tileY * TileSize);
            var screenTopLeft = viewport.WorldToScreen(worldTopLeft);
            var screenSize = TileSize * viewport.Scale;

            var rect = new Rect(
                screenTopLeft,
                new Size(screenSize, screenSize));

            // 2️⃣ 如果缓存里有，直接画
            if (_tileCache.TryGetValue(key, out var bitmap))
            {
                context.DrawImage(
                    bitmap,
                    new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height),
                    rect);
                return;
            }

            // 3️⃣ 没有但正在加载 → 画占位
            if (_loadingTiles.Contains(key))
            {
                DrawLoadingPlaceholder(context, rect);
                return;
            }

            // 4️⃣ 既没有，也没加载过 → 发起加载
            _loadingTiles.Add(key);
            var uri = _tileSource.GetTileUri(tileX, tileY, _zoom);
            _ = LoadTileAsync(key, uri);

            DrawLoadingPlaceholder(context, rect);
        }

        // =========================
        // 占位瓦片绘制
        // =========================
        private void DrawLoadingPlaceholder(
            DrawingContext context,
            Rect rect)
        {
            var brush = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40));
            context.FillRectangle(brush, rect);
            context.DrawRectangle(null, _borderPen, rect);
        }

        
        // =========================
        // 下载瓦片
        // =========================
        private async System.Threading.Tasks.Task LoadTileAsync(
            TileKey key,
            Uri uri)
        {
            Console.WriteLine($"Downloaded {uri}");


            try
            {
                var stream = await _http.GetStreamAsync(uri);
                using var ms = new System.IO.MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;

                var bitmap = new Bitmap(ms);
                _tileCache[key] = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed {uri} : {ex.Message}");
            }

            finally
            {
                _loadingTiles.Remove(key);
                
                //通过事件重绘UI
                TileUpdated?.Invoke();
            }
        }

        
        
        // =========================
        // Tile 坐标文字
        // =========================

        private void DrawTileLabel(
            DrawingContext context,
            Rect rect,
            int x,
            int y)
        {
            var text = new FormattedText(
                $"({x}, {y}, z={_zoom})",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                13,
                Brushes.White
            );

            context.DrawText(
                text,
                new Point(rect.Left + 6, rect.Top + 6)
            );
        }
    }
}
