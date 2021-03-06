﻿using System;
using System.Threading.Tasks;
using Discord;
using CoreTweet;
using Discord.WebSocket;
using CoreTweet.Streaming;

namespace Twitter2Discord
{
    class Program
    {
        static bool IsLogin = false;
        static OAuth.OAuthSession session;
        static string key = "TwitterApplicationのConsumerKey";
        static string secret = "TwitterApplicationのConsumerSecret";
        static Tokens tokens;

        static async Task MainAsync()
        {
            var client = new DiscordSocketClient();
            await client.LoginAsync(TokenType.Bot, "DiscordBotのToken");
            await client.StartAsync();
            client.MessageReceived += Receive;
            Console.ReadLine();
        }
        static void Main(string[] args) => MainAsync().Wait();
        static void SendTweet(SocketMessage msg) => SendTweetAsync(msg).Wait();
        static async Task SendTweetAsync(SocketMessage msg)
        {
            var stream = tokens.Streaming.User();
            foreach (var mes in stream)
            {
                switch (mes)
                {
                    case StatusMessage stm:
                        var stat = stm.Status;
                        await msg.Channel.SendMessageAsync($"```{stat.User.ScreenName}さんがツイートしました\n{stat.Text}\nLink:https://twitter.com/{stat.User.ScreenName}/status/{stat.Id}```");
                        break;
                }
            }
        }
        static async Task Receive(SocketMessage msg)
        {
            if (msg.Author.Username.Equals("Twitter2Discord"))
            {
                return;
            }
            switch (msg.Content)
            {
                case "login":
                    if (IsLogin)
                    {
                        return;
                    }
                    session = OAuth.Authorize(key, secret);
                    await msg.Channel.SendMessageAsync($"下記URLにアクセスし、認証したら「PIN:認証後に出てきた数字」と送信してください\n\n{session.AuthorizeUri.AbsoluteUri}");
                    IsLogin = true;

                    break;
                case "ping":
                    await msg.Channel.SendMessageAsync("pong");
                    break;
            }
            if (msg.Content.Contains("PIN:"))
            {
                if (!IsLogin)
                {
                    return;
                }
                try
                {
                    tokens = await OAuth.GetTokensAsync(session, msg.Content.Replace("PIN:", ""));
                    await msg.Channel.SendMessageAsync("ログインに成功しました");
                    await Task.Run(() => SendTweet(msg));
                }
                catch (Exception)
                {
                    await msg.Channel.SendMessageAsync("ログインに失敗しました、正しいPINコードを入力してください");
                }

            }

        }
    }
}
