using System;
using System.Collections.Generic;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Thread-safe queue for erosion work items and results.
    /// Allows background threads to enqueue work and retrieve results from the UI thread.
    /// </summary>
    public class ErosionQueue
    {
        private readonly Queue<ErosionWorkItem> _workQueue = new Queue<ErosionWorkItem>();
        private readonly Queue<(int id, float[] result)> _resultQueue = new Queue<(int, float[])>();
        private readonly object _workLock = new object();
        private readonly object _resultLock = new object();

        /// <summary>
        /// Enqueues a single erosion work item.
        /// </summary>
        public void Enqueue(ErosionWorkItem item)
        {
            if (!item.IsValid())
                throw new ArgumentException("Invalid work item");

            lock (_workLock)
            {
                _workQueue.Enqueue(item);
            }
        }

        /// <summary>
        /// Enqueues multiple work items.
        /// </summary>
        public void EnqueueBatch(ErosionWorkItem[] items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            lock (_workLock)
            {
                foreach (var item in items)
                {
                    if (item.IsValid())
                        _workQueue.Enqueue(item);
                }
            }
        }

        /// <summary>
        /// Attempts to dequeue a work item. Returns true if successful.
        /// Thread-safe for background worker threads.
        /// </summary>
        public bool TryDequeueWork(out ErosionWorkItem item)
        {
            lock (_workLock)
            {
                if (_workQueue.Count > 0)
                {
                    item = _workQueue.Dequeue();
                    return true;
                }
            }

            item = default;
            return false;
        }

        /// <summary>
        /// Enqueues a result after erosion processing completes.
        /// Called by background worker threads.
        /// </summary>
        public void EnqueueResult(int workItemId, float[] erodedHeightmap)
        {
            if (erodedHeightmap == null)
                throw new ArgumentNullException(nameof(erodedHeightmap));

            lock (_resultLock)
            {
                _resultQueue.Enqueue((workItemId, erodedHeightmap));
            }
        }

        /// <summary>
        /// Attempts to retrieve a result. Returns true if available.
        /// Thread-safe for UI threads.
        /// </summary>
        public bool TryDequeueResult(out int workItemId, out float[] erodedHeightmap)
        {
            lock (_resultLock)
            {
                if (_resultQueue.Count > 0)
                {
                    var (id, result) = _resultQueue.Dequeue();
                    workItemId = id;
                    erodedHeightmap = result;
                    return true;
                }
            }

            workItemId = 0;
            erodedHeightmap = null;
            return false;
        }

        /// <summary>
        /// Returns the number of pending work items.
        /// </summary>
        public int PendingWorkCount
        {
            get
            {
                lock (_workLock)
                {
                    return _workQueue.Count;
                }
            }
        }

        /// <summary>
        /// Returns the number of available results.
        /// </summary>
        public int PendingResultCount
        {
            get
            {
                lock (_resultLock)
                {
                    return _resultQueue.Count;
                }
            }
        }

        /// <summary>
        /// Checks if there is any pending work or results.
        /// </summary>
        public bool HasPending
        {
            get { return PendingWorkCount > 0 || PendingResultCount > 0; }
        }

        /// <summary>
        /// Clears all queued work and results.
        /// </summary>
        public void Clear()
        {
            lock (_workLock)
            {
                _workQueue.Clear();
            }

            lock (_resultLock)
            {
                _resultQueue.Clear();
            }
        }
    }
}
