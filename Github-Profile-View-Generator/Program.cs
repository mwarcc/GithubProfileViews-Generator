using HtmlAgilityPack;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Github_Profile_View_Generator
{
    internal class Program
    {
        private static int _totalViews = 0;
        private static readonly object _lock = new object();

        static async Task Main(string[] args)
        {
            Console.Clear();
            AnsiConsole.Markup("[blue]Enter the URL of the profile you want to boost views for: [/]");
            var profileUrl = Console.ReadLine();
            Console.Clear();
            AnsiConsole.MarkupLine("[green]Fetching profile...[/]");

            var urls = await ProfileScraper.FetchUrls(profileUrl);
            var camoUrl = ProfileScraper.FindCamoUrl(urls);

            if (!string.IsNullOrEmpty(camoUrl))
            {
                Console.Clear();
                const int requestCount = 20;
                StartRequestLoop(camoUrl, requestCount);
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error: Camo URL not found[/]");
            }

            await Task.Delay(Timeout.Infinite);
        }

        private static void StartRequestLoop(string url, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Task.Run(() => RequestSender.SendRequest(url));
            }
        }

        public static void IncrementViews()
        {
            lock (_lock)
            {
                _totalViews++;
                AnsiConsole.MarkupLine($"[cyan]{GetCheckmark()} - View Sent | Total: {_totalViews}[/]");
            }
        }

        private static string GetCheckmark() => "\u2714";
    }

    public static class ProfileScraper
    {
        public static async Task<List<string>> FetchUrls(string profileUrl)
        {
            try
            {
                var client = new HttpClient();
                var response = await client.GetStringAsync(profileUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                return ExtractAnchorHrefs(doc);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error fetching profile: {ex.Message}[/]");
                return new List<string>();
            }
        }

        private static List<string> ExtractAnchorHrefs(HtmlDocument doc)
        {
            return doc.DocumentNode.Descendants("a")
                .Where(a => a.Attributes.Contains("href"))
                .Select(a => a.Attributes["href"].Value)
                .ToList();
        }

        public static string FindCamoUrl(List<string> urls)
        {
            return urls.FirstOrDefault(url => url.Contains("https://camo.githubusercontent.com"));
        }
    }

    public static class RequestSender
    {
        private static readonly HttpClient client = new HttpClient();

        public static async void SendRequest(string url)
        {
            while (true)
            {
                try
                {
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        Program.IncrementViews();
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error sending request: {ex.Message}[/]");
                }
            }
        }
    }
}
