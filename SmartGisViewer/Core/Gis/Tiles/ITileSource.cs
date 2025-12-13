using System.Threading.Tasks;
using System;
using Avalonia.Media.Imaging;

namespace SmartGisViewer.Core.Gis.Tiles
{
    /// <summary>
    /// 瓦片数据源接口
    /// 只负责：给我 (x, y, z)，我返回一张瓦片
    /// </summary>
    public interface ITileSource
    {
        int TileSize { get; } //设定瓦片尺寸
        Uri GetTileUri(int x, int y, int z); //初始化地址
        //Task<Bitmap?> GetTileAsync(int x, int y, int z);
    }
}