#region Using

using System;
using System.Collections.Generic;

#endregion

namespace Libra.Graphics
{
    public sealed class SharedResourcePool<TKey, TData> where TData : class
    {
        public delegate TData CreateSharedResource(TKey key);

        Dictionary<TKey, WeakReference> resourceMap;

        CreateSharedResource createFunction;

        public SharedResourcePool(CreateSharedResource createFunction)
        {
            if (createFunction == null) throw new ArgumentNullException("createFunction");

            this.createFunction = createFunction;

            resourceMap = new Dictionary<TKey, WeakReference>();
        }

        public TData Get(TKey key)
        {
            lock (resourceMap)
            {
                TData data = null;
                WeakReference reference;
                if (resourceMap.TryGetValue(key, out reference))
                {
                    data = reference.Target as TData;
                }
                else
                {
                    reference = new WeakReference(null);
                    resourceMap[key] = reference;
                }

                if (data == null)
                {
                    data = createFunction(key);
                    reference.Target = data;
                }

                return data;
            }
        }
    }
}
