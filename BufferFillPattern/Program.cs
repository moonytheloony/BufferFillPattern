namespace BufferFillPattern
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Program
    {
        static void Main(string[] args)
        {
            TestBufferFillAlgorithmFunction();
            Console.ReadKey();
        }

        private static void TestBufferFillAlgorithmFunction()
        {
            //// Specify action to be taken on each batch when you receive it. This batch will be processed in batches of 5.
            using (var batchBuffer = new SynchronousBatchBuffer<int>(
                5,
                elementBatch =>
                    {
                        foreach (var element in elementBatch)
                        {
                            Console.WriteLine("Got Value:" + element);
                        }
                    }))
            {
                //// This algorithm processes remainder values as well. The remainder values would be flushed out on disposing the object.
                Parallel.For(
                    0,
                    39,
                    testValue =>
                        {
                            //// Simulate delayed feed to buffer for parallel operations.
                            Thread.Sleep(5000);
                            Console.WriteLine("Feeding value " + testValue + " to buffer");
                            batchBuffer.FeedBuffer(testValue);
                        });
            }
        }
    }
}