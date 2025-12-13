using System;

namespace SmartGisViewer.Core.Gis.Tiles
{
    /// <summary>
    /// 高德地图瓦片源（标准 XYZ）
    /// 特点：
    /// - 256x256
    /// - 无需 key
    /// - 可直接 HTTP 访问
    /// - 非 TMS，不需要翻转 Y
    /// </summary>
    public class GDTileSource : ITileSource
    {
        public int TileSize => 256;

        public Uri GetTileUri(int x, int y, int z)
        {
            // 高德子域名 1~4，用于负载均衡
            int sub = Math.Abs(x + y) % 4 + 1;

            var url =
                $"https://webrd0{sub}.is.autonavi.com/appmaptile?" +
                $"lang=zh_cn&size=1&scale=1&style=7&" +
                $"x={x}&y={y}&z={z}";

            return new Uri(url);
        }
    }
}