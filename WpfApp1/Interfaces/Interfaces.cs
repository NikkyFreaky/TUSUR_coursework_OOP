using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorPickerApp.Interfaces
{
    public interface IImageHandler
    {
        BitmapImage OpenImage(OpenFileDialog openFileDialog);
        BitmapImage ResizeImage(BitmapImage sourceImage, int maxWidth, int maxHeight);
    }

    public interface IImageCloner
    {
        BitmapSource CloneBitmapSource(BitmapSource source);
    }

    public interface IColorExtractor
    {
        Color GetColorAtPoint(BitmapSource bitmapSource, Point point, double imageWidth, double imageHeight);
        Color ExtractColor(BitmapSource bitmapSource, int x, int y);
    }

    public interface ITopColorFinder
    {
        List<Color> GetTopColors(BitmapSource bitmapSource, int topCount, IColorExtractor colorExtractor);
    }
}
