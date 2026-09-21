using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Interfaces;

namespace AOSharp.Common.Unmanaged.DbObjects
{
    public class SurfaceResource : DbObject
    {
        public readonly List<Mesh> Meshes;

        private SurfaceResource(IntPtr pointer) : base(pointer)
        {
            Meshes = GetMeshes();
        }

        public static SurfaceResource FromPointer(IntPtr pointer)
        {
            return new SurfaceResource(pointer);
        }

        public unsafe static SurfaceResource Get(int id)
        {
            DBIdentity identity = new DBIdentity(DBIdentityType.SurfaceResource, id);
            IntPtr surfaceResourcePtr = ResourceDatabase.GetDbObject(identity);

            if (surfaceResourcePtr == IntPtr.Zero)
                return null;

            return new SurfaceResource(surfaceResourcePtr + 0x18);
        }

        private unsafe List<Mesh> GetMeshes()
        {
            List<Mesh> meshes = new List<Mesh>();

            Surface surface = *(Surface*)Pointer;

            foreach (SurfaceMeshData surfaceMeshData in surface.Meshes.ToList<SurfaceMeshData>())
            {
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();

                IntPtr vertexArray = surfaceMeshData.TriangleArray + (((surfaceMeshData.NumTriangles * (surfaceMeshData.NumVertices > 255 ? 6 : 3)) + 3) & -04);

                for (int i = 0; i < surfaceMeshData.NumVertices; i++)
                {
                    vertices.Add(new Vector3(*(float*)(vertexArray + i * sizeof(Vector3)),
                                            *(float*)(vertexArray + i * sizeof(Vector3) + 4),
                                            *(float*)(vertexArray + i * sizeof(Vector3) + 8)));
                }

                for (int i = 0; i < surfaceMeshData.NumTriangles; i++)
                {
                    if (surfaceMeshData.NumVertices <= 255)
                    {
                        triangles.Add(*(byte*)(surfaceMeshData.TriangleArray + i * 3));
                        triangles.Add(*(byte*)(surfaceMeshData.TriangleArray + i * 3 + 1));
                        triangles.Add(*(byte*)(surfaceMeshData.TriangleArray + i * 3 + 2));
                    }
                    else
                    {
                        triangles.Add(*(short*)(surfaceMeshData.TriangleArray + i * 6));
                        triangles.Add(*(short*)(surfaceMeshData.TriangleArray + i * 6 + 2));
                        triangles.Add(*(short*)(surfaceMeshData.TriangleArray + i * 6 + 4));
                    }
                }

                meshes.Add(new Mesh
                {
                    Vertices = vertices,
                    Triangles = triangles
                });
            }

            return meshes;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0)]
        private struct Surface
        {
            [FieldOffset(0x0C)]
            public StdStructList Meshes;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 0x38)]
        private struct SurfaceMeshData
        {
            [FieldOffset(0x00)]
            public int NumTriangles;

            [FieldOffset(0x04)]
            public int NumVertices;

            [FieldOffset(0x08)]
            public IntPtr TriangleArray;
        }
    }
}
