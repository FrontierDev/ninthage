using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Game.Editor.WorldEditor.Streaming
{
    /// <summary>
    /// Memory pool for reusing float arrays in erosion processing.
    /// Significantly reduces GC pressure for large terrain generation.
    /// </summary>
    public static class HeightmapArrayPool
    {
        private static readonly ConcurrentDictionary<int, ConcurrentQueue<float[]>> _pools = new();
        private const int MAX_POOL_SIZE = 8; // Maximum arrays per size

        /// <summary>
        /// Rents a float array of the specified size. Returns a pooled array if available.
        /// </summary>
        public static float[] Rent(int size)
        {
            if (_pools.TryGetValue(size, out var queue) && queue.TryDequeue(out var array))
            {
                Array.Clear(array, 0, array.Length); // Clear for reuse
                return array;
            }

            return new float[size];
        }

        /// <summary>
        /// Returns a float array to the pool for reuse.
        /// </summary>
        public static void Return(float[] array)
        {
            if (array == null) return;

            int size = array.Length;
            var queue = _pools.GetOrAdd(size, _ => new ConcurrentQueue<float[]>());

            if (GetQueueSize(queue) < MAX_POOL_SIZE)
            {
                queue.Enqueue(array);
            }
            // If pool is full, let GC handle the array
        }

        /// <summary>
        /// Returns multiple arrays to the pool.
        /// </summary>
        public static void Return(params float[][] arrays)
        {
            foreach (var array in arrays)
            {
                Return(array);
            }
        }

        /// <summary>
        /// Clears all pooled arrays to free memory.
        /// </summary>
        public static void Clear()
        {
            _pools.Clear();
        }

        private static int GetQueueSize(ConcurrentQueue<float[]> queue)
        {
            // Approximate count (ConcurrentQueue doesn't have Count property)
            int count = 0;
            foreach (var _ in queue)
            {
                count++;
                if (count >= MAX_POOL_SIZE) break;
            }
            return count;
        }
    }

    /// <summary>
    /// RAII wrapper for automatic array return to pool.
    /// </summary>
    public struct PooledArray : IDisposable
    {
        private float[] _array;
        public float[] Array => _array;

        public PooledArray(int size)
        {
            _array = HeightmapArrayPool.Rent(size);
        }

        public void Dispose()
        {
            if (_array != null)
            {
                HeightmapArrayPool.Return(_array);
                _array = null;
            }
        }
    }
}