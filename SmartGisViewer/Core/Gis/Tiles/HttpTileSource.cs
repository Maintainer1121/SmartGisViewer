using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace SmartGisViewer.Core.Gis.Tiles
{
    /// <summary>
    /// 基于 HTTP 的瓦片数据源（骨架版）
    /// </summary>
    public class HttpTileSource : ITileSource
    {
        
        public int TileSize => 256; //设置瓦片尺寸

        public Uri GetTileUri(int x, int y, int z)
        { 
            return new Uri($"https://tile.openstreetmap.org/{z}/{x}/{y}.png");
        }
        
    }
}