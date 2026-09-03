namespace backend.Crawler.Workers;

using System.Threading.Channels;

public class crawler_worker
{

    Crawler crawler=new();//injection
    private readonly ChannelWriter<(req,TaskCompletionSource<(req,List<string>)>)> dispatcher;

    private readonly ChannelReader<(req,TaskCompletionSource<(req,List<string>)>)> queue;

    private readonly List<ChannelWriter<(req,TaskCompletionSource<(req,List<string>)>)>> workers = [];

    private readonly int worker_count;

    public crawler_worker(int worker_count)
    {
        this.worker_count = worker_count;

        // Create crawler workers
        foreach (var _ in Enumerable.Range(0, worker_count))
        {
            var workerSender = CreateWorker();
            this.workers.Add(workerSender);
        }

        // Create dispatcher queue
        var channel = Channel.CreateUnbounded<(req,TaskCompletionSource<(req,List<string>)>)>();

        this.dispatcher = channel.Writer;
        this.queue = channel.Reader;
    }

    public void StartDispatcher()
    {
        int currentWorkerIndex = 0;

        _ = Task.Run(async () =>
        {
            await foreach (var req in queue.ReadAllAsync())
            {   
                await this.workers[currentWorkerIndex].WriteAsync(req);

                currentWorkerIndex =(currentWorkerIndex + 1) % worker_count;
            }
        });
    }

    public async Task Send(req request,TaskCompletionSource<(req,List<string>)> res_sender)
    {
        await dispatcher.WriteAsync((request,res_sender));
    }

    private ChannelWriter<(req,   TaskCompletionSource<(req,List<string>)>)> CreateWorker()
    {
        var channel = Channel.CreateUnbounded<(req,   TaskCompletionSource<(req,List<string>)>)>();

        var sender = channel.Writer;
        var reader = channel.Reader;

        _ = Task.Run(async () =>
        {
            await foreach (var (req,res_sender) in reader.ReadAllAsync())
            {
                // Console.WriteLine($"Dsipatcher got req for url {req.url} and teh run is {req.crawler_run} ");
                var (crawled_req,new_urls)= await this.crawler.StartCrawl(req);
                res_sender.SetResult((crawled_req,new_urls));
            }
        });

        return sender;
    }

    public req PrepCrawlerReq(string url,int crawler_runs,string mode)
    {
        var req=new req();
        req.url=url;
        req.discovered_urls=[];
        req.crawler_run=crawler_runs;
        req.mode=mode;

        return req;
    } 

    public (TaskCompletionSource<(req,List<string>)>,Task<(req, List<string>)>) CrawlerResponseCollector()
    {
        var sender=new TaskCompletionSource<(req,List<string>)>();
        var receiver=sender.Task;
        return (sender,receiver);
    }


    
}