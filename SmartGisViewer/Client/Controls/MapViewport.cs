using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SmartGisViewer.Core.Gis.Layers;
using SmartGisViewer.Core.Gis.Viewport;
using System.Globalization;



namespace SmartGisViewer.Client.Controls
{
    /// <summary>
    /// 地图视口控件（核心 UI 容器）
    ///
    /// 职责：
    /// 1. 持有 ViewportState（缩放 / 平移 / 坐标变换）
    /// 2. 接收鼠标输入（拖拽、滚轮）
    /// 3. 调度所有 IGisLayer 进行绘制
    ///
    /// 非职责：
    /// - 不关心画什么
    /// - 不关心瓦片从哪来
    /// - 不包含任何具体 GIS 业务逻辑
    /// </summary>
    public class MapViewport : Control
    {
        // =========================
        // 核心状态
        // =========================

        private readonly ViewportState _viewport = new ViewportState();
        private readonly List<IGisLayer> _layers = new();

        public ViewportState Viewport => _viewport;
        public IList<IGisLayer> Layers => _layers;

        // =========================
        // 交互状态
        // =========================

        private bool _isPanning = false;
        private Point _lastPointerPosition;

        private const double ZoomFactor = 1.1;

        public MapViewport()
        {
            Focusable = true;
        }

        // =========================
        // 鼠标输入处理
        // =========================

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                _isPanning = true;
                _lastPointerPosition = point.Position;
                e.Pointer.Capture(this);
            }

            Focus();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (!_isPanning)
                return;

            var current = e.GetPosition(this);
            var delta = current - _lastPointerPosition;

            // 拖拽本质：在屏幕坐标系中平移视口
            _viewport.Pan(new Vector(delta.X, delta.Y));

            _lastPointerPosition = current;
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_isPanning)
            {
                _isPanning = false;
                e.Pointer.Capture(null);
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (e.Delta.Y == 0)
                return;

            var mousePosition = e.GetPosition(this);
            var factor = e.Delta.Y > 0 ? ZoomFactor : 1.0 / ZoomFactor;

            _viewport.ZoomAt(mousePosition, factor);
            InvalidateVisual();
        }

        // =========================
        // 渲染调度（核心）
        // =========================

        public override void Render(DrawingContext context)
        {
            base.Render(context); //让父类rander先绘制需要绘制的东西

            // 1️⃣ 当前视口区域
            Rect bounds = this.Bounds;

            // 2️⃣ 清空背景（避免残影）
            context.FillRectangle(Brushes.Black, bounds);

            // 3️⃣ 依次调度图层绘制
            foreach (var layer in _layers)
            {
                if (!layer.IsVisible)
                    continue;

                layer.Render(
                    _viewport,   // 视口状态（世界↔屏幕）
                    context,     // Avalonia 绘图上下文
                    bounds       // 明确告知绘制区域
                );
            }

            // 4️⃣ 调试信息（可删）
            DrawDebugInfo(context, bounds);
        }

        // =========================
        // 调试辅助
        // =========================

        private void DrawDebugInfo(DrawingContext context, Rect bounds)
        {
            var text = new FormattedText(
                $"Scale = {_viewport.Scale:F2}\n" +
                $"Offset = ({_viewport.Offset.X:F1}, {_viewport.Offset.Y:F1})",

                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                12,
                Brushes.White
            );

            context.DrawText(
                text,
                new Point(bounds.Left + 10, bounds.Top + 10)
            );
        }


    }
}
