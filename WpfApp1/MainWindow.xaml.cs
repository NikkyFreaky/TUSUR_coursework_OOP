using ColorPickerApp.Classes;
using ColorPickerApp.Interfaces;
using Microsoft.Win32;
using System.Collections.Generic;
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
        private readonly IImageHandler _imageHandler;
        private readonly IImageCloner _imageCloner;
        private readonly IColorExtractor _colorExtractor;
        private readonly ITopColorFinder _topColorFinder;

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
}