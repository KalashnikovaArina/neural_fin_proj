﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIMLbot;
using AIMLbot.Utils;

namespace NeuralNetwork1
{
    class AIMLBotik
    {
        Bot bot;
        readonly Dictionary<long, User> users = new Dictionary<long, User>();

        public AIMLBotik()
        {
            string root = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            bot = new Bot();
            bot.loadSettings($"{root}\\config\\Settings.xml");
            bot.isAcceptingUserInput = false;
            AIMLLoader loader = new AIMLLoader(bot);
            loader.loadAIML($"{root}\\aiml");
            bot.isAcceptingUserInput = true;
        }
        public string Talk(long userId, string userName, string phrase)
        {
            var result = "";
            User user;
            if (!users.ContainsKey(userId))
            {
                user = new User(userId.ToString(), bot);
                users.Add(userId, user);
                Request r = new Request($"Меня зовут {userName}", user, bot);
                result += bot.Chat(r).Output + System.Environment.NewLine;
            }
            else
            {
                user = users[userId];
            }
            result += bot.Chat(new Request(phrase, user, bot)).Output;
            return result;
        }
    }
}
