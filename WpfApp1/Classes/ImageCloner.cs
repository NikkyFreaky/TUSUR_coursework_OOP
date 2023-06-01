using System.Windows.Media.Imaging;
using ColorPickerApp.Interfaces;

namespace ColorPickerApp.Classes
{
    // Класс для клонирования изображения
    public class ImageCloner : ImageProcessor, IImageCloner
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
}
