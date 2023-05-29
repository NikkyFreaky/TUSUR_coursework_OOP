using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorPickerApp
{
    public partial class MainWindow : Window
    {
        // Объявление объектов для обработки изображения, клонирования, извлечения цвета и поиска топ цветов
        private readonly ImageHandler _imageHandler;
        private readonly ImageCloner _imageCloner;
        private readonly ColorExtractor _colorExtractor;
        private readonly TopColorFinder _topColorFinder;

        public MainWindow()
        {
            // Инициализация компонентов
            InitializeComponent();

            // Инициализация объектов для обработки изображения, клонирования, извлечения цвета и поиска топ цветов
            _imageHandler = new ImageHandler();
            _imageCloner = new ImageCloner();
            _colorExtractor = new ColorExtractor();
            _topColorFinder = new TopColorFinder();
        }

        private void ImgUpload_Click(object sender, RoutedEventArgs e)
        {
            // Открытие изображения через ImageHandler
            OpenFileDialog openFileDialog = new OpenFileDialog();

            ImageHandler imageHandler = new ImageHandler();
            BitmapImage image = imageHandler.OpenImage(openFileDialog);

            // Если изображение не было загружено, то выходим из обработчика событий
            if (image == null) return;

            // Отображение загруженного изображения
            ImgDynamic.Source = image;

            // Запуск задачи в отдельном потоке
            Task.Run(() =>
            {
                // Клонирование изображения
                BitmapSource clone = _imageCloner.CloneBitmapSource(image);

                // Включение индикатора обработки
                Dispatcher.Invoke(() => ProcessingBar.Visibility = Visibility.Visible);

                // Извлечение топ 5 цветов из изображения
                List<Color> topColors = _topColorFinder.GetTopColors(clone, 5, _colorExtractor);

                // Обновление интерфейса
                Dispatcher.Invoke(() =>
                {
                    // Отображение топ цветов
                    DisplayTopColors(topColors);
                    // Выключение индикатора обработки
                    ProcessingBar.Visibility = Visibility.Hidden;
                });
            });
        }

        private void CopyColor_Click(object sender, RoutedEventArgs e)
        {
            // Если нажатая кнопка, то копирование соответствующего значения цвета в буфер обмена
            if (sender is Button button)
            {
                Clipboard.SetText(button.Name == "CopyRGB" ? TextRGB.Text : TextHEX.Text);
            }
        }

        private void ImgDynamic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Получение точки нажатия и извлечение цвета в этой точке
            Point clickPoint = e.GetPosition(ImgDynamic);
            Color color = _colorExtractor.GetColorAtPoint((BitmapSource)ImgDynamic.Source, clickPoint, ImgDynamic.ActualWidth, ImgDynamic.ActualHeight);

            // Отображение информации о выбранном цвете
            DisplayColorInfo(color);
        }

        private void DisplayColorInfo(Color color)
        {
            // Заполнение превью цвета и отображение значения в формате RGB и HEX
            ColorPreview.Background = new SolidColorBrush(color);
            TextRGB.Text = $"RGB: {color.R}, {color.G}, {color.B}";
            TextHEX.Text = $"HEX: #{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void DisplayTopColors(List<Color> colors)
        {
            // Массивы контейнеров и текстовых блоков для отображения цветов
            Border[] colorContainers = { TopColor1, TopColor2, TopColor3, TopColor4, TopColor5 };
            TextBlock[] colorTexts = { TopColorText1, TopColorText2, TopColorText3, TopColorText4, TopColorText5 };

            // Обновление интерфейса в основном потоке
            Dispatcher.Invoke(() =>
            {
                // Цикл по всем полученным цветам
                for (int i = 0; i < colors.Count; i++)
                {
                    // Установка цвета фона для каждого контейнера и отображение RGB кода цвета
                    colorContainers[i].Background = new SolidColorBrush(colors[i]);
                    colorTexts[i].Text = $"RGB: {colors[i].R}, {colors[i].G}, {colors[i].B}";
                }
            });
        }
    }

    public abstract class ImageProcessor
    {
        // Фильтр файлов для диалогового окна выбора изображений
        protected const string ImageFilter = "Файлы изображений (*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd)|*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd";
    }

    // Класс для работы с изображениями
    public class ImageHandler : ImageProcessor
    {
        // Открытие и возможное изменение размера изображения
        public BitmapImage OpenImage(OpenFileDialog openFileDialog)
        {
            openFileDialog.Filter = ImageFilter;

            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                var sourceImage = new BitmapImage(fileUri);

                // Если размер изображения больше заданного, изменяем его
                return sourceImage.Width > 1280 || sourceImage.Height > 720
                    ? ResizeImage(sourceImage, 1280, 720)
                    : sourceImage;
            }

            return null;
        }

        // Изменение размера изображения
        public BitmapImage ResizeImage(BitmapImage sourceImage, int maxWidth, int maxHeight)
        {
            double ratio = Math.Min(maxWidth / sourceImage.Width, maxHeight / sourceImage.Height);

            var targetWidth = (int)(sourceImage.Width * ratio);
            var targetHeight = (int)(sourceImage.Height * ratio);

            var resizedImage = new BitmapImage();
            resizedImage.BeginInit();
            resizedImage.UriSource = sourceImage.UriSource;
            resizedImage.DecodePixelWidth = targetWidth;
            resizedImage.DecodePixelHeight = targetHeight;
            resizedImage.EndInit();

            return resizedImage;
        }
    }

    // Класс для клонирования изображения
    public class ImageCloner : ImageProcessor
    {
        public BitmapSource CloneBitmapSource(BitmapSource source)
        {
            // Метод Dispatcher.Invoke() используется для выполнения кода в потоке, в котором был создан Dispatcher
            return source.Dispatcher.Invoke(() =>
            {
                // Если исходник заморожен, просто возвращаем его
                if (source.IsFrozen)
                {
                    return source;
                }

                // Если исходник может быть заморожен, мы замораживаем и возвращаем его
                if (source.CanFreeze)
                {
                    source.Freeze();
                    return source;
                }

                // Клонируем исходное изображение
                var copy = new WriteableBitmap(source);
                copy.Freeze();
                return copy;
            });
        }
    }

    // Класс для извлечения цвета из изображения
    public class ColorExtractor : ImageProcessor
    {
        private const int BitsPerPixelByteRatio = 8;

        // Получение цвета в заданной точке изображения
        public Color GetColorAtPoint(BitmapSource bitmapSource, Point point, double imageWidth, double imageHeight)
        {
            if (bitmapSource == null)
            {
                return default(Color);
            }

            double scaleX = bitmapSource.PixelWidth / imageWidth;
            double scaleY = bitmapSource.PixelHeight / imageHeight;

            int x = (int)(point.X * scaleX);
            int y = (int)(point.Y * scaleY);

            if (x >= 0 && x < bitmapSource.PixelWidth && y >= 0 && y < bitmapSource.PixelHeight)
            {
                return ExtractColor(bitmapSource, x, y);
            }

            return default(Color);
        }

        // Извлечение цвета из конкретного пикселя изображения
        public Color ExtractColor(BitmapSource bitmapSource, int x, int y)
        {
            int stride = (bitmapSource.PixelWidth * bitmapSource.Format.BitsPerPixel + 7) / BitsPerPixelByteRatio;
            byte[] pixels = new byte[bitmapSource.Format.BitsPerPixel / BitsPerPixelByteRatio];

            bitmapSource.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, stride, 0);

            if (bitmapSource.Format == PixelFormats.Bgr32 || bitmapSource.Format == PixelFormats.Bgra32)
            {
                return Color.FromRgb(pixels[2], pixels[1], pixels[0]);
            }
            else if (bitmapSource.Format == PixelFormats.Rgb24)
            {
                return Color.FromRgb(pixels[0], pixels[1], pixels[2]);
            }
            else
            {
                throw new NotSupportedException("Неподдерживаемый формат пикселей");
            }
        }
    }

    // Класс для определения самых часто встречающихся цветов в изображении
    public class TopColorFinder : ImageProcessor
    {
        public List<Color> GetTopColors(BitmapSource bitmapSource, int topCount, ColorExtractor colorExtractor)
        {
            int rudenessLevel = 16;

            if (bitmapSource == null)
            {
                return null;
            }

            var colorCount = new ConcurrentDictionary<Color, int>();

            // Проходим по всем пикселям изображения
            Parallel.For(0, bitmapSource.PixelWidth, x =>
            {
                for (int y = 0; y < bitmapSource.PixelHeight; y++)
                {
                    var color = colorExtractor.ExtractColor(bitmapSource, x, y);

                    // Квантование цвета
                    color = Color.FromArgb(
                        QuantizeColorComponent(color.A, rudenessLevel),
                        QuantizeColorComponent(color.R, rudenessLevel),
                        QuantizeColorComponent(color.G, rudenessLevel),
                        QuantizeColorComponent(color.B, rudenessLevel)
                    );

                    // Обновляем словарь с подсчетом количества каждого цвета
                    colorCount.AddOrUpdate(color, 1, (c, count) => count + 1);
                }
            });

            // Возвращаем топ самых встречающихся цветов
            return colorCount
                .OrderByDescending(kvp => kvp.Value)
                .Take(topCount)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        // Квантование цвета
        private byte QuantizeColorComponent(byte colorComponent, int ton)
        {
            return (byte)((colorComponent / ton) * ton + ton / 2);
        }
    }
}