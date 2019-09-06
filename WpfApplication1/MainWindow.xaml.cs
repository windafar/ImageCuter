using System;
using System.Collections.Generic;
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
using VisualAttentionDetection;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using CutImage;
using System.Threading;
using ImageBasic;

namespace WpfApplication1
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
        //string filepath = Environment.CurrentDirectory + @"\Image\2259399638156672448.jpg";
        string filepath = @"C:\Users\yjdcb\Source\Repos\player\OSU-NetcloudMusic-player\PlayProjectGame\bin\Debug\cach\「プラスティック・メモリーズ」6巻特典,雨宮天,_A";
            //源图
        private void imagesrc_Loaded(object sender, RoutedEventArgs e)
        {
            imagesrc.Source = new BitmapImage(new Uri(filepath));
        }
        //显著性图
        private void VewImage_Loaded(object sender, RoutedEventArgs e)
        {
            MemoryStream mss = new MemoryStream();
            ImageBasic.BasicMethodClass.MakeThumbnail(filepath, mss, 1280, 768, "W", "jpg");
            var srcBitmap = new Bitmap(mss);

            #region 测试FT显著性函数
            Thread t1 = new Thread((ThreadStart)delegate
              {
                  var b = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);
                MemoryStream ms = new MemoryStream();
                  b.Save(ms, ImageFormat.Bmp);
                  BitmapImage BitImage = new BitmapImage();
                  BitImage.BeginInit();
                  BitImage.StreamSource = ms;
                  BitImage.EndInit();
                  BitImage.Freeze();
                  Dispatcher.Invoke(delegate
                  {
                      VewImage.Source = BitImage;
                  });

              });
            t1.Start();
            #endregion
        }
        //显著性图的区域统计图
        private void image_Loaded(object sender, RoutedEventArgs e)
        {
            Thread t1 = new Thread((ThreadStart)delegate
            {
                MemoryStream mss = new MemoryStream();
                ImageBasic.BasicMethodClass.MakeThumbnail(filepath, mss, 1280, 768, "W", "jpg");
                var srcBitmap = new Bitmap(mss);
              //  var srcBitmap = new Bitmap(filepath);

                var b = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);
                #region 测试高斯模糊函数以及RGB与LAB的互转函数
                // var b = VisualAttentionDetection.Test.TestGaussianSmooth(srcBitmap);
                #endregion

                #region 测试FT后的区域选取函数
                int width = b.Width, height = b.Height;
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
                BitmapData srcBmData = b.LockBits(rect,
                          ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                Bitmap dstBitmap = new Bitmap(width, height);
                BitmapData dstBmData = dstBitmap.LockBits(rect,
                          ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                us_PixlPoint[] fx;
                CutImageClass cuter = new CutImageClass(srcBitmap, rect);
                fx = cuter.FindArea(srcBmData);
                CutImageClass.DrawingArea(cuter.AreaArr, fx, dstBmData, srcBmData);
                b.UnlockBits(srcBmData);
                dstBitmap.UnlockBits(dstBmData);
                b = dstBitmap;
                #endregion

                #region 显示区域
                MemoryStream ms = new MemoryStream();
                b.Save(ms, ImageFormat.Bmp);
                BitmapImage BitImage = new BitmapImage();
                BitImage.BeginInit();
                BitImage.StreamSource = ms;
                BitImage.EndInit();
                BitImage.Freeze();
                Dispatcher.Invoke(delegate
                {
                    image.Source = BitImage;
                });

                #endregion
            });
            t1.Start();
        }
        //最终切割区域图
        private void imagecut_Loaded(object sender, RoutedEventArgs e)
        {
            Thread t1 = new Thread((ThreadStart)delegate
            {
               // MemoryStream mss = new MemoryStream();
               // ImageBasic.BasicMethodClass.MakeThumbnail(filepath, mss, 1280, 768, "W", "jpg");
               // var srcBitmap = new Bitmap(mss);
                var srcBitmap = new Bitmap(filepath);
                var x1 = DateTime.Now;
                var b = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);
                var x2 = DateTime.Now;
                var NewImage = new Bitmap(filepath);
                CutImageClass cuter = new CutImageClass(b, new System.Drawing.Rectangle(0, 0, NewImage.Width
                    , 256));
                b = cuter.GCSsimp_getLightPointFromSource(NewImage);
                //if (Math.Abs(b.Height - 256) > 3||Math.Abs(b.Width-NewImage.Width)>3) throw new Exception("not exceptation result");
                var x3 = DateTime.Now;
                #region 显示区域
                MemoryStream ms = new MemoryStream();
                b.Save(ms, ImageFormat.Bmp);
                BitmapImage BitImage = new BitmapImage();
                BitImage.BeginInit();
                BitImage.StreamSource = ms;
                BitImage.EndInit();
                BitImage.Freeze();
                
                Dispatcher.Invoke(delegate
                {
                    imagecut.Source = BitImage;
                    var x4 = DateTime.Now;
                    timeTextBlock.Text = "求显著图的时间：" + (x2 - x1).TotalMilliseconds + "/r/n"
                    + "求切图的时间：" + (x3 - x2).TotalMilliseconds.ToString() + "/r/n"
                    + "转换的时间：" + (x4 - x3).TotalMilliseconds.ToString() + "/r/n";
                });

                #endregion
            });
            t1.Start();
        }
    }
}
