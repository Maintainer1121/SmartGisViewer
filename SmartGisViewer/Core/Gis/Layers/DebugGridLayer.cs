using Avalonia;
using Avalonia.Media;
using SmartGisViewer.Core.Gis.Viewport;
using System;

namespace SmartGisViewer.Core.Gis.Layers
{
    /// <summary>
    /// 调试用网格图层
    /// 用于验证：
    /// 1. Viewport 坐标变换是否正确
    /// 2. Layer 机制是否真正解耦
    /// </summary>
    public class DebugGridLayer : IGisLayer
    {
        public string Name => "Debug Grid";
        public bool IsVisible { get; set; } = true;

        private readonly double _gridStep;

        //======构造函数=======
        public DebugGridLayer(double gridStep = 100)
        {
            _gridStep = gridStep;
        }

        public void Render(
            ViewportState viewport,
            DrawingContext context,
            Rect bounds
        )
        {
            var gridPen = new Pen(
                new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 1);

            var axisPen = new Pen(
                new SolidColorBrush(Color.FromArgb(200, 0, 255, 0)), 2);

            var topLeftWorld = viewport.ScreenToWorld(bounds.TopLeft);
            var bottomRightWorld = viewport.ScreenToWorld(bounds.BottomRight);

            double startX = Math.Floor(topLeftWorld.X / _gridStep) * _gridStep;
            double endX   = Math.Ceiling(bottomRightWorld.X / _gridStep) * _gridStep;
            double startY = Math.Floor(topLeftWorld.Y / _gridStep) * _gridStep;
            double endY   = Math.Ceiling(bottomRightWorld.Y / _gridStep) * _gridStep;

            // 纵向网格线
            for (double x = startX; x <= endX; x += _gridStep)
            {
                var p1 = viewport.WorldToScreen(new Point(x, startY));
                var p2 = viewport.WorldToScreen(new Point(x, endY));
                context.DrawLine(gridPen, p1, p2);
            }

            // 横向网格线
            for (double y = startY; y <= endY; y += _gridStep)
            {
                var p1 = viewport.WorldToScreen(new Point(startX, y));
                var p2 = viewport.WorldToScreen(new Point(endX, y));
                context.DrawLine(gridPen, p1, p2);
            }

            // 原点坐标轴
            var origin = viewport.WorldToScreen(new Point(0, 0));

            context.DrawLine(axisPen,
                new Point(bounds.Left, origin.Y),
                new Point(bounds.Right, origin.Y));

            context.DrawLine(axisPen,
                new Point(origin.X, bounds.Top),
                new Point(origin.X, bounds.Bottom));
        }

    }
}
