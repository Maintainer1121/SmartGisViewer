using Avalonia.Controls;
using Avalonia.Threading;
using SmartGisViewer.Client.Controls;
using SmartGisViewer.Core.Gis.Layers;
using SmartGisViewer.Core.Gis.Tiles;

namespace SmartGisViewer.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var map = this.FindControl<MapViewport>("Map");

        var tileSource = new GDTileSource();
        var tileLayer = new TileLayer(tileSource);

        // ✅ 关键：瓦片加载完成 → 通知 MapViewport 重绘
        tileLayer.TileUpdated += () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                map.InvalidateVisual();
            });
        };

        map.Layers.Add(tileLayer);
    }
}