using System;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace rest_client
{
    class Program
    {
        // Deklarera variabler för slutpunkt och nyckel för Cognitive Services
        private static string cogSvcEndpoint;
        private static string cogSvcKey;

        static async Task Main(string[] args)
        {
            try
            {
                // Hämta konfigurationsinställningar från appsettings.json
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
                cogSvcKey = configuration["CognitiveServiceKey"];

                // Sätt konsolens kodning till Unicode
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;

                // Hämta användarens inmatning (tills de skriver "quit")
                string userText = "";
                while (userText.ToLower() != "quit")
                {
                    Console.WriteLine("Ange lite text ('quit' för att avsluta)");
                    userText = Console.ReadLine();
                    if (userText.ToLower() != "quit")
                    {
                        // Anropa funktionen för att detektera språk
                        await GetLanguage(userText);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fånga och skriv ut eventuella undantag
                Console.WriteLine(ex.Message);
            }
        }

        // Funktion för att detektera språk
        static async Task GetLanguage(string text)
        {
            try
            {
                // Konstruera JSON-begäran
                JObject jsonBody = new JObject(
                    // Skapa en samling av dokument (vi använder bara ett, men fler kan läggas till)
                    new JProperty("documents",
                    new JArray(
                        new JObject(
                            // Varje dokument behöver ett unikt ID och lite text
                            new JProperty("id", 1),
                            new JProperty("text", text)
                    ))));

                // Koda som UTF-8
                UTF8Encoding utf8 = new UTF8Encoding(true, true);
                byte[] encodedBytes = utf8.GetBytes(jsonBody.ToString());

                // Visa JSON som vi skickar till tjänsten
                Console.WriteLine(utf8.GetString(encodedBytes, 0, encodedBytes.Length));

                // Skapa en HTTP-klient för att göra REST-anrop
                var client = new HttpClient();
                var queryString = HttpUtility.ParseQueryString(string.Empty);

                // Lägg till autentiseringsnyckeln i headern
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", cogSvcKey);

                // Använd slutpunkten för att komma åt Text Analytics språk-API
                var uri = cogSvcEndpoint + "text/analytics/v3.1/languages?" + queryString;

                // Skicka begäran och få svaret
                HttpResponseMessage response;
                using (var content = new ByteArrayContent(encodedBytes))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, content);
                }

                // Om anropet var framgångsrikt, få svaret
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Visa hela JSON-svaret (bara för att vi ska kunna se det)
                    string responseContent = await response.Content.ReadAsStringAsync();
                    JObject results = JObject.Parse(responseContent);
                    Console.WriteLine(results.ToString());

                    // Extrahera detekterat språknamn för varje dokument
                    foreach (JObject document in results["documents"])
                    {
                        Console.WriteLine("\nSpråk: " + (string)document["detectedLanguage"]["name"]);
                    }
                }
                else
                {
                    // Något gick fel, skriv hela svaret
                    Console.WriteLine(response.ToString());
                }
            }
            catch (Exception ex)
            {
                // Fånga och skriv ut eventuella undantag
                Console.WriteLine(ex.Message);
            }

        }
    }
}
