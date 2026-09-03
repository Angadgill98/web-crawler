


namespace backend.Crawler.Workers;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Playwright;

class playwright_worker_dispatcher
{

    PlaywrightManager play_manager=new(1,4);//injection
    readonly ChannelWriter<req> dispatcher;
    readonly List<ChannelWriter<req>> workers=[];

    ChannelReader<req> queue;

    readonly int worker_count;
    public playwright_worker_dispatcher(int worker_count)
    {

        foreach(var i in Enumerable.Range(0, worker_count))
        {
            var worker_sender_signal=CreateWorkers();
            this.workers.Add(worker_sender_signal);
        }

        var channel=Channel.CreateUnbounded<req>();
        var dispatch_data_signal=channel.Writer;
        var dispatcher_queue=channel.Reader;

        

        this.dispatcher=dispatch_data_signal;
        this.worker_count=worker_count;
        this.queue=dispatcher_queue;


    }

    public void StartDispatcher()
    {   
        var current_wroker_index=0;
        
        _ = Task.Run(async () =>
        {
            await foreach(var req in this.queue.ReadAllAsync())
            {

                await this.workers[current_wroker_index].WriteAsync(req);
                
                current_wroker_index = (current_wroker_index + 1) % this.worker_count;
            } 
        });
    }

    public async Task Send(req reqs)
    {
        await this.dispatcher.WriteAsync(reqs);    
        
    }

    ChannelWriter<req> CreateWorkers()
    {
        var channel=Channel.CreateUnbounded<req>();
        var sender=channel.Writer;
        var reader=channel.Reader;

        

        _ = Task.Run(async () =>
        {

            await this.play_manager.InitializePlaywirght();


            await foreach(req req in reader.ReadAllAsync())
            {   

                Browser browser= await this.play_manager.GetBrowser();
                var tab= await this.play_manager.AcquireTabLock(browser);
                try
                {
                    string html = await tab.OpenURL(req.url);

                    req.crawler_html_complete_signal.SetResult(html);
                }
                finally
                {
                    Console.WriteLine("Released teh lock");
                    tab.tab_lock.Release();
                }
            } 
        });



        return sender;

    }

   
  

}
