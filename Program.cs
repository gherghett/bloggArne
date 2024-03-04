using System.IO;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using HtmlAgilityPack;

using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;

using Facebook;


namespace bloggArne;
class Program
{
    static async Task Main(string[] args)
    {
        string wikiText = await GetTodaysWikiAsync();
        string response = await GenerateArticleAsync(wikiText);
        PostToFB(response);
    }

    static async Task<string> GetTodaysWikiAsync()
    {
        var url = "https://sv.wikipedia.org/wiki/Portal:Huvudsida"; 
        var httpClient = new HttpClient();
        string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:93.0) Gecko/20100101 Firefox/93.0";
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

        try
        {
            var html = await httpClient.GetStringAsync(url);
            //Console.WriteLine(html);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            // Extract all text content from the HTML document
            string allText = htmlDocument.DocumentNode.InnerText;

            // Print the extracted text
            //Console.WriteLine(allText.Length);

            // Scraping the text content of a div element with class 'frontPageBlockContent'
            var node = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='frontPageBlockContent']");
            if (node != null)
            {
                //Console.WriteLine(node.InnerText);  // Use InnerText to get the text content
                //File.WriteAllText("./todays.txt", node.InnerText);
                return node.InnerText;
            }
            else
            {
                Console.WriteLine("Element not found.");
                return "";
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return "";
        }
    }

    static async Task<string> GenerateArticleAsync(string str)
    {
        string dllPath = Assembly.GetExecutingAssembly().Location;
        string dllDirectory = Path.GetDirectoryName(dllPath);
        string promptFilePath = Path.Combine(dllDirectory, "preprompt.txt");
        string prompt = File.ReadAllText(promptFilePath);
        prompt += "Här är texten:\n\n\"\"\""+str+"\"\"\"\n\n";

        string tokenFilePath = Path.Combine(dllDirectory, "OpenAIToken.txt");
        Console.WriteLine(tokenFilePath);
        string OpenAItoken = File.ReadAllText(tokenFilePath);
        var api = new OpenAIClient(OpenAItoken);
        var messages = new List<Message>
        {
            new Message(Role.System, "You are a helpful assistant."),
            new Message(Role.User, prompt)
        };
        var chatRequest = new ChatRequest(messages, Model.GPT3_5_Turbo);
        var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);

        Console.WriteLine($"{result.FirstChoice.Message.Role}: {result.FirstChoice.Message.Content}");
        return result.FirstChoice.Message.Content;
    }

    static public void PostToFB(string str)
    {
        string dllPath = Assembly.GetExecutingAssembly().Location;
        string dllDirectory = Path.GetDirectoryName(dllPath);
        string tokenFilePath = Path.Combine(dllDirectory, "fbAccessToken.txt");
        string access_token = File.ReadAllText(tokenFilePath);
        // Initialize Facebook client
        var fb = new FacebookClient(access_token);

        // Prepare the post parameters
        dynamic parameters = new System.Dynamic.ExpandoObject();
        parameters.message = str;

        try
        {
            // Post the message to the page's feed
            var result = fb.Post("/137662769435471/feed", parameters);

            // Output the result (the new post's ID)
            Console.WriteLine("Post ID: " + result.id);
        }
        catch (FacebookOAuthException ex)
        {
            // Handle OAuth exceptions
            Console.WriteLine("OAuth Exception: " + ex.Message);
        }
        catch (FacebookApiException ex)
        {
            // Handle Facebook API exceptions
            Console.WriteLine("API Exception: " + ex.Message);
        }
    }
}
