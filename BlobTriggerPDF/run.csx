public static void Run(Stream myBlob, string name, ICollector<string> outputQueueItem, TraceWriter log)
{
    log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
    log.Info($"C# Queue trigger function processed: {outputQueueItem}\n");
    outputQueueItem.Add(name);
}
