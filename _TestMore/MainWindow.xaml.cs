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
                     //   Stream sm_ms = new MemoryStream(); sm_ms.Seek(0, SeekOrigin.Begin);
                     // ImageBasic.BasicMethodClass.MakeThumbnail(item, sm_ms, 256, 256, "W", "jpg");
                     //  var srcBitmap = new Bitmap(sm_ms);
                     //sm_ms.Dispose();
                    var srcBitmap = new Bitmap(item);
                     var VisualAttentionBitmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);
                    #region 测试FT后的区域选取函数
                    int width = VisualAttentionBitmap.Width, height = VisualAttentionBitmap.Height;
                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
                    BitmapData VisualAttentionBmData = VisualAttentionBitmap.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                    Bitmap areaImage = new Bitmap(width, height);
                    BitmapData areaBmData = areaImage.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    //准备切图类
                    var finimage = new Bitmap(item);
                    CutImageClass cuter = new CutImageClass(VisualAttentionBitmap, new System.Drawing.Rectangle(0, 0, 700, 200),215);
                    us_PixlPoint[] fx = cuter.FindArea(VisualAttentionBmData);
                    CutImageClass.DrawingArea(cuter.AreaArr, fx, areaBmData, VisualAttentionBmData);
                    VisualAttentionBitmap.UnlockBits(VisualAttentionBmData);

                    var GenerImage = cuter.GCSsimp_getLightPointFromSource(finimage);
                    areaImage.UnlockBits(areaBmData);
                    ////保持到磁盘
                    FileInfo file = new FileInfo(item);
                    string name = file.Name.Remove(file.Name.IndexOf(file.Extension), file.Extension.Count());
                    VisualAttentionBitmap.Save(name + "v" + file.Extension);
                    areaImage.Save(name + "a"+file.Extension);
                    GenerImage.Save(name + "c"+file.Extension);
                    File.Copy(item, file.Name,true);
                    ////
                    VisualAttentionBitmap.Dispose();
                    srcBitmap.Dispose();
                    finimage.Dispose();
                    #endregion
                    #region 准备数据
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

