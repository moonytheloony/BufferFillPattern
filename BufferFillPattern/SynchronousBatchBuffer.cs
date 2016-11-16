namespace BufferFillPattern
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks.Dataflow;

    /// <summary>
    ///     The synchronous batch buffer.
    /// </summary>
    /// <typeparam name="T">
    ///     Type of data for the buffer.
    /// </typeparam>
    public class SynchronousBatchBuffer<T> : IDisposable
    {
        /// <summary>
        ///     The buffered batch.
        /// </summary>
        private readonly BatchBlock<T> bufferedBatch;

        /// <summary>
        ///     The buffer full action.
        /// </summary>
        private readonly ActionBlock<IEnumerable<T>> bufferFullAction;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SynchronousBatchBuffer{T}" /> class.
        /// </summary>
        /// <param name="batchSize">
        ///     The batch size.
        /// </param>
        /// <param name="invokeOnBufferFull">
        ///     The invoke on buffer full.
        /// </param>
        public SynchronousBatchBuffer(int batchSize, Action<IEnumerable<T>> invokeOnBufferFull)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            this.TaskCancellationToken = cancellationTokenSource.Token;
            this.BatchSize = batchSize;
            this.bufferedBatch = new BatchBlock<T>(
                this.BatchSize,
                new GroupingDataflowBlockOptions { Greedy = true, CancellationToken = this.TaskCancellationToken });
            this.bufferFullAction = new ActionBlock<IEnumerable<T>>(
                invokeOnBufferFull,
                new ExecutionDataflowBlockOptions { CancellationToken = this.TaskCancellationToken });

            //// Link the buffer to action to invoke
            this.bufferedBatch.LinkTo(this.bufferFullAction);

            //// If the buffer gets completed the associated action should complete as well.
            this.bufferedBatch.Completion.ContinueWith(element => this.bufferFullAction.Complete());
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="SynchronousBatchBuffer{T}" /> class.
        /// </summary>
        ~SynchronousBatchBuffer()
        {
            //// Clear out buffer if not already completed.
            if (!this.bufferedBatch.Completion.IsCompleted)
            {
                this.ProcessCompleted();
            }
        }

        /// <summary>
        ///     Gets the batch size.
        /// </summary>
        public int BatchSize { get; }

        /// <summary>
        ///     Gets the task cancellation token.
        /// </summary>
        public CancellationToken TaskCancellationToken { get; }

        /// <summary>
        ///     The dispose.
        /// </summary>
        public void Dispose()
        {
            this.ProcessCompleted();
        }

        /// <summary>
        ///     The feed buffer.
        /// </summary>
        /// <param name="dataToFeed">
        ///     The data to feed.
        /// </param>
        public void FeedBuffer(T dataToFeed)
        {
            this.bufferedBatch.Post(dataToFeed);
        }

        /// <summary>
        ///     The process completed.
        /// </summary>
        public void ProcessCompleted()
        {
            this.bufferedBatch.Complete();
            this.bufferFullAction.Completion.Wait();
        }
    }
}