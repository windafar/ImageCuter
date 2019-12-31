using CutImage;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace example
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Destimage_Loaded(object sender, RoutedEventArgs e)
        {
            CutImageClass cuter = new CutImageClass(new System.Drawing.Bitmap("pic1.jpg"), new System.Drawing.Rectangle(0, 0, 364, 240), 215);
            var GenerImage =cuter
                        .MakeThumbnail(364, 240)//可选，压缩当前结果
                        .MakeVisualAttentionBitmap()//建议，制作显著图
                        .MakeCutBitmap()//制作切图
                        .OutputCurrentDestImage();//输出

            MemoryStream ms2 = new MemoryStream();
            ms2.Seek(0, SeekOrigin.Begin);
            GenerImage.Save(ms2, ImageFormat.Jpeg);
            GenerImage.Dispose();
            BitmapImage FinBitImage = new BitmapImage();
            FinBitImage.BeginInit();
            ms2.Seek(0, SeekOrigin.Begin);
            FinBitImage.CacheOption = BitmapCacheOption.OnLoad;
            FinBitImage.StreamSource = ms2;
            FinBitImage.EndInit();
            FinBitImage.Freeze();
            ms2.Dispose();

            destimage.Source = FinBitImage;
        }
    }
}
