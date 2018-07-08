﻿using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KokoniLinebotOCRServices.Library
{
    public class LineController
    {
        private static HttpClient client = new HttpClient();

        /// <summary>
        /// Lineからコンテンツを取得
        /// </summary>
        /// <returns>Stream</returns>
        public static async Task<Stream> GetLineContents(string messageId)
        {
            Stream responsestream = new MemoryStream();

            // 画像を取得するLine APIを実行

            //　認証ヘッダーを追加
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ConfigurationManager.AppSettings["ChannelAccessTokenOCR"]}");

            // 非同期でPOST
            var res = await client.GetAsync($"https://api.line.me/v2/bot/message/{messageId}/content");
            responsestream = await res.Content.ReadAsStreamAsync();

            return responsestream;
        }

        // <summary>
        /// Lineににreplyを送信する
        /// </summary>
        /// <returns>Stream</returns>
        public static async Task PutLineReply(Response content)
        {
            // JSON形式に変換
            var reqData = JsonConvert.SerializeObject(content);

            // リクエストデータを作成
            // ※HttpClientで[application/json]をHTTPヘッダに追加するときは下記のコーディングじゃないとエラーになる
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/reply");
            request.Content = new StringContent(reqData, Encoding.UTF8, "application/json");

            //　認証ヘッダーを追加
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ConfigurationManager.AppSettings["ChannelAccessTokenOCR"]}");

            // 非同期でPOST
            var res = await client.SendAsync(request);
        }

        /// <summary>
        /// リプライ情報の作成
        /// </summary>
        /// <param name="token"></param>
        /// <param name="translateWord"></param>
        /// <param name="log"></param>
        /// <returns>response</returns>
        public static Response CreateResponse(string token, string translateWord, TraceWriter log)
        {
            Response res = new Response();
            Messages msg = new Messages();

            // リプライトークンはリクエストに含まれるリプライトークンを使う
            res.replyToken = token;
            res.messages = new List<Messages>();

            // メッセージタイプがtext以外は単一のレスポンス情報とする
            msg.type = "text";
            msg.text = translateWord;
            res.messages.Add(msg);

            return res;
        } 
    }

    // ******************************************************
    //　リクエスト
    public class Request
    {
        public List<Event> events { get; set; }
    }
    public class Event
    {
        public string replyToken { get; set; }
        public string type { get; set; }
        public object timestamp { get; set; }
        public Source source { get; set; }
        public message message { get; set; }
    }
    public class Source
    {
        public string type { get; set; }
        public string userId { get; set; }
    }
    public class message
    {
        public string id { get; set; }
        public string type { get; set; }
        public string text { get; set; }
    }
    // ******************************************************

    // ******************************************************
    // レスポンス
    public class Response
    {
        public string replyToken { get; set; }
        public List<Messages> messages { get; set; }
    }

    // レスポンスメッセージ
    public class Messages
    {
        public string type { get; set; }
        public string text { get; set; }
    }
    // ******************************************************
}
