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
        private readonly ImageHandler _imageHandler;
        private readonly ColorExtractor _colorExtractor;

        public MainWindow()
        {
            InitializeComponent();
            _imageHandler = new ImageHandler();
            _colorExtractor = new ColorExtractor();
        }

        private void ImgUpload_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage image = _imageHandler.OpenImage();
            if (image == null) return;

            ImgDynamic.Source = image;
            Task.Run(() =>
            {
                BitmapSource clone = _imageHandler.CloneBitmapSource(image);

                Dispatcher.Invoke(() => ProcessingBar.Visibility = Visibility.Visible);
                List<Color> topColors = _colorExtractor.GetTopColors(clone, 5);
                Dispatcher.Invoke(() =>
                {
                    DisplayTopColors(topColors);
                    ProcessingBar.Visibility = Visibility.Hidden;
                });
            });
        }

        private void CopyColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Clipboard.SetText(button.Name == "CopyRGB" ? TextRGB.Text : TextHEX.Text);
            }
        }

        private void ImgDynamic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point clickPoint = e.GetPosition(ImgDynamic);
            Color color = _colorExtractor.GetColorAtPoint((BitmapSource)ImgDynamic.Source, clickPoint, ImgDynamic.ActualWidth, ImgDynamic.ActualHeight);
            DisplayColorInfo(color);
        }

        private void DisplayColorInfo(Color color)
        {
            ColorPreview.Background = new SolidColorBrush(color);
            TextRGB.Text = $"RGB: {color.R}, {color.G}, {color.B}";
            TextHEX.Text = $"HEX: #{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void DisplayTopColors(List<Color> colors)
        {
            Border[] colorContainers = { TopColor1, TopColor2, TopColor3, TopColor4, TopColor5 };
            TextBlock[] colorTexts = { TopColorText1, TopColorText2, TopColorText3, TopColorText4, TopColorText5 };

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < colors.Count; i++)
                {
                    colorContainers[i].Background = new SolidColorBrush(colors[i]);
                    colorTexts[i].Text = $"RGB: {colors[i].R}, {colors[i].G}, {colors[i].B}";
                }
            });
        }
    }

    public class ImageHandler
    {
        // Фильтр для диалога открытия файла
        private const string ImageFilter = "Файлы изображений (*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd)|*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd";

        // Метод для открытия изображения
        public BitmapImage OpenImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = ImageFilter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                var sourceImage = new BitmapImage(fileUri);

                // Создание нового изображения только при необходимости
                return sourceImage.Width > 1920 || sourceImage.Height > 1080
                    ? ResizeImage(sourceImage, 1920, 1080)
                    : sourceImage;
            }

            return null;
        }

        // Метод для клонирования источника изображения
        public BitmapSource CloneBitmapSource(BitmapSource source)
        {
            return source.Dispatcher.Invoke(() =>
            {
                if (source.IsFrozen)
                {
                    return source;
                }

                if (source.CanFreeze)
                {
                    source.Freeze();
                    return source;
                }

                // Если источник нельзя заморозить, создаем его копию
                var copy = new WriteableBitmap(source);
                copy.Freeze();
                return copy;
            });
        }

        // Метод для изменения размера изображения
        private BitmapImage ResizeImage(BitmapImage sourceImage, int maxWidth, int maxHeight)
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

    public class ColorExtractor
    {
        private const int BitsPerPixelByteRatio = 8;

        // Метод для получения цвета в заданной точке на изображении
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

        // Метод для извлечения цвета из определенной точки на изображении
        private Color ExtractColor(BitmapSource bitmapSource, int x, int y)
        {
            // Вычисляем шаг для извлечения пикселей
            int stride = (bitmapSource.PixelWidth * bitmapSource.Format.BitsPerPixel + 7) / BitsPerPixelByteRatio;

            // Создаем массив для хранения данных пикселей
            byte[] pixels = new byte[bitmapSource.Format.BitsPerPixel / BitsPerPixelByteRatio];

            // Копируем данные пикселей в массив
            bitmapSource.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, stride, 0);

            // В зависимости от формата пикселей возвращаем соответствующий цвет
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

        // Метод для получения списка самых популярных цветов
        public List<Color> GetTopColors(BitmapSource bitmapSource, int topCount)
        {
            int rudenessLevel = 16;

            if (bitmapSource == null)
            {
                return null;
            }

            var colorCount = new ConcurrentDictionary<Color, int>();

            // Проходим по всем пикселям изображения параллельно
            Parallel.For(0, bitmapSource.PixelWidth, x =>
            {
                for (int y = 0; y < bitmapSource.PixelHeight; y++)
                {
                    var color = ExtractColor(bitmapSource, x, y);

                    // Объединяем похожие цвета
                    color = Color.FromArgb(
                        QuantizeColorComponent(color.A, rudenessLevel),
                        QuantizeColorComponent(color.R, rudenessLevel),
                        QuantizeColorComponent(color.G, rudenessLevel),
                        QuantizeColorComponent(color.B, rudenessLevel)
                    );

                    // Обновляем или добавляем цвет в словарь
                    colorCount.AddOrUpdate(color, 1, (c, count) => count + 1);
                }
            });

            // Возвращаем список самых популярных цветов
            return colorCount
                .OrderByDescending(kvp => kvp.Value)
                .Take(topCount)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        // Метод для уменьшения точности компонента цвета
        private byte QuantizeColorComponent(byte colorComponent, int ton)
        {
            return (byte)((colorComponent / ton) * ton + ton / 2);
        }
    }

}