using System;
using Avalonia;

namespace SmartGisViewer.Core.Gis.Viewport
{
    /// <summary>
    /// 视口状态：
    /// - 决定当前“看哪里、怎么看”
    /// - 不关心 UI、不关心图层、不关心瓦片
    /// </summary>
    public class ViewportState
    {
        public double Scale { get; private set; } = 1.0;
        public Vector Offset { get; private set; } = new Vector(0, 0);

        public const double MinScale = 0.1;
        public const double MaxScale = 20.0;

        /// <summary>
        /// 世界坐标 → 屏幕坐标
        /// </summary>
        public Point WorldToScreen(Point world)
        {
            return new Point(
                world.X * Scale + Offset.X,
                world.Y * Scale + Offset.Y
            );
        }

        /// <summary>
        /// 屏幕坐标 → 世界坐标
        /// </summary>
        public Point ScreenToWorld(Point screen)
        {
            return new Point(
                (screen.X - Offset.X) / Scale,
                (screen.Y - Offset.Y) / Scale
            );
        }

        /// <summary>
        /// 平移（拖拽）
        /// </summary>
        public void Pan(Vector deltaScreen)
        {
            Offset += deltaScreen;
        }

        /// <summary>
        /// 以某个屏幕点为中心缩放
        /// </summary>
        public void ZoomAt(Point screenPoint, double factor)
        {
            if (factor <= 0) return;

            var worldBefore = ScreenToWorld(screenPoint);

            var newScale = Math.Clamp(Scale * factor, MinScale, MaxScale);
            if (Math.Abs(newScale - Scale) < double.Epsilon)
                return;

            Scale = newScale;

            Offset = new Vector(
                screenPoint.X - worldBefore.X * Scale,
                screenPoint.Y - worldBefore.Y * Scale
            );
        }
    }
}