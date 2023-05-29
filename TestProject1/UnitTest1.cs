using Microsoft.Win32;
using Moq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorPickerApp.Tests
{
    [TestFixture]
    public class ImageHandlerTests
    {
        private ImageHandler imageHandler;
        private OpenFileDialog openFileDialog;

        [SetUp]
        public void SetUp()
        {
            imageHandler = new ImageHandler();
            openFileDialog = new OpenFileDialog();
        }

        /*
        [Test]
        public void OpenImage_WithValidImage_ReturnsImage()
        {
            // Arrange
            string imagePath = "C:\\Users\\Nikita\\Desktop\\0.jpg";
            string extension = Path.GetExtension(imagePath).ToLower();

            // Создаем макет OpenFileDialog
            var openFileDialogMock = new Mock<OpenFileDialog>();

            // Устанавливаем ожидаемое поведение макета
            openFileDialogMock.Setup(o => o.ShowDialog()).Returns(true);
            openFileDialogMock.Setup(o => o.FileName).Returns(imagePath);

            // Act
            BitmapImage result = null;
            if (IsImageValid(extension))
            {
                result = imageHandler.OpenImage(openFileDialogMock.Object);
            }

            // Assert
            Assert.IsNotNull(result);
        }
        */

        [Test]
        public void OpenImage_WithInvalidImage_ReturnsNull()
        {
            // Arrange
            string imagePath = "F:\\Documents\\Visual Studio 2022\\Projects\\WpfApp1\\TestProject1\\Images\\11.heic";
            BitmapImage image = new BitmapImage(new Uri(imagePath));
            string extension = Path.GetExtension(imagePath).ToLower();

            // Act
            BitmapImage result = null;
            if (IsImageValid(extension))
            {
                result = imageHandler.OpenImage(openFileDialog);
            }

            // Assert
            Assert.IsNull(result);
        }

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

    [TestFixture]
    public class ColorExtractorTests
    {
        private ColorExtractor _colorExtractor;
        private Mock<BitmapSource> _mockBitmapSource;

        [SetUp]
        public void SetUp()
        {
            _colorExtractor = new ColorExtractor();

            _mockBitmapSource = new Mock<BitmapSource>();
            _mockBitmapSource.SetupGet(x => x.PixelWidth).Returns(100);
            _mockBitmapSource.SetupGet(x => x.PixelHeight).Returns(100);
            _mockBitmapSource.Setup(x => x.Format).Returns(PixelFormats.Bgr32);

            byte[] fakePixels = new byte[] { 0, 0, 255, 255 }; // red color
            _mockBitmapSource.Setup(x => x.CopyPixels(It.IsAny<Int32Rect>(), It.IsAny<Array>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback(new Action<Int32Rect, Array, int, int>((rect, pixels, stride, offset) =>
                {
                    fakePixels.CopyTo((byte[])pixels, 0);
                }));
        }

        [Test]
        public void ExtractColor_WhenCalled_ShouldReturnRedColor()
        {
            // Arrange
            int x = 50;
            int y = 50;

            // Act
            var result = _colorExtractor.ExtractColor(_mockBitmapSource.Object, x, y);

            // Assert
            Assert.That(result, Is.EqualTo(Colors.Red));
        }

        [Test]
        public void GetColorAtPoint_WhenCalled_ShouldReturnRedColor()
        {
            // Arrange
            var point = new Point(50, 50);

            // Act
            var result = _colorExtractor.GetColorAtPoint(_mockBitmapSource.Object, point, 100, 100);

            // Assert
            Assert.That(result, Is.EqualTo(Colors.Red));
        }
    }

    [TestFixture]
    public class TopColorFinderTests
    {
        [Test]
        public void GetTopColors_ReturnsTopColors()
        {
            // Arrange
            var bitmapSource = new BitmapImage(new Uri("F:\\Documents\\Visual Studio 2022\\Projects\\WpfApp1\\TestProject1\\Images\\0.jpg"));
            var colorExtractor = new ColorExtractor();

            var topColorFinder = new TopColorFinder();

            // Act
            bitmapSource.Freeze(); // Замораживаем объект BitmapSource
            var result = topColorFinder.GetTopColors(bitmapSource, 3, colorExtractor);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
            Assert.That(result.Count, Is.EqualTo(3));
        }
    }

}