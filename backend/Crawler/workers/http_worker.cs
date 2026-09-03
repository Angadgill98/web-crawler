namespace backend.Crawler.Workers;

using System.Threading.Channels;

public class http_worker
{
    HttpClient http=new HttpClient();//injection
    private readonly ChannelWriter<req> dispatcher;

    private readonly List<ChannelWriter<req>> workers = [];

    private readonly ChannelReader<req> queue;

    private readonly int worker_count;

    public http_worker(int worker_count)
    {
        this.worker_count = worker_count;

        // Create workers
        foreach (var _ in Enumerable.Range(0, worker_count))
        {
            var workerSender = CreateWorker();
            this.workers.Add(workerSender);
        }

        // Create dispatcher queue
        var channel = Channel.CreateUnbounded<req>();

        this.dispatcher = channel.Writer;
        this.queue = channel.Reader;
    }

    public void StartDispatcher()
    {
        var currentWorkerIndex = 0;

        _ = Task.Run(async () =>
        {
            await foreach (var req in this.queue.ReadAllAsync())
            {
                
                await this.workers[currentWorkerIndex].WriteAsync(req);

                currentWorkerIndex =(currentWorkerIndex + 1) % this.worker_count;
            }
        });
    }

    public async Task Send(req reqs)
    {
       
        await this.dispatcher.WriteAsync(reqs);
    }

    private ChannelWriter<req> CreateWorker()
    {
        var channel = Channel.CreateUnbounded<req>();

        var sender = channel.Writer;
        var reader = channel.Reader;

        _ = Task.Run(async () =>
        {
            await foreach (var req in reader.ReadAllAsync())
            {//this get teh html without js 

                // Console.WriteLine($"Worker processing: {req.url}");

                var response = await this.http.GetAsync(req.url);

                string static_html =await response.Content.ReadAsStringAsync();

                req.crawler_html_complete_signal.SetResult(static_html);
            }
        });

        return sender;
    }
}