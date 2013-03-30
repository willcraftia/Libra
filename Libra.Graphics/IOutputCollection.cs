#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics
{
    public interface IOutputCollection : IEnumerable<IOutput>
    {
        IOutput this[int index] { get; }
    }
}
