



namespace backend.Api;

using System.Threading.Tasks;
using backend.Crawler;



public class Api_Crawler
{

    private Crawler crawler;
    public Api_Crawler()
    {
        this.crawler=new Crawler();
    }

    public async Task StartCrawl(String url)
    {
        // Console.WriteLine("req came");
        await this.crawler.StartCrawl(url);
    }
}