using Microsoft.Extensions.Configuration;
using Microsoft.Web.WebView2.WinForms;

namespace EdgeView;

public partial class Billboard : Form
{
    private readonly IConfiguration _configuration;
    private readonly WebView2 _browser = new();

    public Billboard(IConfiguration configuration)
    {
        _configuration = configuration;
        InitializeComponent();
        Controls.Add(_browser);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        var billboard = _configuration.GetSection("Billboard").Get<BillboardOptions>();
        _browser.Location = new Point(0, 0);
        _browser.Visible = true;
        _browser.Dock = DockStyle.Fill;
        _browser.ZoomFactor = billboard.ZoomFactor;

        await _browser.EnsureCoreWebView2Async();

        foreach (var item in billboard.Items.Where(x => !x.Enabled.HasValue || x.Enabled.Value).Cycle())
        {
            Text = billboard.Title.Replace("{Version}", Program.Version).Replace("{Item.Name}", item.Name);

            // Forces the browser to reload the page so that #fragment links work.
            _browser.CoreWebView2.Navigate("about:blank");
            await Task.Delay(billboard.FragmentFixDelay);

            _browser.CoreWebView2.Navigate(item.Url);
            await Task.Delay(item.DelayInSeconds * 1000);
        }
    }
}

internal class BillboardOptions
{
    public string Title { get; set; }

    public double ZoomFactor { get; set; }

    public int FragmentFixDelay { get; set; }

    public Item[] Items { get; set; }

    internal class Item
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public int DelayInSeconds { get; set; }

        public bool? Enabled { get; set; }
    }
}

internal static class Extensions
{
    public static IEnumerable<T> Cycle<T>(this IEnumerable<T> source)
    {
        while (true)
        {
            using var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                yield return e.Current;
            }
        }
    }
}