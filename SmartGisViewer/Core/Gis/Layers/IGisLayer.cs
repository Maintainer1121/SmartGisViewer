using Avalonia;
using Avalonia.Media;
using SmartGisViewer.Core.Gis.Viewport;

namespace SmartGisViewer.Core.Gis.Layers
{
    /// <summary>
    /// GIS 图层统一接口
    /// </summary>
    public interface IGisLayer
    {
        /// <summary>
        /// 图层名称（调试 / 图层管理用）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 是否可见
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// 在当前视口下绘制图层
        /// </summary>
        void Render(ViewportState viewport, 
            DrawingContext context,
            Rect viewBounds,
            Point viewCenterScreen);
    }
}