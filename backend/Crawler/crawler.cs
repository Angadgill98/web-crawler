


using System.Threading.Tasks;
using backend.Crawler.Workers;

namespace backend.Crawler;

public class Crawler
{

    playwright_worker_dispatcher playwright_dispatcher;//may injection
    http_worker http_dispatcher;//may injection
    Parser parser=new();

    public Crawler(){
        int playwright_workers_count=1;//yahe
        this.playwright_dispatcher=new playwright_worker_dispatcher(playwright_workers_count);
        this.playwright_dispatcher.StartDispatcher();
        

        int http_workers_count=1;//yahe

        this.http_dispatcher=new http_worker(http_workers_count);
        this.http_dispatcher.StartDispatcher();


    }

    public async Task<(req,List<string>)> StartCrawl(req req)
    {
        var html_sender=new TaskCompletionSource<string>();
        var html_receiver=html_sender.Task;

        string html;
        req.crawler_html_complete_signal=html_sender;
        switch (req.mode)
        {
            //now we got teh html form teh url this will send it http for static content 
            case "http":
                await this.http_dispatcher.Send(req);
                break;
            
            //now we got teh html form teh url this wills end it to playeithgt adn get html+js
            case "js":
                await this.playwright_dispatcher.Send(req);
                break;
            
            default:
                break;

        }

    
        html=await html_receiver;

        //parsing the html            
        var json=this.parser.parse_pipeline(html);
        Dictionary<string, List<JsonTree>> map = new();
        json.CreateElementHashMap(map);

        req.elements=json;

        var a_tag_elements=map["a"];

        List<string> discovered_urls=this.ExtractHrefLink(a_tag_elements);

        List<string> urls_to_crawl= this.CreateFullUrls(discovered_urls,req.url);


        

        req.discovered_urls.UnionWith(urls_to_crawl);

        return (req,urls_to_crawl);

    }

    List<string> ExtractHrefLink(List<JsonTree> a_tag)
    {
        List<string> links = new();

        foreach (var element in a_tag)
        {
            string content = element.content;

            int hrefIndex = content.IndexOf("href",StringComparison.OrdinalIgnoreCase);

            if (hrefIndex == -1)
                continue;

            int equalsIndex = content.IndexOf('=', hrefIndex);

            if (equalsIndex == -1)
                continue;

            int start = equalsIndex + 1;

            // Skip spaces after =
            while (start < content.Length && char.IsWhiteSpace(content[start]))
            {
                start++;
            }

            if (start >= content.Length)
                continue;

            char quote = content[start];

            if (quote == '"' || quote == '\'')
            {
                start++;

                int end = content.IndexOf(quote, start);

                if (end == -1)
                    continue;

                string href = content.Substring(
                    start,
                    end - start
                );

                links.Add(href);
            }
        }

        return links;
    }

    List<string> CreateFullUrls(List<string> extractedUrls,string requestUrl)
    {
        List<string> fullUrls = new();

        foreach (var url in extractedUrls)
        {
            Uri baseUri = new Uri(requestUrl);
            Uri fullUri = new Uri(baseUri, url);

            fullUrls.Add(fullUri.ToString());
        }

        return fullUrls;
    }


    //function from t
    // public req PrepCrawlerReq(string url,int crawler_runs,string mode)
    // {
    //     var req=new req();
    //     req.url=url;
    //     req.discovered_urls=[];
    //     req.crawler_run=crawler_runs;
    //     req.mode=mode;

    //     return req;
    // } 


}




public struct req
{
    public string url { get; set; }
    public TaskCompletionSource<string> crawler_html_complete_signal { get; set; }
    public HashSet<string> discovered_urls { get; set; }
    public int crawler_run { get; set; }
    public string mode { get; set; }

    public JsonTree elements { get; set; }

}


