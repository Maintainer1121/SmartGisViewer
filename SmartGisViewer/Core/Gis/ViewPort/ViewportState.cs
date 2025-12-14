using System;
using Avalonia;

namespace SmartGisViewer.Core.Gis.Viewport
{
    /// <summary>
    /// ViewportState（世界坐标固定为 Zoom=0 像素世界：0~256）
    /// </summary>
    public class ViewportState
    {
        // Zoom=0 时世界宽度（像素）
        public const double WorldSize0 = 256.0;

        // 视口中心（世界坐标，范围建议：X 可 wrap，Y clamp）
        public Point CenterWorld { get; private set; } = new Point(WorldSize0 / 2, WorldSize0 / 2);

        // 连续缩放级别
        public double Zoom { get; private set; } = 10.0;

        public const double MinZoom = 0.0;
        public const double MaxZoom = 18.0;

        // 当前整数瓦片层级
        public int TileZoom => (int)Math.Floor(Zoom);

        // 屏幕上每 1 个世界单位（Zoom=0 像素）对应多少屏幕像素
        public double PixelsPerWorldUnit => Math.Pow(2, Zoom);

        // -------------------------
        // 坐标变换
        // -------------------------
        public Point WorldToScreen(Point world, Point viewCenterScreen)
        {
            var dx = (world.X - CenterWorld.X) * PixelsPerWorldUnit;
            var dy = (world.Y - CenterWorld.Y) * PixelsPerWorldUnit;
            return new Point(viewCenterScreen.X + dx, viewCenterScreen.Y + dy);
        }

        public Point ScreenToWorld(Point screen, Point viewCenterScreen)
        {
            var dx = (screen.X - viewCenterScreen.X) / PixelsPerWorldUnit;
            var dy = (screen.Y - viewCenterScreen.Y) / PixelsPerWorldUnit;
            return new Point(CenterWorld.X + dx, CenterWorld.Y + dy);
        }

        // -------------------------
        // 平移 / 缩放
        // -------------------------
        public void Pan(Vector deltaScreen)
        {
            CenterWorld = new Point(
                CenterWorld.X - deltaScreen.X / PixelsPerWorldUnit,
                CenterWorld.Y - deltaScreen.Y / PixelsPerWorldUnit
            );

            // X 环绕，Y 限制
            CenterWorld = new Point(WrapWorldX(CenterWorld.X), ClampWorldY(CenterWorld.Y));
        }

        public void ZoomAt(Point screenPoint, Point viewCenterScreen, double factor)
        {
            if (factor <= 0) return;

            var before = ScreenToWorld(screenPoint, viewCenterScreen);

            var newZoom = Math.Clamp(Zoom + Math.Log2(factor), MinZoom, MaxZoom);
            if (Math.Abs(newZoom - Zoom) < 1e-9) return;
            Zoom = newZoom;

            var after = ScreenToWorld(screenPoint, viewCenterScreen);

            CenterWorld = new Point(
                CenterWorld.X + (before.X - after.X),
                CenterWorld.Y + (before.Y - after.Y)
            );

            CenterWorld = new Point(WrapWorldX(CenterWorld.X), ClampWorldY(CenterWorld.Y));
        }

        public void SetZoom(double zoom)
        {
            Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        }

        public void SetCenterWorld(Point world)
        {
            CenterWorld = new Point(WrapWorldX(world.X), ClampWorldY(world.Y));
        }

        // -------------------------
        // 经纬度 → 世界坐标（Zoom=0 像素世界）
        // -------------------------
        public static Point LonLatToWorldPixel(double lon, double lat)
        {
            // WebMercator 投影（归一化到 [0,1] 再乘 256）
            double x01 = (lon + 180.0) / 360.0;

            double latRad = lat * Math.PI / 180.0;
            double y01 = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0;

            return new Point(x01 * WorldSize0, y01 * WorldSize0);
        }
        
        //-------------------------
        // 世界坐标 → 经纬度（Zoom=0 像素世界）
        // -------------------------
        public static (double lon, double lat) WorldPixelToLonLat(Point world)
        {
            // world 是 Zoom=0 的像素世界（0~256）
            double x01 = world.X / WorldSize0;
            double y01 = world.Y / WorldSize0;

            double lon = x01 * 360.0 - 180.0;

            double n = Math.PI - 2.0 * Math.PI * y01;
            double lat = 180.0 / Math.PI * Math.Atan(Math.Sinh(n));

            return (lon, lat);
        }

        

        private static double ClampWorldY(double y) => Math.Clamp(y, 0.0, WorldSize0);

        private static double WrapWorldX(double x)
        {
            x %= WorldSize0;
            if (x < 0) x += WorldSize0;
            return x;
        }
    }
}
