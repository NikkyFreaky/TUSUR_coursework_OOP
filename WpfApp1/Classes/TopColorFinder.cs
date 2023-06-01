using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ColorPickerApp.Interfaces;

namespace ColorPickerApp.Classes
{
    // Класс для определения самых часто встречающихся цветов в изображении
    public class TopColorFinder : ImageProcessor, ITopColorFinder
    {
        public List<Color> GetTopColors(BitmapSource bitmapSource, int topCount, IColorExtractor colorExtractor)
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
