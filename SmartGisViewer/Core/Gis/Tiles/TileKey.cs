using System;

namespace SmartGisViewer.Core.Gis.Tiles
{
    public readonly struct TileKey
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public TileKey(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override int GetHashCode()
            => HashCode.Combine(X, Y, Z);

        public override bool Equals(object? obj)
            => obj is TileKey k && k.X == X && k.Y == Y && k.Z == Z;
    }
}