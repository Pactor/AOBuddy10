using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Interfaces;

namespace AOSharp.Common.Unmanaged.DbObjects
{
    public class DungeonRDBTilemap : RDBTilemap
    {
        public unsafe IntPtr DungeonHeightmapPtr => ((RDBTilemapMemStruct*)Pointer)->DungeonHeightmapPtr;
        public unsafe IntPtr CollisionDataPtr => ((RDBTilemapMemStruct*)Pointer)->CollisionDataPtr;
        public byte[,] Heightmap;
        public byte[,] CollisionData;

        internal unsafe DungeonRDBTilemap(IntPtr pointer) : base(pointer)
        {
            Parse();
        }

        public static DungeonRDBTilemap Get(int id)
        {
            DBIdentity identity = new DBIdentity(DBIdentityType.RDBTilemap, id);
            return ResourceDatabase.GetDbObject<DungeonRDBTilemap>(identity);
        }

        public static DungeonRDBTilemap FromPointer(IntPtr pointer)
        {
            return new DungeonRDBTilemap(pointer);
        }

        protected override unsafe void Parse()
        {
            using (UnmanagedMemoryStream unmanagedMemStream = new UnmanagedMemoryStream((byte*)DungeonHeightmapPtr.ToPointer(), Width * Height))
            {
                using (BinaryReader reader = new BinaryReader(unmanagedMemStream))
                {
                    Heightmap = new byte[Width, Height];

                    for (int y = 0; y < Height; y++)
                        for (int x = 0; x < Width; x++)
                            Heightmap[x, y] = reader.ReadByte();
                }
            }

            using (UnmanagedMemoryStream unmanagedMemStream = new UnmanagedMemoryStream((byte*)CollisionDataPtr.ToPointer(), Width * Height))
            {
                using (BinaryReader reader = new BinaryReader(unmanagedMemStream))
                {
                    CollisionData = new byte[Width, Height];

                    for (int y = 0; y < Height; y++)
                        for (int x = 0; x < Width; x++)
                            CollisionData[x, y] = reader.ReadByte();
                }
            }
        }
    }

    public class OutdoorRDBTilemap : RDBTilemap
    {
        public List<Chunk> Chunks;

        public int NumChunksX => GroundData.NumChunksX;
        public int NumChunksZ => GroundData.NumChunksZ;
        public int Modulo => GroundData.Modulo;

        private unsafe IntPtr GroundDataPtr => ((RDBTilemapMemStruct*)Pointer)->GroundDataPtr;
        private unsafe AnarchyGroundDataMemStruct GroundData => *(AnarchyGroundDataMemStruct*)GroundDataPtr;

        internal unsafe OutdoorRDBTilemap(IntPtr pointer) : base(pointer)
        {
            Parse();
        }

        public static OutdoorRDBTilemap Get(int id)
        {
            DBIdentity identity = new DBIdentity(DBIdentityType.RDBTilemap, id);
            return ResourceDatabase.GetDbObject<OutdoorRDBTilemap>(identity);
        }

        public static OutdoorRDBTilemap FromPointer(IntPtr pointer)
        {
            return new OutdoorRDBTilemap(pointer);
        }

        protected unsafe override void Parse()
        {
            Chunks = new List<Chunk>();
            int numChunks = NumChunksX * NumChunksZ;

            using (UnmanagedMemoryStream unmanagedMemStream = new UnmanagedMemoryStream((byte*)GroundData.ChunkDataPtr.ToPointer(), 0x54 * numChunks))
            {
                using (BinaryReader reader = new BinaryReader(unmanagedMemStream))
                {
                    for (int i = 0; i < numChunks; i++)
                    {
                        int x = reader.ReadInt32();
                        int y = reader.ReadInt32();
                        reader.ReadInt32();
                        int chunkSize = reader.ReadInt32() + 1;

                        byte[] decompressedHeightMap = DecompressArray(reader.ReadInt32(), (IntPtr)reader.ReadInt32());

                        bool hasShortHeights = (Math.Sqrt(decompressedHeightMap.Length) != Math.Truncate(Math.Sqrt(decompressedHeightMap.Length)));

                        ushort[,] heightmap = hasShortHeights ? UnfilterShortHeightmap(decompressedHeightMap, chunkSize) : UnfilterHeightmap(decompressedHeightMap, chunkSize);

                        reader.ReadBytes(0x3C);

                        Chunks.Add(new Chunk
                        {
                            X = x,
                            Y = y,
                            Size = chunkSize,
                            Heightmap = heightmap
                        });
                    }
                }
            }
        }

        private unsafe byte[] DecompressArray(int size, IntPtr pArray)
        {
            using (UnmanagedMemoryStream unmanagedMemStream = new UnmanagedMemoryStream((byte*)pArray.ToPointer(), size))
            {
                using (DeflateStream inflateStream = new DeflateStream(unmanagedMemStream, CompressionMode.Decompress))
                {
                    using (MemoryStream output = new MemoryStream())
                    {
                        inflateStream.BaseStream.ReadByte();
                        inflateStream.BaseStream.ReadByte();
                        inflateStream.CopyTo(output);
                        return output.ToArray();
                    }
                }
            }
        }

        public class Chunk
        {
            public int X;
            public int Y;
            public int Size;
            public ushort[,] Heightmap;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        private struct AnarchyGroundDataMemStruct
        {
            [FieldOffset(0x34)]
            public int Width;

            [FieldOffset(0x38)]
            public int Height;

            [FieldOffset(0x3C)]
            public int Modulo;

            [FieldOffset(0x50)]
            public int NumChunksX;

            [FieldOffset(0x54)]
            public int NumChunksZ;

            [FieldOffset(0x58)]
            public IntPtr ChunkDataPtr;
        }
    }

    public class RDBTilemap : DbObject
    {
        public unsafe bool IsDungeon => ((RDBTilemapMemStruct*)Pointer)->IsDungeon;

        public unsafe float HeightmapScale => ((RDBTilemapMemStruct*)Pointer)->HeightmapScale;

        public unsafe short NumMainTiles => ((RDBTilemapMemStruct*)Pointer)->NumMainTiles;

        public unsafe IntPtr MainTileIds => ((RDBTilemapMemStruct*)Pointer)->MainTileIds;

        public unsafe int Width => ((RDBTilemapMemStruct*)Pointer)->Width;

        public unsafe int Height => ((RDBTilemapMemStruct*)Pointer)->Height;

        public unsafe float TileSize => ((RDBTilemapMemStruct*)Pointer)->TileSize;

        protected unsafe RDBTilemap(IntPtr pointer) : base(pointer)
        {
            Parse();
        }

        protected virtual void Parse()
        {
        }

        private ushort[] GetShortHeights(BinaryReader reader, int numHeights)
        {
            ushort[] heights = new ushort[numHeights];

            for (int i = 0; i < numHeights; i++)
                heights[i] = reader.ReadUInt16();

            return heights;
        }

        protected ushort[,] UnfilterShortHeightmap(byte[] heightMap, int chunkSize)
        {
            ushort[] heights;

            using (BinaryReader reader = new BinaryReader(new MemoryStream(heightMap)))
            {
                heights = new ushort[heightMap.Length / 2];

                for (int i = 0; i < heights.Length; i++)
                    heights[i] = reader.ReadUInt16();
            }

            ushort delta;

            for (var y = 0; y < chunkSize; y++)
            {
                delta = 0;

                for (var x = 0; x < chunkSize; x++)
                {
                    int idx = x + y * chunkSize;
                    heights[idx] += delta;
                    delta = heights[idx];
                }
            }

            delta = 0;

            for (var x = 0; x < chunkSize; x++)
            {
                delta = 0;
                for (var y = 0; y < chunkSize; y++)
                {
                    int idx = x + y * chunkSize;
                    heights[idx] += delta;
                    delta = heights[idx];
                }
            }

            ushort[,] unfilteredHeightmap = new ushort[chunkSize, chunkSize];
            for (int x = 0; x < chunkSize; x++)
                for (int y = 0; y < chunkSize; y++)
                    unfilteredHeightmap[x, y] = heights[chunkSize * y + x];

            return unfilteredHeightmap;
        }

        protected ushort[,] UnfilterHeightmap(byte[] heightMap, int chunkSize)
        {
            int delta;

            for (var y = 0; y < chunkSize; y++)
            {
                delta = 0;
                for (var x = 0; x < chunkSize; x++)
                {
                    var val = heightMap[chunkSize * y + x];
                    heightMap[chunkSize * y + x] = (byte)(delta + val);
                    delta = delta + val;
                }
            }

            for (var x = 0; x < chunkSize; x++)
            {
                delta = 0;
                for (var y = 0; y < chunkSize; y++)
                {
                    var val = heightMap[chunkSize * y + x];
                    heightMap[chunkSize * y + x] = (byte)(delta + val);
                    delta = delta + val;
                }
            }

            ushort[,] unfilteredHeightmap = new ushort[chunkSize, chunkSize];
            for (int x = 0; x < chunkSize; x++)
                for (int y = 0; y < chunkSize; y++)
                    unfilteredHeightmap[x, y] = heightMap[chunkSize * y + x];

            return unfilteredHeightmap;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        protected struct RDBTilemapMemStruct
        {
            [FieldOffset(0x18)]
            public bool IsDungeon;

            [FieldOffset(0x1C)]
            public float HeightmapScale;

            [FieldOffset(0x20)]
            public IntPtr CollisionDataPtr;

            [FieldOffset(0x228)]
            public IntPtr DungeonHeightmapPtr;

            [FieldOffset(0x230)]
            public short NumMainTiles;

            [FieldOffset(0x234)]
            public IntPtr MainTileIds;

            [FieldOffset(0x8258)]
            public IntPtr GroundDataPtr;

            [FieldOffset(0x825C)]
            public int Width;

            [FieldOffset(0x8260)]
            public int Height;

            [FieldOffset(0x8264)]
            public float TileSize;
        }
    }
}
