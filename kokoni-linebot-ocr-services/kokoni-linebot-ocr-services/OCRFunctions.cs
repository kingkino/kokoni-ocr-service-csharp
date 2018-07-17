
using KokoniLinebotOCRServices.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace KokoniLinebotOCRServices
{
    public static class OCRFunctions
    {
        [FunctionName("OCR_Functions")]
        public static async Task<String> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, 
            TraceWriter log , 
            ExecutionContext context)
        {
            log.Info("Start");

            var config = new ConfigurationBuilder()
                             .SetBasePath(context.FunctionAppDirectory)
                             .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                             .AddEnvironmentVariables()
                             .Build();

            // リクエストJSONをパース
            var jsonContent = await req.ReadAsStringAsync();
            Request data = JsonConvert.DeserializeObject<Request>(jsonContent);

            string replyToken = null;
            string messageType = null;
            string messageId = null;

            string fileName = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + Guid.NewGuid().ToString();
            string containerName = "contents";

            Response content;
            Stream responsestream = new MemoryStream();

            // リクエストデータからデータを取得
            foreach (var item in data.events)
            {
                // リプライデータ送付時の認証トークンを取得
                replyToken = item.replyToken.ToString();
                if (item.message != null)
                {
                    // メッセージタイプを取得
                    messageType = item.message.type.ToString();
                    messageId = item.message.id.ToString();
                }
            }

            log.Info(messageId);

            if (messageType == "image")
            {
                // Lineから指定MessageIdの画像を再取得
                responsestream = await LineAPIController.GetLineContents(messageId);

#if DEBUG
                responsestream = await LineAPIController.GetLineContents(
                    messageId);
#else
                responsestream = await LineAPIController.GetLineContents(
                    messageId, 
                    config.GetSection("requestDomain").Value
                    );
#endif

                // ComputerVisionAPIにリクエストを送る
                var OCRResponse = await OCRAPIController.GetOCRData(
                    responsestream,
                    config.GetSection("SubscriptionKey").Value
                    );

                // 文字列をパース
                var words = await OCRAPIController.GetParseString(OCRResponse, log);

                // 文字列を翻訳
                var transword = await TranslateAPIController.GetTranslateWord(words, config.GetSection("TranslateAuthToken").Value, log);



                // リプライデータの作成
                content = LineAPIController.CreateResponse(replyToken, transword, log);

                // Line ReplyAPIにリクエスト
                await LineAPIController.PutLineReply(content);

                // ここは失敗してもいいのでtryしとく
                try
                {
                    // Lineから指定MessageIdの画像を取得
                    responsestream = await LineAPIController.GetLineContents(messageId);
                    // 取得した画像をAzure Storageに保存
                    await AzureStorageController.PutLineContentsToStorageAsync(responsestream, containerName, fileName);
                }
                catch { }
            }
            else
            {
                // リプライデータの作成
                content = LineAPIController.CreateResponse(replyToken, "Sorry! Picture Only!!", log);

                // Line ReplyAPIにリクエスト
                await LineAPIController.PutLineReply(content);
            }

            log.Info("END");

            return HttpStatusCode.OK.ToString();
        }
    }
}
