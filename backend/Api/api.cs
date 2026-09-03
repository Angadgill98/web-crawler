



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

        //this is depth first algo so the bottm ar treated first 
        int crawler_runs=2;//yahe

        router.MapPost("/crawl-url",async (string url,string mode)=>{
            var result = await crawler.StartCrawler(
                url,
                mode,
                crawler_runs,
                new()
            );
            // Console.WriteLine($"we got total {result.Count()} urls from {crawler_runs} run for url {url}");
            return Results.Ok(result);
        });
    }


   
}

// router.MapPost("/crawl-url",async (string url,string mode)=>{
//             var result = await crawler.StartCrawl(
//                 url,
//                 mode,
//                 crawler_runs
//             );

//             return Results.Ok(result);
//         });