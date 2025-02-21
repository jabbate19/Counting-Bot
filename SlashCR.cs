﻿using SlackNet.Bot;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;

namespace CountVonCount
{
    internal static class SlashCR
    {
        private enum Commands
        {
            ping,
            reset,
            //kys,
            setemoji,
            settimeout,
            help,
            config,
            timeleft
        }
        internal static void HandleSlashCommand(string command, IMessage message)
        {
            var args = command.Split(' ');
            if (Enum.TryParse(args[0].ToLower(), out Commands parse))
                switch (parse)
                {
                    case Commands.ping:
                        // uhhhhhhhhh
                        Thread pingThread = new(() =>
                        {
                            int? elapsedMs = null;
                            var dm = Counter.ConversationsApi!.OpenAndReturnInfo(new string[] { message.User.Id });
                            Stopwatch stopwatch = new();
                            stopwatch.Restart();
                            stopwatch.Start();
                            Counter.SendMessage(dm.Result.Channel.Id, "testing").Wait();
                            stopwatch.Stop();
                            elapsedMs = (int)stopwatch.ElapsedMilliseconds;

                            HandleDm(message, $"pong! {(elapsedMs is null ? "latency unavailable" : elapsedMs)}").Wait();
                        });
                        pingThread.Start();
                        break;
                    case Commands.reset:
                        Counter.ResetCount().Wait();
                        break;
                    //case "kys":
                    //    HandleDm(message, "Terminating... Mrs. Obama, its been an honor.").Wait();
                    //    Environment.Exit(-2);
                    //    break;
                    case Commands.setemoji:
                        //string emoji = args[2].Contains(':') ? args[2].Split(':')[1] : args[2];
                        string emoji = args[2];
                        if (args.Length < 3 || !emoji.Contains(':')) // the contains() is a workaround for the following issues v
                        {
                            HandleDm(message, @"Usage: \setemoji [ok | bad] <emoji>").Wait();
                            return;
                        }
                        emoji = emoji.Split(':')[1];
                        //bool ok = CheckEmoji(emoji).Result; // see the commented out method to understand why this hasnt been implemented yet
                        //if (ok)
                        switch (args[1].ToLower())
                        {
                            case "ok":
                                Config.OkCountEmoji = emoji;
                                break;
                            case "bad":
                                Config.BadCountEmoji = emoji;
                                break;
                            default:
                                HandleDm(message, @"Usage: \setemoji [ok | bad] <emoji>").Wait();
                                return;
                        }
                        //HandleDm(message, $"{(ok ? "Set successfully." : "Emoji not found.")}").Wait();
                        HandleDm(message, "Set successfully.").Wait();
                        break;
                    case Commands.settimeout: // yeah i think its a good time to learn regex, and I managed to figure out what I needed. so swag
                        if (args.Length != 2)
                        {
                            HandleDm(message, @"Usage: \settimeout <time>[d | h | m | s]").Wait();
                            return;
                        }
                        Match[] timeArgs = new Regex(@"\d+[s,m,h,d]").Matches(args[1].ToLower()).ToArray(); // holy crap vs has regex highlighting
                        if (timeArgs.Length < 1)
                        {
                            HandleDm(message, @"Usage: \settimeout<time>[d | h | m | s]").Wait();
                            return;
                        }
                        ulong timeOut = 0;
                        foreach (var timeArg in timeArgs)
                            if (ulong.TryParse(timeArg.Value[..^1], out var i))
                                timeOut += timeArg.Value[^1] switch
                                {
                                    'd' => i * 86400,
                                    'h' => i * 3600,
                                    'm' => i * 60,
                                    's' => i,
                                    _ => 0
                                };
                            else
                                HandleDm(message, @"Time parse failed. Usage: \settimeout <time>[d | h | m | s]").Wait();
                        Config.WaitTimeSeconds = timeOut;
                        Counter.SendMessage(Config.Channel!, $"Timeout is now {timeOut} seconds.").Wait();
                        HandleDm(message, "Set successfully.").Wait();
                        break;
                    case Commands.help:
                        HandleDm(message, $@"\[{string.Join(" | ", Enum.GetNames<Commands>().Where(s => !string.IsNullOrEmpty(s)))}] <args>").Wait();
                        break;
                    case Commands.config:
                        string config = "Current configuration { ";
                        foreach (var prop in typeof(Config).GetProperties(BindingFlags.Static | BindingFlags.NonPublic))
                            config += $"{prop.Name} ({prop.PropertyType}): {prop.GetValue(null, null)}, ";
                        HandleDm(message, $"{config[..^1]} }}").Wait();
                        break;
                    case Commands.timeleft:
                        for (int i = 0; i < Counter.contributors.Count; i++)
                            if (message.User.Id == Counter.contributors[i].ID)
                            {
                                ulong timeLeft = (ulong)(Config.WaitTimeSeconds! - (ulong)(double.Parse(message.Ts) - double.Parse(Counter.contributors[i].TimeStamp)));
                                HandleDm(message, $"{(timeLeft < Config.WaitTimeSeconds ? $"Seconds until next available count: {timeLeft}" : "You are all set to count again!")}").Wait();
                            }
                            else
                                HandleDm(message, "You are all set to count again!").Wait();
                        break;
                    default:
                        HandleDm(message, "Unknown command.").Wait();
                        break;
                }
            else
            {
                HandleDm(message, $@"\[{string.Join(" | ", Enum.GetNames<Commands>().Where(s => !string.IsNullOrEmpty(s)))}] <args>").Wait();
                return;
            }
            //DeleteMsg(message).Wait();
            Counter.ReactionsApi!.AddToMessage("ok", message.Conversation.Id, message.Ts).Wait();
        }

        #pragma warning disable CS1998, IDE0051
        // #warning The slack API Mentions that legacy bot users (such as 
        private static async Task<bool> CheckEmoji(string query)
        {
            throw new NotImplementedException();
            //foreach (var emoji in await Counter.EmojiApi!.List())
            //{
            //    if (emoji.Key == query)
            //        return true;
            //}
            //return false;
        }
        #pragma warning restore CS1998, IDE0051

        private static async Task HandleDm(IMessage message, string text)
        {
            var dm = await Counter.ConversationsApi!.OpenAndReturnInfo(new string[] { message.User.Id });
            await Counter.ChatApi!.PostMessage(new()
            {
                Channel = dm.Channel.Id,
                Text = text
            });
        }

        // #warning bot users cant delete messages??!
        private static async Task DeleteMsg(IMessage message) => throw new NotImplementedException(); //await Counter.ChatApi!.Delete(message.Ts, message.Conversation.Id);
    }
}
