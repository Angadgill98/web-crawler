
using System.Threading.Tasks;
using Microsoft.Playwright;




//would be plawright class but constructor cant haev async so we use another function to intialize the oalywirght object
public class PlaywrightManager
{
    int browser_count=1;//yahe
    int tab_count=4;//yahe

    int current_browser_index=0;

    private IPlaywright playwright=null!;

    Dictionary<int,Browser> browsers;
    
    public PlaywrightManager(int browser_count,int tab_count){
        this.browser_count=browser_count;
        this.tab_count=tab_count;
        this.browsers=new();
    }

    public async Task InitializePlaywirght()
    {
        var playwright=await Playwright.CreateAsync();
        this.playwright=playwright;

        for (int i = 0; i < this.browser_count; i++)
        {
            await this.Openbrowser(i);
        }
    }

    async Task Openbrowser(int id)
    {
        var browser = await this.playwright.Chromium.LaunchAsync(
        new BrowserTypeLaunchOptions
        {
            Headless = false
        });

        await IntializeBrowser(id,browser);
    }
   
    async Task IntializeBrowser(int id,IBrowser browser)
    {   
        List<Tab> tabs=[];
        foreach (var i in Enumerable.Range(0,this.tab_count))
        {
            var page = await browser.NewPageAsync();
            tabs.Add(new Tab(page));
        }

        var a=new Browser();
        a.id=id;
        a.tabs=tabs;
        a.tabs_count=this.tab_count;
        this.browsers.Add(id,a);

    }

    public async Task<Browser> GetBrowser()
    {
        var browser_id=current_browser_index%this.browser_count;
        var browser=this.browsers[browser_id];

        this.current_browser_index++;
        return browser;
    }

    public async Task<Tab> AcquireTabLock(Browser browser)
    {
        var tabs=browser.tabs;
        Console.WriteLine("wating for tab lcok");
        while (true)
        {
            foreach (Tab tab in tabs)
            {
                bool acquired = await tab.tab_lock.WaitAsync(100);

                if (acquired)
                {
                    Console.WriteLine("got the tab lcok");

                    return tab; 
                }
            }
        }
    }

    async Task OpenURL(string url,IPage tab)
    {
        await tab.GotoAsync(url);
        string html = await tab.ContentAsync();
    }
}


public struct Browser
{
    public int id;
    public int tabs_count;
    public List<Tab> tabs;


}

public class Tab
{
    IPage tab;
    public SemaphoreSlim tab_lock= new SemaphoreSlim(1, 1);
    public Tab(IPage page)
    {
        this.tab=page;
    }

    public async Task<string> OpenURL(string url)
    {
        await tab.GotoAsync(url);

        string html = await tab.ContentAsync();

        return html;
    }
}