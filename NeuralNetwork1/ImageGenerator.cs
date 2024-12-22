using AForge.Imaging.Filters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetwork1
{
    /// <summary>
    /// Тип фигуры
    /// </summary>
    //public enum FigureType : byte { Triangle = 0, Rectangle, Circle, Sinusiod, Undef };
    //public enum FigureType : byte { MachineWash = 0, WashCarefully, Chlorine, Ironing110, NoIron, YesDry, OvenDry, NoDry, DrySort, NoWash, Undef };
    public enum FigureType : byte { MachineWash = 0, WashCarefully, Chlorine, Ironing110, NoIron, YesDry, OvenDry, NoDry, DrySort, NoWash, Undef };

    public class GenerateImage
    {
        /// <summary>
        /// Бинарное представление образа
        /// </summary>
        public bool[,] img = new bool[100, 100];

        //  private int margin = 50;
        private Random rand = new Random();

        /// <summary>
        /// Текущая сгенерированная фигура
        /// </summary>
        public FigureType currentFigure = FigureType.Undef;

        /// <summary>
        /// Количество классов генерируемых фигур (4 - максимум)
        /// </summary>
        public int FigureCount { get; set; } = 10;

        /// <summary>
        /// Диапазон смещения центра фигуры (по умолчанию +/- 20 пикселов от центра)
        /// </summary>
        public int FigureCenterGitter { get; set; } = 50;

        /// <summary>
        /// Диапазон разброса размера фигур
        /// </summary>
        //public int FigureSizeGitter { get; set; } = 50;

        /// <summary>
        /// Диапазон разброса размера фигур
        /// </summary>
        //public int FigureSize { get; set; } = 100;
        string rootFolderPath = @"C:\Users\Арина\Downloads\NeuralNetrworkTLGBot_2021\NeuralNetrworkTLGBot\NeuralNetwork1\bin\Debug\imgs"; // Укажите путь к папке imgs
        static int targetWidth = 100; // Целевая ширина
        static int targetHeight = 100; // Целевая высота


        // ----------------------------- новые штуки -------------------------------------------------------------------------


        // Создаем массив массивов для хранения обработанных изображений
        public Bitmap[][] processedImages = new Bitmap[10][];



        static Bitmap CropImage(Bitmap original, int width, int height)
        {
            // Вычисляем координаты для центрированного обрезания
            int cropX = (original.Width - width) / 2;
            int cropY = (original.Height - height) / 2;

            // Убедимся, что координаты находятся в пределах изображения
            cropX = Math.Max(0, cropX);
            cropY = Math.Max(0, cropY);

            Rectangle cropRect = new Rectangle(cropX, cropY, width, height);

            // Обрезаем изображение
            return original.Clone(cropRect, original.PixelFormat);
        }

        static Bitmap BinarizeImage(Bitmap original)
        {
            // Создаем новое изображение в оттенках серого
            Bitmap binaryImage = new Bitmap(original.Width, original.Height);

            for (int x = 0; x < original.Width; x++)
            {
                for (int y = 0; y < original.Height; y++)
                {
                    // Получаем цвет текущего пикселя
                    Color originalColor = original.GetPixel(x, y);

                    // Преобразуем его в оттенки серого
                    int grayValue = (originalColor.R + originalColor.G + originalColor.B) / 3;

                    // Преобразуем в черно-белый (бинарный)
                    Color binaryColor = grayValue > 100 ? Color.White : Color.Black;

                    // Устанавливаем новый цвет пикселя
                    binaryImage.SetPixel(x, y, binaryColor);
                }
            }

            return binaryImage;
        }

        static Bitmap ResizeImage(Bitmap original, int width, int height)
        {
            // Создаем новое изображение нужного размера
            Bitmap resizedImage = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                // Настраиваем качество интерполяции
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                // Копируем изображение с изменением размера
                graphics.DrawImage(original, 0, 0, width, height);
            }

            return resizedImage;
        }
        // Изменяем FillBtmp для выполнения новых этапов обработки
        public Action<int> ProgressCallback { get; set; } // Делегат для передачи прогресса

        public void FillBtmp()
        {
            int completedTasks = 0;

            string outputFolder = @"C:\Users\Арина\Downloads\NeuralNetrworkTLGBot_2021\NeuralNetrworkTLGBot\NeuralNetwork1\bin\Debug\ProcessedImages";
            Directory.CreateDirectory(outputFolder); // Создаем папку, если она не существует

            for (int folderIndex = 0; folderIndex < FigureCount; folderIndex++)
            {
                string folderPath = Path.Combine(rootFolderPath, folderIndex.ToString());
                if (Directory.Exists(folderPath))
                {
                    string[] imagePaths = Directory.GetFiles(folderPath, "*.jpg");
                    processedImages[folderIndex] = new Bitmap[imagePaths.Length];

                    Parallel.For(0, imagePaths.Length, i =>
                    {
                        using (Bitmap original = new Bitmap(imagePaths[i]))
                        {
                            // Бинаризуем изображение
                            Bitmap binaryImage = BinarizeImage(original);

                            processedImages[folderIndex][i] = binaryImage;

                            // Сохраняем обработанное изображение
                            string outputFileName = Path.Combine(outputFolder, $"from_btmp_folder{folderIndex}_image{i}.jpg");
                            binaryImage.Save(outputFileName, ImageFormat.Jpeg);

                            // Освобождаем память
                            //binaryImage.Dispose();
                        }
                    });
                }
                else
                {
                    processedImages[folderIndex] = new Bitmap[0];
                }

                // Обновляем прогресс
                completedTasks++;
                ProgressCallback?.Invoke((completedTasks * 100) / FigureCount);
            }
        }


        /// <summary>
        /// Очистка образа
        /// </summary>
        public void ClearImage()
        {
            for (int i = 0; i < targetHeight; ++i)
                for (int j = 0; j < targetWidth; ++j)
                    img[i, j] = false;
        }
        Random random = new Random();
        int folderIndex = 0;
        int imageIndex = 0;
        public Sample GenerateFigure()
        {
            folderIndex = random.Next(0, FigureCount);

            // Проверяем, есть ли изображения в выбранной папке
            if (processedImages[folderIndex] == null || processedImages[folderIndex].Length == 0)
            {
                throw new InvalidOperationException($"Папка {folderIndex} не содержит изображений.");
            }
            // Генерируем случайный индекс изображения внутри папки
            imageIndex = random.Next(0, processedImages[folderIndex].Length);
            // Получаем случайное изображение
            Bitmap resizedImage = processedImages[folderIndex][imageIndex];

            double noiseProbability = 0.005;
            for (int i = 0; i < targetHeight; i++)
            {
                for (int j = 0; j < targetWidth; j++)
                {
                    //if (noise)
                    //{
                    if (random.NextDouble() < noiseProbability)
                    {
                        resizedImage.SetPixel(i, j, Color.Black);
                    }
                    //}
                }
            }
            double[] input = new double[603];
            for (int i = 0; i < 603; i++)
            {
                input[i] = 0;
            }

            int blackPixelCount = 0;
            int iSum = 0;
            int jSum = 0;

            for (int i = 0; i < targetHeight; i++)
            {
                int rowChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int j = 1; j < targetHeight - 1; j++)
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

            for (int j = 0; j < targetHeight; j++)
            {
                int colChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int i = 1; i < targetHeight - 1; i++)
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
            input[602] = (double)blackPixelCount / (targetHeight * targetHeight);


            // Создаем Sample
            return new Sample(input, FigureCount, (FigureType)folderIndex);
        }
        public Sample GenerateSampleOnBitmapWithRes(Bitmap resizedImage, FigureType ft)
        {
            double[] input = new double[603];
            for (int i = 0; i < 603; i++)
            {
                input[i] = 0;
            }

            int blackPixelCount = 0;
            int iSum = 0;
            int jSum = 0;

            for (int i = 0; i < targetHeight; i++)
            {
                int rowChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int j = 1; j < targetHeight - 1; j++)
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

            for (int j = 0; j < targetHeight; j++)
            {
                int colChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int i = 1; i < targetHeight - 1; i++)
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
            input[602] = (double)blackPixelCount / (targetHeight * targetHeight);


            // Создаем Sample
            return new Sample(input, FigureCount, ft);
        }

        public SamplesSet GetTrain()
        {
            string outputFolder = @"C:\Users\Арина\Downloads\NeuralNetrworkTLGBot_2021\NeuralNetrworkTLGBot\NeuralNetwork1\bin\Debug\TrainImgs";
            Directory.CreateDirectory(outputFolder); // Создаем папку, если она не существует
            var ss = new SamplesSet();
            for (int i = 0; i < FigureCount; i++)
            {
                for (int j = 0; j < ((int)(processedImages[i].Length * 0.8)); j++)
                {
                    ss.AddSample(GenerateSampleOnBitmapWithRes(processedImages[i][j], (FigureType)i));
                    string outputFileName = Path.Combine(outputFolder, $"folder{i}_image{j}.jpg");
                    processedImages[i][j].Save(outputFileName, ImageFormat.Jpeg);

                }
            }
            return ss;
        }

        public SamplesSet GetTest()
        {
            string outputFolder = @"C:\Users\Арина\Downloads\NeuralNetrworkTLGBot_2021\NeuralNetrworkTLGBot\NeuralNetwork1\bin\Debug\TestImgs";
            Directory.CreateDirectory(outputFolder); // Создаем папку, если она не существует
            var ss = new SamplesSet();
            for (int i = 0; i < FigureCount; i++)
            {
                for (int j = ((int)(processedImages[i].Length * 0.8)); j < processedImages[i].Length; j++)
                {
                    ss.AddSample(GenerateSampleOnBitmapWithRes(processedImages[i][j], (FigureType)i));
                    string outputFileName = Path.Combine(outputFolder, $"folder{i}_image{j}.jpg");
                    processedImages[i][j].Save(outputFileName, ImageFormat.Jpeg);
                }
            }
            return ss;
        }

        static public Sample GenerateSampleOnBitmap(Bitmap resizedImage)
        {
            double[] input = new double[603];
            for (int i = 0; i < 603; i++)
            {
                input[i] = 0;
            }

            int blackPixelCount = 0;
            int iSum = 0;
            int jSum = 0;

            for (int i = 0; i < targetHeight; i++)
            {
                int rowChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int j = 1; j < targetHeight - 1; j++)
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

            for (int j = 0; j < targetHeight; j++)
            {
                int colChanges = 0;
                int maxLength = 0;
                int currentLength = 0;

                for (int i = 1; i < targetHeight - 1; i++)
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
            input[602] = (double)blackPixelCount / (targetHeight * targetHeight);
            // Создаем Sample
            return new Sample(input, 10, (FigureType)10); /////////////////////////////////костыль!!!!!!!!!!!!!!!!!! (FigureCount)
        }
        ///// <summary>
        ///// Возвращает битовое изображение для вывода образа
        ///// </summary>
        ///// <returns></returns>
        public Bitmap GenBitmap() ////////////////////////////////////////////////////////////////////////////////
        {
            return processedImages[folderIndex][imageIndex];
        }
    }

}
