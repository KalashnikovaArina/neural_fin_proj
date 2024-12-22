using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace NeuralNetwork1
{
    class TLGBotik
    {
        public TelegramBotClient client = null;
        private readonly AIMLBotik aiml;
        public int x = 0;

        MagicEye proc = new MagicEye();

        //   GenerateImage generateImage = new GenerateImage();

        private UpdateTLGMessages formUpdater;

        private BaseNetwork perseptron = null;
        // CancellationToken - инструмент для отмены задач, запущенных в отдельном потоке
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        public string Username { get; }
        public TLGBotik(BaseNetwork net, UpdateTLGMessages updater)
        {
            aiml = new AIMLBotik();
            var botKey = System.IO.File.ReadAllText("..\\..\\botKey.txt");
            // generateImage.LoadImages();
            client = new TelegramBotClient(botKey);
            formUpdater = updater;
            perseptron = net;
        }

        public void SetNet(BaseNetwork net)
        {
            perseptron = net;
            formUpdater("Net updated!");
        }

        private async Task HandleUpdateMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            //  Тут очень простое дело - банально отправляем назад сообщения
            var message = update.Message;
            var chatId = message.Chat.Id;
            var username = message.Chat.FirstName;
            formUpdater("Тип сообщения : " + message.Type.ToString());

            //  Получение файла (картинки)
            if (message.Type == MessageType.Photo)
            {
                formUpdater("Picture loadining started");
                var photoId = message.Photo.Last().FileId;
                Telegram.Bot.Types.File fl = client.GetFileAsync(photoId).Result;
                var imageStream = new MemoryStream();
                await client.DownloadFileAsync(fl.FilePath, imageStream, cancellationToken: cancellationToken);
                var img = Image.FromStream(imageStream);

                Bitmap bm = new Bitmap(img);

                proc.ProcessImage(bm);
                var procImage = proc.processed;
                x++;
                procImage.Save("../../Images/image_" + x + ".png");
                var sensor = getBmp(procImage);
                Sample sample = new Sample(sensor, 7, FigureType.Undef);
                string lastSymbol = string.Empty;

                switch (perseptron.Predict(sample))
                {
                    case FigureType.MachineWash:
                        lastSymbol = "Машинная стирка при 30 градусах";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, машинная стирка при 30 градусах!");
                        break;
                    case FigureType.WashCarefully:
                        lastSymbol = "Аккуратная стирка при 30 градусах";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, аккуратная стирка при 30 градусах!"); 
                        break;
                    case FigureType.Chlorine:
                        lastSymbol = "можно использовать хлор";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, использование хлора допустимо!"); 
                        break;
                    case FigureType.Ironing110:
                        lastSymbol = "Глажка до температуры 110 градусов";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, глажка до 110 градусов!"); 
                        break;
                    case FigureType.NoIron:
                        lastSymbol = "Глажка запрещена";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, глажка запрещена!"); 
                        break;
                    case FigureType.YesDry:
                        lastSymbol = "сушка в машинке разрешена";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, сушка разрешена!"); 
                        break;
                    case FigureType.OvenDry:
                        lastSymbol = "сушка в духовке разрешена";
                        client.SendTextMessageAsync(message.Chat.Id, "Может быть высушено аккуратно в духовке!"); 
                        break;
                    case FigureType.NoDry:
                        lastSymbol = "сушка в стиральной машине запрещена";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, запрет сушить в стиральной машине!"); 
                        break;
                    case FigureType.DrySort:
                        lastSymbol = "сухой распределитель";
                        client.SendTextMessageAsync(message.Chat.Id, "Сухой распределитель!"); 
                        break;
                    case FigureType.NoWash:
                        lastSymbol = "стирка в машине запрещена";
                        client.SendTextMessageAsync(message.Chat.Id, "Это легко, запрет на стирку!"); 
                        break;
                    default: client.SendTextMessageAsync(message.Chat.Id, "Я такого не знаю!"); break;
                }
                
                formUpdater("Picture recognized!");
                return;
            }
            else if (message.Type == MessageType.Text)
            {
                var messageText = update.Message.Text;

                Console.WriteLine($"Received a '{messageText}' message in chat {chatId} with {username}.");

                // Echo received message text
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: aiml.Talk(chatId, username, messageText),
                    cancellationToken: cancellationToken);
                return;
            }

            if (message.Type == MessageType.Video)
            {
                await client.SendTextMessageAsync(message.Chat.Id, aiml.Talk(chatId, username, "Видео"), cancellationToken: cancellationToken);
                return;
            }
            if (message.Type == MessageType.Audio)
            {
                await client.SendTextMessageAsync(message.Chat.Id, aiml.Talk(chatId, username, "Аудио"), cancellationToken: cancellationToken);
                return;
            }
        }

        private double[] getBmp(Bitmap resizedImage)
        {
            double[] input = new double[603];
            for (int i = 0; i < 603; i++)
            {
                input[i] = 0;
            }

            int blackPixelCount = 0;
            int iSum = 0;
            int jSum = 0;

            for (int i = 0; i < 100; i++)
            {
                int rowChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int j = 1; j < 100 - 1; j++)
                {
                    if (resizedImage.GetPixel(i, j) != resizedImage.GetPixel(i, j - 1) && resizedImage.GetPixel(i, j) == resizedImage.GetPixel(i, j + 1))
                    {
                        rowChanges++;
                    }
                    if (resizedImage.GetPixel(i, j).R != 255 && resizedImage.GetPixel(i, j).G != 255 && resizedImage.GetPixel(i, j).B != 255)
                    {
                        currentLength++;
                        maxLength = Math.Max(maxLength, currentLength);
                        input[400 + i] += 1;
                        input[500 + j] += 1;
                        blackPixelCount++;
                        jSum += j;
                        iSum += i;
                    }
                    else
                    {
                        currentLength = 0;
                    }
                }
                input[i] = rowChanges;
                input[200 + i] = maxLength;
            }

            for (int j = 0; j < 100; j++)
            {
                int colChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int i = 1; i < 100 - 1; i++)
                {

                    if (resizedImage.GetPixel(i, j) != resizedImage.GetPixel(i - 1, j) && resizedImage.GetPixel(i, j) == resizedImage.GetPixel(i + 1, j))
                    {
                        colChanges++;
                    }
                    if (resizedImage.GetPixel(i, j).R != 255 && resizedImage.GetPixel(i, j).G != 255 && resizedImage.GetPixel(i, j).B != 255)
                    {
                        currentLength++;
                        maxLength = Math.Max(maxLength, currentLength);
                    }
                    else
                    {
                        currentLength = 0;
                    }
                }

                input[100 + j] = colChanges;
                input[300 + j] = maxLength;
            }

            input[600] = blackPixelCount > 0 ? (double)iSum / blackPixelCount : 0;
            input[601] = blackPixelCount > 0 ? (double)jSum / blackPixelCount : 0;
            input[602] = (double)blackPixelCount / (100 * 100);
            return input;
        }
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var apiRequestException = exception as ApiRequestException;
            if (apiRequestException != null)
                Console.WriteLine($"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}");
            else
                Console.WriteLine(exception.ToString());
            return Task.CompletedTask;
        }

        public bool Act()
        {
            try
            {
                client.StartReceiving(HandleUpdateMessageAsync, HandleErrorAsync, new ReceiverOptions
                {   // Подписываемся только на сообщения
                    AllowedUpdates = new[] { UpdateType.Message }
                },
                cancellationToken: cts.Token);
                // Пробуем получить логин бота - тестируем соединение и токен
                Console.WriteLine($"Connected as {client.GetMeAsync().Result}");
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public void Stop()
        {
            cts.Cancel();
        }

    }
}
