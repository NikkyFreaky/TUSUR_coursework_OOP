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
        // Обработчик изображений
        private readonly ImageHandler _imageHandler;

        // Извлекатель цветов
        private readonly ColorExtractor _colorExtractor;

        // Конструктор главного окна
        public MainWindow()
        {
            InitializeComponent();

            // Инициализация обработчика изображений и извлекателя цветов
            _imageHandler = new ImageHandler();
            _colorExtractor = new ColorExtractor();
        }

        // Обработчик нажатия на кнопку загрузки изображения
        private void ImgUpload_Click(object sender, RoutedEventArgs e)
        {
            // Загрузка изображения
            BitmapImage image = _imageHandler.OpenImage();

            // Если изображение не загружено, выходим из обработчика
            if (image == null) return;

            // Отображение загруженного изображения
            ImgDynamic.Source = image;

            // Запуск задачи в отдельном потоке
            Task.Run(() =>
            {
                // Клонирование изображения для работы в отдельном потоке
                BitmapSource clone = _imageHandler.CloneBitmapSource(image);

                // Включение индикатора обработки в основном потоке
                Dispatcher.Invoke(() => ProcessingBar.Visibility = Visibility.Visible);

                // Получение пяти наиболее часто встречающихся цветов
                List<Color> topColors = _colorExtractor.GetTopColors(clone, 5);

                // Обновление интерфейса в основном потоке
                Dispatcher.Invoke(() =>
                {
                    // Отображение наиболее часто встречающихся цветов
                    DisplayTopColors(topColors);

                    // Отключение индикатора обработки
                    ProcessingBar.Visibility = Visibility.Hidden;
                });
            });
        }

        // Обработчик нажатия на кнопку копирования цвета
        private void CopyColor_Click(object sender, RoutedEventArgs e)
        {
            // Копирование выбранного цвета в буфер обмена
            if (sender is Button button)
            {
                Clipboard.SetText(button.Name == "CopyRGB" ? TextRGB.Text : TextHEX.Text);
            }
        }

        // Обработчик нажатия на изображение
        private void ImgDynamic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Получение точки нажатия и цвета в этой точке
            Point clickPoint = e.GetPosition(ImgDynamic);
            Color color = _colorExtractor.GetColorAtPoint((BitmapSource)ImgDynamic.Source, clickPoint, ImgDynamic.ActualWidth, ImgDynamic.ActualHeight);

            // Отображение информации о выбранном цвете
            DisplayColorInfo(color);
        }

        // Метод для отображения информации о цвете
        private void DisplayColorInfo(Color color)
        {
            // Отображение цвета и его кодов RGB и HEX
            ColorPreview.Background = new SolidColorBrush(color);
            TextRGB.Text = $"RGB: {color.R}, {color.G}, {color.B}";
            TextHEX.Text = $"HEX: #{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        // Метод для отображения наиболее часто встречающихся цветов
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

    public class ImageHandler
    {
        // Фильтр для диалогового окна выбора файла с изображением
        private const string ImageFilter = "Файлы изображений (*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd)|*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd";

        // Метод для открытия изображения
        public BitmapImage OpenImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = ImageFilter
            };

            // Открываем диалог выбора файла и загружаем изображение, если файл был выбран
            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                var sourceImage = new BitmapImage(fileUri);

                // Создаем новое изображение, только если его размер больше определенного
                return sourceImage.Width > 1280 || sourceImage.Height > 720
                    ? ResizeImage(sourceImage, 1280, 720)
                    : sourceImage;
            }

            return null;
        }

        // Метод для клонирования источника изображения
        public BitmapSource CloneBitmapSource(BitmapSource source)
        {
            return source.Dispatcher.Invoke(() =>
            {
                // Если исходное изображение заморожено, просто возвращаем его
                if (source.IsFrozen)
                {
                    return source;
                }

                // Если исходное изображение можно заморозить, замораживаем его и возвращаем
                if (source.CanFreeze)
                {
                    source.Freeze();
                    return source;
                }

                // Если исходное изображение нельзя заморозить, создаем его копию
                var copy = new WriteableBitmap(source);
                copy.Freeze();
                return copy;
            });
        }

        // Метод для изменения размера изображения
        private BitmapImage ResizeImage(BitmapImage sourceImage, int maxWidth, int maxHeight)
        {
            // Вычисляем соотношение сторон для сохранения пропорций изображения
            double ratio = Math.Min(maxWidth / sourceImage.Width, maxHeight / sourceImage.Height);

            // Вычисляем новые размеры изображения
            var targetWidth = (int)(sourceImage.Width * ratio);
            var targetHeight = (int)(sourceImage.Height * ratio);

            // Создаем новое изображение с измененными размерами
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
            // Если источник пуст, возвращаем цвет по умолчанию
            if (bitmapSource == null)
            {
                return default(Color);
            }

            // Вычисляем масштаб изображения
            double scaleX = bitmapSource.PixelWidth / imageWidth;
            double scaleY = bitmapSource.PixelHeight / imageHeight;

            // Вычисляем координаты точки в пикселях
            int x = (int)(point.X * scaleX);
            int y = (int)(point.Y * scaleY);

            // Проверяем, находится ли точка внутри изображения
            if (x >= 0 && x < bitmapSource.PixelWidth && y >= 0 && y < bitmapSource.PixelHeight)
            {
                // Извлекаем и возвращаем цвет из точки на изображении
                return ExtractColor(bitmapSource, x, y);
            }

            // Если точка находится за пределами изображения, возвращаем цвет по умолчанию
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
                // Если формат пикселей не поддерживается, выбрасываем исключение
                throw new NotSupportedException("Неподдерживаемый формат пикселей");
            }
        }

        // Метод для получения списка самых популярных цветов
        public List<Color> GetTopColors(BitmapSource bitmapSource, int topCount)
        {
            int rudenessLevel = 16;

            // Если источник пуст, возвращаем null
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
                    // Извлекаем цвет из каждого пикселя
                    var color = ExtractColor(bitmapSource, x, y);

                    // Объединяем похожие цвета, уменьшая точность каждого компонента цвета
                    color = Color.FromArgb(
                        QuantizeColorComponent(color.A, rudenessLevel),
                        QuantizeColorComponent(color.R, rudenessLevel),
                        QuantizeColorComponent(color.G, rudenessLevel),
                        QuantizeColorComponent(color.B, rudenessLevel)
                    );

                    // Обновляем или добавляем цвет в словарь, увеличивая его счетчик
                    colorCount.AddOrUpdate(color, 1, (c, count) => count + 1);
                }
            });

            // Сортируем словарь по количеству каждого цвета в обратном порядке
            // и берем первые 'topCount' цветов
            // Возвращаем список самых популярных цветов
            return colorCount
                .OrderByDescending(kvp => kvp.Value)
                .Take(topCount)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        // Метод для уменьшения точности компонента цвета
        // Это помогает объединить похожие цвета в один
        private byte QuantizeColorComponent(byte colorComponent, int ton)
        {
            return (byte)((colorComponent / ton) * ton + ton / 2);
        }
    }
}