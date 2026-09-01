


using System.Threading.Tasks;
using backend.Crawler.Workers;

namespace backend.Crawler;

public class Crawler
{

    playwright_worker_dispatcher playwright_dispatcher;//may injection
    http_worker http_dispatcher;//may injection

    public Crawler(){
        int playwright_workers_count=1;//yahe
        this.playwright_dispatcher=new playwright_worker_dispatcher(playwright_workers_count);
        this.playwright_dispatcher.StartDispatcher();

        int http_workers_count=1;//yahe

        this.http_dispatcher=new http_worker(http_workers_count);
        this.http_dispatcher.StartDispatcher();
    }

    public async Task StartCrawl(string url)
    {
        int req_runs=1;//yahe
        var (req_list,receivers)=this.PrepStaticList(url,req_runs);

        //now we got teh static html form teh url this  
        await this.http_dispatcher.Send(req_list);
        List<string> responses=[];
        { //collect teh responses
            foreach (var req in req_list)
            {
                string result= await req.static_run_complete_signal.Task;
                responses.Add(result);
            }
        }

        Console.WriteLine($"{responses}");
        
        
        //this is for after we deied what is volatile adn non-v
        //to-decied:reqlist struct to change follows teh static struct 
        // await this.playwright_dispatcher.Send(req_list);


    }

    (List<static_req> req_list, List<Task<string>> receivers)  PrepStaticList(string url,int req_runs)
    {
        var req_list=new List<static_req>();
        var receivers=new List< System.Threading.Tasks.Task<string>>();
        foreach (var i in Enumerable.Range(0,req_runs))
        {
            var job_singaler=new TaskCompletionSource<string>();
            var job_waiting_signal=job_singaler.Task;

            var req=new static_req();
            req.url=url;
            req.static_run_complete_signal=job_singaler;
            req.instructions=[];

            req_list.Add(req);
            receivers.Add(job_waiting_signal);
        }

        return (req_list, receivers);
    } 

}


public struct static_req
{
    public string url;
    public System.Threading.Tasks.TaskCompletionSource<string> static_run_complete_signal;

    public List<String> instructions;
}


