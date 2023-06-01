using ColorPickerApp.Interfaces;
using Microsoft.Win32;
using System;
using System.Windows.Media.Imaging;

namespace ColorPickerApp.Classes
{
    // Класс для работы с изображениями
    public class ImageHandler : ImageProcessor, IImageHandler
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
}
