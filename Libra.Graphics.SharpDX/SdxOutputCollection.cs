#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics.SharpDX
{
    public sealed class SdxOutputCollection : IOutputCollection
    {
        List<SdxOutput> outputs;

        public IOutput this[int index]
        {
            get { return outputs[index]; }
        }

        public SdxOutputCollection(int size)
        {
            outputs = new List<SdxOutput>();
        }

        public void Add(SdxOutput output)
        {
            outputs.Add(output);
        }

        public IEnumerator<IOutput> GetEnumerator()
        {
            return outputs.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
