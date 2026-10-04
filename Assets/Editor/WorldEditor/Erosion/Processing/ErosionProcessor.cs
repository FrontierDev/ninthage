using System;
using System.Threading;

namespace Game.Editor.WorldEditor
{
    /// <summary>
    /// Manages background thread erosion processing.
    /// Dequeues work items, applies erosion, and enqueues results for retrieval.
    /// </summary>
    public class ErosionProcessor : IDisposable
    {
        private readonly ErosionQueue _queue;
        private Thread _processingThread;
        private volatile bool _isRunning;
        private volatile bool _shouldStop;

        /// <summary>
        /// Create a new erosion processor with the given queue.
        /// </summary>
        public ErosionProcessor(ErosionQueue queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _isRunning = false;
            _shouldStop = false;
        }

        /// <summary>
        /// Starts the background processing thread.
        /// Safe to call multiple times; idempotent.
        /// </summary>
        public void StartProcessing()
        {
            if (_processingThread != null)
                return; // Already running

            _shouldStop = false;
            _processingThread = new Thread(ProcessingThreadMain)
            {
                IsBackground = true,
                Name = "ErosionProcessor"
            };
            _processingThread.Start();
        }

        /// <summary>
        /// Stops the background processing thread and waits for it to finish.
        /// Drains remaining queue items before shutdown.
        /// </summary>
        public void StopProcessing()
        {
            if (_processingThread == null)
                return; // Not running

            _shouldStop = true;

            // Wait for thread to finish (with timeout to prevent hang)
            if (_processingThread.IsAlive)
            {
                _processingThread.Join(TimeSpan.FromSeconds(10));
                if (_processingThread.IsAlive)
                {
                    UnityEngine.Debug.LogWarning("ErosionProcessor thread did not shutdown cleanly");
                    _processingThread.Abort();
                }
            }

            _processingThread = null;
            _isRunning = false;
        }

        /// <summary>
        /// Returns true if the processor is actively running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Main loop for the background processing thread.
        /// Dequeues work items, applies erosion, and enqueues results.
        /// </summary>
        private void ProcessingThreadMain()
        {
            _isRunning = true;

            try
            {
                while (!_shouldStop)
                {
                    // Try to dequeue a work item
                    if (_queue.TryDequeueWork(out var workItem))
                    {
                        try
                        {
                            // Apply erosion
                            float[] erodedHeightmap = workItem.mode.Erode(
                                workItem.heightmap,
                                workItem.width,
                                workItem.height,
                                workItem.settings);

                            // Enqueue result
                            _queue.EnqueueResult(workItem.id, erodedHeightmap);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Erosion processing failed for work item {workItem.id}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // No work available; sleep briefly to avoid busy-waiting
                        Thread.Sleep(10);
                    }
                }

                // Process any remaining items before shutdown
                while (_queue.TryDequeueWork(out var workItem))
                {
                    try
                    {
                        float[] erodedHeightmap = workItem.mode.Erode(
                            workItem.heightmap,
                            workItem.width,
                            workItem.height,
                            workItem.settings);

                        _queue.EnqueueResult(workItem.id, erodedHeightmap);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Erosion processing failed for work item {workItem.id}: {ex.Message}");
                    }
                }
            }
            finally
            {
                _isRunning = false;
            }
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            StopProcessing();
        }
    }
}
