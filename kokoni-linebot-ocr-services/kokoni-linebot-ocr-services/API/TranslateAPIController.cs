using System;
using System.Net;
using System.Net.Http.Headers;
using System.Configuration;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Linq;
using System.Web;
using System.Collections.Generic;

namespace KokoniLinebotOCRServices.Library
{
    class TranslateAPIController
    {
        static string host = "https://api.cognitive.microsofttranslator.com";
        static string path = "/translate?api-version=3.0";
        static string params_ = "&to=ja";
        static string uri = host + path + params_;

        /// <summary>
        /// 翻訳APIにリクエストする
        /// </summary>
        /// <returns>string</returns>
        public static async Task<string> GetTranslateWord(string transWord, string token, TraceWriter log)
        {
            string translated = string.Empty;
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Computer vision APIのOCRにリクエスト
            System.Object[] body = new System.Object[] { new { Text = transWord } };
            var requestBody = JsonConvert.SerializeObject(body);
            var responseText = string.Empty;

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", token);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<TranslateRootObject>>(responseBody);

                responseText = result.FirstOrDefault().detectedLanguage.language + Environment.NewLine;
                responseText += result.FirstOrDefault().detectedLanguage.score + Environment.NewLine;
                responseText += result.FirstOrDefault().translations.FirstOrDefault().to + Environment.NewLine;
                responseText += result.FirstOrDefault().translations.FirstOrDefault().text + Environment.NewLine;
                responseText = responseText.Replace(Environment.NewLine, "<br>");

                return responseText;
            }
        }
    }

    public class TranslateRootObject
    {
        public Detectedlanguage detectedLanguage { get; set; }
        public Translation[] translations { get; set; }
    }

    public class Detectedlanguage
    {
        public string language { get; set; }
        public float score { get; set; }
    }

    public class Translation
    {
        public string text { get; set; }
        public string to { get; set; }
    }


}
