using System.Collections.Generic;

namespace AOSharp.Common.GameData
{
    public class Mesh
    {
        public List<Vector3> Vertices;
        public List<int> Triangles;
        public Quaternion Rotation;
        public Vector3 Position;
        public Vector3 Scale;

        public Mesh()
        {
            Scale = new Vector3(1, 1, 1);
        }

        public Matrix4x4 LocalToWorldMatrix
        {
            get
            {
                var translation = Matrix4x4.Translate(Position);
                var rotation = Matrix4x4.Rotate(Rotation);
                var scale = Matrix4x4.Scale(Scale);

                return translation * rotation * scale;
            }
        }
    }
}
