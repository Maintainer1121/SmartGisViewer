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
        
        private Point? _lastMouseScreen; //记录最后一次鼠标在屏幕的位置（可能为空）


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
            
            //记录鼠标所在的屏幕坐标
            _lastMouseScreen = e.GetPosition(this);

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
            
            var viewCenter = new Point(Bounds.Width / 2, Bounds.Height / 2); //获取视口中心

            _viewport.ZoomAt(mousePosition, viewCenter, factor);
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
            
            //获取视口区域，为了避免偏移新建一个rect并且设定左上角为0，0
            var viewportBounds = new Rect(0, 0, bounds.Width, bounds.Height);

            using (context.PushClip(viewportBounds))
            {
                //清空背景避免残影
                context.FillRectangle(Brushes.Black, viewportBounds); 
                
                //新建视口中心点
                var viewCenter = new Point(viewportBounds.Width / 2, viewportBounds.Height / 2);

                foreach (var layer in _layers)
                {
                    //如果图层不可见则不渲染
                    if(!layer.IsVisible)
                        continue;
                    
                    layer.Render(
                        _viewport,
                        context,
                        viewportBounds,
                        viewCenter
                        );
                }
                
                //绘制调试信息
                DrawDebugInfo(context, viewportBounds);
                DrawMouseInfo(context, viewportBounds, viewCenter);
                DrawViewCenter(context, viewCenter);
                
                context.DrawRectangle(
                    null,
                    new Pen(Brushes.Lime, 2),
                    viewportBounds
                );


            }

        }

        // =========================
        // 调试辅助
        // =========================

        private void DrawDebugInfo(DrawingContext context, Rect bounds)
        {
            var text = new FormattedText(
                $"Zoom = {_viewport.Zoom:F2}\n" +
                $"TileZoom = {_viewport.TileZoom}\n" +
                $"CenterWorld = ({_viewport.CenterWorld.X:F3}, {_viewport.CenterWorld.Y:F3})",

                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                12,
                Brushes.Red
            );

            context.DrawText(
                text,
                new Point(bounds.Left + 10, bounds.Top + 10)
            );
        }
        
        /// <summary>
        /// 绘制鼠标信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bounds"></param>
        /// <param name="viewCenterScreen"></param>
        private void DrawMouseInfo(
            DrawingContext context,
            Rect bounds,
            Point viewCenterScreen)
        {
            if (_lastMouseScreen == null)
                return;

            var screen = _lastMouseScreen.Value;

            // Screen → World
            var world = _viewport.ScreenToWorld(screen, viewCenterScreen);

            // World → LonLat
            var (lon, lat) = ViewportState.WorldPixelToLonLat(world);

            var text = new FormattedText(
                $"Screen: ({screen.X:F1}, {screen.Y:F1})\n" +
                $"World : ({world.X:F3}, {world.Y:F3})\n" +
                $"LonLat: ({lon:F6}, {lat:F6})",
                System.Globalization.CultureInfo.CurrentCulture,
                Avalonia.Media.FlowDirection.LeftToRight,
                Avalonia.Media.Typeface.Default,
                12,
                Avalonia.Media.Brushes.Red
            );

            // 右上角显示
            context.DrawText(
                text,
                new Point(bounds.Right - text.Width - 10, bounds.Top + 10)

            );
        }
        
        
        private void DrawViewCenter(DrawingContext context, Point viewCenter)
        {
            var pen = new Pen(Brushes.Lime, 1);

            context.DrawLine(
                pen,
                new Point(viewCenter.X - 10, viewCenter.Y),
                new Point(viewCenter.X + 10, viewCenter.Y));

            context.DrawLine(
                pen,
                new Point(viewCenter.X, viewCenter.Y - 10),
                new Point(viewCenter.X, viewCenter.Y + 10));
        }





    }
}
