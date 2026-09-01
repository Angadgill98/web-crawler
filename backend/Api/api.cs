



namespace backend.Api;





public class Api
{
    public Api(WebApplication app)
    {
        SetupCrawlerRoutes(app);
    }

    void SetupCrawlerRoutes(WebApplication app)
    {
        var router=app.MapGroup("/api/crawler");
        
        Api_Crawler crawler=new Api_Crawler();


        router.MapPost("/crawl-url",async (String url)=>{await crawler.StartCrawl(url);});
    }
}