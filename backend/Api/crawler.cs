



namespace backend.Api;

using System.Threading.Channels;
using System.Threading.Tasks;
using backend.Crawler;

using backend.Crawler.Workers;

public class Api_Crawler
{

    private crawler_worker crawler;
    public Api_Crawler()
    {
        int crawler_workers=3;//yahe
        this.crawler=new crawler_worker(crawler_workers);
        this.crawler.StartDispatcher();
    }

    public async Task<List<req>> StartCrawler(string url,string mode,int crawler_runs,HashSet<string> visited_urls)
    {
       
        

        List<req> response=[];


        bool isNew = visited_urls.Add(url);

        if (!isNew || crawler_runs <= 0)
        {
            if (crawler_runs <= 0)
            {
                // Console.WriteLine($"URL={url} | run={crawler_runs} | new={isNew}  run over");
                return [];    
            }
            // Console.WriteLine($"URL={url} | run={crawler_runs} | new={isNew}  skipping already got");
            return [];

        }

        // Console.WriteLine($"URL={url} | run={crawler_runs} | new={isNew}");

        

        // Console.WriteLine($"Crawling url {url} and run is {crawler_runs} and visited urls are {visited_urls.Count()}");

        var(writer,crawler_res_reciver)=this.crawler.CrawlerResponseCollector();

        

        var req=this.crawler.PrepCrawlerReq(url,crawler_runs,mode);

        await this.crawler.Send(req,writer);
        var (completed_req_crawl,new_urls)=await crawler_res_reciver;
        crawler_runs--;

        response.Add(completed_req_crawl); 

        // Console.WriteLine($"we discovered total {new_urls.Count()} and the from url  {url}");
        

        foreach (var new_url in new_urls)
        {
        // Console.WriteLine($"the nexr url is {new_url}");

            var res=await StartCrawler(new_url, mode,crawler_runs,visited_urls);
            
            response.AddRange(res);
        }
        

        return response;
    }

    
}