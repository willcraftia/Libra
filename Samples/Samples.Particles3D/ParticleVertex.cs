#region Using

using System;
using Libra;
using Libra.Graphics;
using Libra.PackedVector;

#endregion

namespace Samples.Particles3D
{
    public struct ParticleVertex
    {
        public Short2 Corner;

        public Vector3 Position;

        public Vector3 Velocity;

        public Color Random;

        public float Time;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement("CORNER", 0, InputElementFormat.Short2),
            new VertexElement("POSITION", 0, InputElementFormat.Vector3),
            new VertexElement("VELOCITY", 0, InputElementFormat.Vector3),
            new VertexElement("RANDOM", 0, InputElementFormat.Color),
            new VertexElement("TIME", 0, InputElementFormat.Single)
            );
    }
}
