#region Using

using System;
using Libra;

#endregion

namespace Samples.Audio3D
{
    public interface IAudioEmitter
    {
        Vector3 Position { get; }
        
        Vector3 Forward { get; }
        
        Vector3 Up { get; }
        
        Vector3 Velocity { get; }
    }
}
