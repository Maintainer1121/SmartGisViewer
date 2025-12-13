using System;
using Avalonia;

namespace SmartGisViewer.Core.Gis.Viewport
{
    /// <summary>
    /// 新版 ViewportState（稳定不跳的关键）
    /// 世界坐标：归一化 [0,1] × [0,1]
    /// Zoom：连续 double（例如 5.2）
    /// </summary>
    public class ViewportState
    {
        // 视口中心对应的世界坐标（归一化）
        public Point CenterWorld { get; private set; } = new Point(0.5, 0.5);

        // 连续缩放级别
        public double Zoom { get; private set; } = 2.0;

        //定义最大最小缩放倍数
        public const double MinZoom = 0.0;
        public const double MaxZoom = 18.0;

        // 给 TileLayer 用：整数瓦片层级
        public int TileZoom => (int)Math.Floor(Zoom);

        // 给 TileLayer 用：当前屏幕相对于 TileZoom 的额外缩放（0~2倍）
        public double ZoomScale => Math.Pow(2, Zoom - TileZoom);

        // 世界坐标 1.0 对应多少像素（Zoom=0 时全世界宽 256px）
        public double PixelsPerWorldUnit => 256.0 * Math.Pow(2, Zoom);

        /// <summary>
        /// 世界坐标 -> 屏幕坐标
        /// viewCenterScreen：控件中心点（屏幕坐标）
        /// </summary>
        public Point WorldToScreen(Point world, Point viewCenterScreen)
        {
            var dx = (world.X - CenterWorld.X) * PixelsPerWorldUnit;
            var dy = (world.Y - CenterWorld.Y) * PixelsPerWorldUnit;
            return new Point(viewCenterScreen.X + dx, viewCenterScreen.Y + dy);
        }

        /// <summary>
        /// 屏幕坐标 -> 世界坐标
        /// </summary>
        public Point ScreenToWorld(Point screen, Point viewCenterScreen)
        {
            var dx = (screen.X - viewCenterScreen.X) / PixelsPerWorldUnit;
            var dy = (screen.Y - viewCenterScreen.Y) / PixelsPerWorldUnit;
            return new Point(CenterWorld.X + dx, CenterWorld.Y + dy);
        }

        /// <summary>
        /// 平移：拖拽屏幕 delta -> 世界中心移动
        /// </summary>
        public void Pan(Vector deltaScreen)
        {
            // 拖拽向右，地图应向右移动 => 世界中心应向左移动
            var dx = -deltaScreen.X / PixelsPerWorldUnit;
            var dy = -deltaScreen.Y / PixelsPerWorldUnit;

            CenterWorld = new Point(CenterWorld.X + dx, CenterWorld.Y + dy);

            // X 可以环绕（世界左右可无限拖）
            CenterWorld = new Point(Wrap01(CenterWorld.X), Clamp01(CenterWorld.Y));
        }

        /// <summary>
        /// 以鼠标点为锚点缩放（不跳的关键）
        /// </summary>
        public void ZoomAt(Point screenPoint, Point viewCenterScreen, double factor)
        {
            if (factor <= 0) return;

            // 先记住：缩放前鼠标指向的世界坐标
            var anchorWorldBefore = ScreenToWorld(screenPoint, viewCenterScreen);

            // 更新 Zoom（连续）
            var newZoom = Math.Clamp(Zoom + Math.Log2(factor), MinZoom, MaxZoom);
            if (Math.Abs(newZoom - Zoom) < 1e-9) return;
            Zoom = newZoom;

            // 缩放后仍要让鼠标指向同一个世界点：
            // 通过调整 CenterWorld 来补偿
            var anchorWorldAfter = ScreenToWorld(screenPoint, viewCenterScreen);

            var dx = anchorWorldBefore.X - anchorWorldAfter.X;
            var dy = anchorWorldBefore.Y - anchorWorldAfter.Y;

            CenterWorld = new Point(CenterWorld.X + dx, CenterWorld.Y + dy);
            CenterWorld = new Point(Wrap01(CenterWorld.X), Clamp01(CenterWorld.Y));
        }

        
        // =========================
        // 7) 工具函数：Clamp / Wrap
        // =========================

        /// <summary>
        /// 将值限制在 [0,1] 范围内
        /// 用于 Y 方向（上下不允许无限拖）
        /// </summary>
        private static double Clamp01(double v) => Math.Clamp(v, 0.0, 1.0);
        
        /// <summary>
        /// 将值环绕到 [0,1) 区间
        /// 用于 X 方向（左右允许无限循环）
        ///
        /// v %= 1.0 会保留小数部分：
        ///  1.2 -> 0.2
        /// -0.2 -> -0.2（所以需要再 +1.0 变成 0.8）
        /// </summary>
        private static double Wrap01(double v)
        {
            v %= 1.0;
            if (v < 0) v += 1.0;
            return v;
        }
    }
}
