using Microsoft.Win32;
using Moq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorPickerApp.Tests
{
    // Класс тестов для ImageHandler
    [TestFixture]
    public class ImageHandlerTests
    {
        // Объекты, которые будут использоваться в тестах
        private ImageHandler imageHandler;
        private OpenFileDialog openFileDialog;

        // Этот метод будет вызываться перед каждым тестом, он инициализирует тестовые объекты
        [SetUp]
        public void SetUp()
        {
            imageHandler = new ImageHandler();
            openFileDialog = new OpenFileDialog();
        }

        /*
        // Тестовый случай для проверки работы открытия изображения с подходящим расширением файла
        [Test]
        public void OpenImage_WithValidImage_ReturnsImage()
        {
            // Подготовка тестовых данных
            string imagePath = "C:\\Users\\Nikita\\Desktop\\0.jpg";
            string extension = Path.GetExtension(imagePath).ToLower();

            // Создаем макет OpenFileDialog
            var openFileDialogMock = new Mock<OpenFileDialog>();

            // Устанавливаем ожидаемое поведение макета
            openFileDialogMock.Setup(o => o.ShowDialog()).Returns(true);
            openFileDialogMock.Setup(o => o.FileName).Returns(imagePath);

            // Вызов тестируемого метода
            BitmapImage result = null;
            if (IsImageValid(extension))
            {
                result = imageHandler.OpenImage(openFileDialogMock.Object);
            }

            // Проверка результата
            Assert.IsNotNull(result);
        }
        */

        // Тестовый случай для проверки работы открытия изображения с неподходящим расширением файла
        [Test]
        public void OpenImage_WithInvalidImage_ReturnsNull()
        {
            // Подготовка тестовых данных
            string imagePath = "F:\\Documents\\Visual Studio 2022\\Projects\\WpfApp1\\TestProject1\\Images\\11.heic";
            BitmapImage image = new BitmapImage(new Uri(imagePath));
            string extension = Path.GetExtension(imagePath).ToLower();

            // Вызов тестируемого метода
            BitmapImage result = null;
            if (IsImageValid(extension))
            {
                result = imageHandler.OpenImage(openFileDialog);
            }

            // Проверка результата
            Assert.IsNull(result);
        }

        // Вспомогательный метод для проверки расширения файла изображения
        private bool IsImageValid(string extension)
        {
            // Фильтр файлов для диалогового окна выбора изображений
            const string ImageFilter = "Файлы изображений (*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd)|*.png;*.jpeg;*.jpg;*.gif;*.raw;*.tiff;*.bmp;*.psd";

            // Разделение фильтра на расширения
            string[] extensions = ImageFilter.Split('|')[1].Split(';');

            // Проверка соответствия расширения изображения фильтру
            foreach (string validExtension in extensions)
            {
                if (extension == validExtension.ToLower())
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Класс тестов для ColorExtractor
    [TestFixture]
    public class ColorExtractorTests
    {
        // Объекты, которые будут использоваться в тестах
        private ColorExtractor _colorExtractor;
        private Mock<BitmapSource> _mockBitmapSource;

        // Этот метод будет вызываться перед каждым тестом, он инициализирует тестовые объекты
        [SetUp]
        public void SetUp()
        {
            _colorExtractor = new ColorExtractor();

            // Создаем макет BitmapSource
            _mockBitmapSource = new Mock<BitmapSource>();
            _mockBitmapSource.SetupGet(x => x.PixelWidth).Returns(100);
            _mockBitmapSource.SetupGet(x => x.PixelHeight).Returns(100);
            _mockBitmapSource.Setup(x => x.Format).Returns(PixelFormats.Bgr32);

            // Создаем фейковые пиксели красного цвета
            byte[] fakePixels = new byte[] { 0, 0, 255, 255 };
            _mockBitmapSource.Setup(x => x.CopyPixels(It.IsAny<Int32Rect>(), It.IsAny<Array>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback(new Action<Int32Rect, Array, int, int>((rect, pixels, stride, offset) =>
                {
                    fakePixels.CopyTo((byte[])pixels, 0);
                }));
        }

        // Тестовый случай для проверки работы извлечения цвета из изображения
        [Test]
        public void ExtractColor_WhenCalled_ShouldReturnRedColor()
        {
            // Подготовка тестовых данных
            int x = 50;
            int y = 50;

            // Вызов тестируемого метода
            var result = _colorExtractor.ExtractColor(_mockBitmapSource.Object, x, y);

            // Проверка результата
            Assert.That(result, Is.EqualTo(Colors.Red));
        }

        // Тестовый случай для проверки работы получения цвета в указанной точке
        [Test]
        public void GetColorAtPoint_WhenCalled_ShouldReturnRedColor()
        {
            // Подготовка тестовых данных
            var point = new Point(50, 50);

            // Вызов тестируемого метода
            var result = _colorExtractor.GetColorAtPoint(_mockBitmapSource.Object, point, 100, 100);

            // Проверка результата
            Assert.That(result, Is.EqualTo(Colors.Red));
        }
    }

    // Класс тестов для TopColorFinder
    [TestFixture]
    public class TopColorFinderTests
    {
        // Тестовый случай для проверки работы поиска топовых цветов
        [Test]
        public void GetTopColors_ReturnsTopColors()
        {
            // Подготовка тестовых данных
            var bitmapSource = new BitmapImage(new Uri("F:\\Documents\\Visual Studio 2022\\Projects\\WpfApp1\\TestProject1\\Images\\0.jpg"));
            var colorExtractor = new ColorExtractor();

            var topColorFinder = new TopColorFinder();

            // Вызов тестируемого метода
            bitmapSource.Freeze(); // Замораживаем объект BitmapSource
            var result = topColorFinder.GetTopColors(bitmapSource, 3, colorExtractor);

            // Проверка результата
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
            Assert.That(result.Count, Is.EqualTo(3));
        }
    }

}