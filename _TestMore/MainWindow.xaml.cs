using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using VisualAttentionDetection;
using CutImage;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.ObjectModel;
using OtherAppy;

namespace _TestMore
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

        public class Thkindofimage
        {
            public BitmapImage source { set; get; }
            public BitmapImage area { set; get; }
            public BitmapImage fin { set; get; }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string patch = dirpatchTextBox.Text;

            if (!Directory.Exists(patch)) 
                patch=Environment.CurrentDirectory+ @"\Image\";
            string[] Imagefiles = Directory.GetFiles(patch).Where(x => x.EndsWith(".jpg", true, null)
             || x.EndsWith(".png", true, null) || x.EndsWith(".bmp", true, null)).ToArray();
            ObservableCollection<Thkindofimage> images = new ObservableCollection<Thkindofimage>();
            ImageListBox.ItemsSource = images;
            Thread t1 = new Thread((ThreadStart)delegate
            {
                foreach (var item in Imagefiles)
                {
                    var srcBitmap = new Bitmap(item);
                    #region 测试FT后的区域选取函数
                    //准备切图类
                    CutImageClass cuter = new CutImageClass(srcBitmap, new System.Drawing.Rectangle(0, 0, 364, 240),215);
                    cuter.MakeThumbnail(364,240);
                    cuter.MakeVisualAttentionBitmap();
                    var areaImage = cuter.OutputAreaBitmap();
                    cuter.MakeCutBitmap();
                    var GenerImage = new Bitmap(cuter.CurDestBitmap);
                    #endregion

                    #region 准备视图数据
                    MemoryStream ms = new MemoryStream();
                    if (areaImage == null) continue ;
                    areaImage.Save(ms, ImageFormat.Jpeg);
                    areaImage.Dispose();
                    BitmapImage AreaBitImage = new BitmapImage();
                    AreaBitImage.BeginInit();
                    ms.Seek(0, SeekOrigin.Begin);
                    AreaBitImage.CacheOption = BitmapCacheOption.OnLoad;
                    AreaBitImage.StreamSource = ms;
                    AreaBitImage.EndInit();
                    AreaBitImage.Freeze();
                    ms.Dispose();
                    MemoryStream ms2 = new MemoryStream();
                    ms2.Seek(0, SeekOrigin.Begin);
                    if (GenerImage == null) { continue; }
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

                    Dispatcher.Invoke(delegate
                    {

                        images.Add(new Thkindofimage
                        {
                            source = new BitmapImage(new Uri(item)),
                            area = AreaBitImage,
                            fin = FinBitImage,
                        });
                    });
                    //GC.Collect(0,GCCollectionMode.Forced);
                }
            });
            #endregion
            t1.Start();
        }
    }
}

