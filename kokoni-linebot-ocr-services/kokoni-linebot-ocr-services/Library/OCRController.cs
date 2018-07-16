using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace KokoniLinebotOCRServices.Library
{
    public class OCRController
    {

        /// <summary>
        /// 画像から文字を取得する
        /// </summary>
        /// <returns>Stream</returns>
        public static async Task<HttpResponseMessage> GetOCRData(Stream stream, string SubscriptionKey, TraceWriter log)
        {
            var OCRResponse = new HttpResponseMessage();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Computer vision APIのOCRにリクエスト
            // リクエストパラメータ作成
            queryString["language"] = "unk";
            queryString["detectOrientation"] = "true";
            var uri = @"https://japaneast.api.cognitive.microsoft.com/vision/v1.0/ocr?" + queryString;

            // Computer vision APIのOCRにリクエスト
            using (var getOCRDataClient = new HttpClient())
            {
                // ComputerVisionAPIへのリクエスト情報を作成
                HttpRequestMessage OCRRequest = new HttpRequestMessage(HttpMethod.Post, uri);
                OCRRequest.Content = new StreamContent(stream);
                OCRRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // リクエストヘッダーの作成
                getOCRDataClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", $"{SubscriptionKey}");

                OCRResponse = await getOCRDataClient.SendAsync(OCRRequest);
            }

            return OCRResponse;
        }

        /// <summary>
        /// ComputerVisionAPIのレスポンスメッセージをパース
        /// </summary>
        /// <returns>Stream</returns>
        public static async Task<string> GetParseString(HttpResponseMessage OCRResponse, TraceWriter log)
        {
            // ComputerVisionAPIのレスポンスをパースして文章に
            var jsonContent = await OCRResponse.Content.ReadAsStringAsync();

            log.Info(jsonContent);

            OCR_Response ocr_data = JsonConvert.DeserializeObject<OCR_Response>(jsonContent);

            string words = String.Empty;

            if (ocr_data.regions.Count > 0)
            {
                // OCRで取得した文字列をパースして文章にする。
                foreach (var regions in ocr_data.regions)
                {
                    foreach (var line in regions.lines)
                    {
                        foreach (var word in line.words)
                        {
                            words = words + word.text + " ";
                        }
                        words = words + Environment.NewLine;
                    }
                }
            }
            else
            {
                words = "There is no words!!";
            }

            return words;
        }
    }

    // OCRから返却されたデータ
    public class OCR_Response
    {
        public string language { get; set; }
        public double textAngle { get; set; }
        public string orientation { get; set; }
        public List<Region> regions { get; set; }
    }
    public class Region
    {
        public string boundingBox { get; set; }
        public List<Line> lines { get; set; }
    }
    public class Line
    {
        public string boundingBox { get; set; }
        public List<Word> words { get; set; }
    }
    public class Word
    {
        public string boundingBox { get; set; }
        public string text { get; set; }
    }
}
