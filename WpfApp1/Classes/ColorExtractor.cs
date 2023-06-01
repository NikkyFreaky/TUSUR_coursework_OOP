using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorPickerApp.Interfaces;

namespace ColorPickerApp.Classes
{
    // Класс для извлечения цвета из изображения
    public class ColorExtractor : ImageProcessor, IColorExtractor
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
}
