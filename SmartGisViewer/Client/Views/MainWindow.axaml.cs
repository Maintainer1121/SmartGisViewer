using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SmartGisViewer.Client.Controls;
using SmartGisViewer.Core.Gis.Layers;
using SmartGisViewer.Core.Gis.Tiles;
using SmartGisViewer.Core.Gis.Viewport;

namespace SmartGisViewer.Client.Views;

public partial class MainWindow : Window
{
    //常量配置
    private const double ZoomFactor = 1.2;
    
    //初始地点坐标（成都）
    private const double lon = 104.0668; 
    private const double lat = 30.5728;
    private const int initZoom = 10;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        //获取MapViewPort
        var map = this.FindControl<MapViewport>("Map");

        //初始化瓦片数据源和图层
        var tileSource = new GDTileSource();
        var tileLayer = new TileLayer(tileSource);

        //初始化视图
        map.Viewport.SetZoom(initZoom);
        map.Viewport.SetCenterWorld(ViewportState.LonLatToWorldPixel(lon, lat));
        
        // 调试网格（可保留）
        map.Layers.Add(new DebugGridLayer(256));
        
        // 瓦片更新 >> 重绘
        tileLayer.TileUpdated += () =>
        {
            Dispatcher.UIThread.Post(map.InvalidateVisual);
        };

        //添加瓦片到图层
        map.Layers.Add(tileLayer);
    }
    
    
    //======视口按钮事件======
    private void ZoomIn_Click(object? sender, RoutedEventArgs e)
    {
        ZoomByButton(ZoomFactor);
    }

    private void ZoomOut_Click(object? sender, RoutedEventArgs e)
    {
        ZoomByButton(1.0 / ZoomFactor);
    }

    private void ResetView_Click(object? sender, RoutedEventArgs e)
    {
        ResetView();
    }
    
    
    //======窗体逻辑======
    /// <summary>
    /// 设置缩放比例
    /// </summary>
    /// <param name="factor"></param>
    private void ZoomByButton(double factor)
    {
        //如果没有MapViewport就返回
        if(Map == null)
            return;
        
        //获取mapViewPort尺寸
        var bounds = Map.Bounds;
        
        //以当前视口中心作为缩放锚点
        var viewCenter = new Point(
            bounds.Width / 2,
            bounds.Height / 2
            );
        
        Map.Viewport.ZoomAt(
            viewCenter,
            viewCenter,
            factor
            );
        
        //重绘控件
        Map.InvalidateVisual();
    }

    /// <summary>
    /// 重置视角
    /// </summary>
    private void ResetView()
    {
        //如果没有MapViewport就返回
        if (Map == null)
            return;
        
        Map.Viewport.SetZoom(initZoom); //重置缩放
        Map.Viewport.SetCenterWorld(ViewportState.LonLatToWorldPixel(lon, lat)); //重置世界中心点
        
        //重绘控件
        Map.InvalidateVisual();
    }
    
    
    
}