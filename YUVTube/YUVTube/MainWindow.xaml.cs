using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace YUVTube {
    public partial class MainWindow : Window {
        System.Windows.Threading.DispatcherTimer player = new System.Windows.Threading.DispatcherTimer();
        int maxFrame = 0;
        int width = 352;
        int height = 288;
        int type = 420;
        byte[] bytearr;
        BitmapSource bitmap;
        public static byte[] ArraySplitterr(byte[] arr, int start, int end) {
            byte[] splited = new byte[end - start];
            Array.Copy(arr, start, splited, 0, end - start);
            return splited;
        }
        public double Limiter(double c) { return Math.Round(c > 255 ? 255 : c < 0 ? 0 : c); }
        public BitmapSource YUV2RGB(byte[] Y, byte[] U, byte[] V) {
            byte[] rgba = new byte[width * height * 4];
            for (int h = 0; h < height; h++) {
                for (int w = 0; w < width; w++) {
                    var k = new int[2] { h * width + w, (type == 420 ? (h / 4) : type == 422 ? (h / 2) : h) * width + (type == 444 ? w : w / 2)};
                    var limitArr = new double[4] {
                        Limiter(1.164 * (Y[k[0]] - 16) + 2.018 * (U[k[1]] - 128)),
                        Limiter(1.164 * (Y[k[0]] - 16) - 0.813 * (V[k[1]] - 128) - 0.391 * (U[k[1]] - 128)),
                        Limiter(1.164 * (Y[k[0]] - 16) + 1.596 * (V[k[1]] - 128)),
                        255 };
                    for (int m = 0; m < 3; m++) rgba[4 * k[0] + m] = (byte)limitArr[m];
                }
            }
            return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, rgba, width * 4);
        }
        public void RenderFrame() {
            int[] limits = new int[4] { 0, width * height,
                type == 444 ? (2 * width * height) : type == 422 ? (int)(1.5 * width * height) : type == 420 ? (int)(1.25 * width * height) : 0,
                type == 444 ? (3 * width * height) : type == 422 ? (int)(2.0 * width * height) : type == 420 ? (int)(1.50 * width * height) : 0
            };
            double affect = type == 444 ? 3 : type == 422 ? 2 : type == 420 ? 1.5 : 0;
            maxFrame = bytearr != null ? bytearr.Length / (int)(width * height * affect) - 1 : -1;
            if (maxFrame == -1) return;
            slider.Maximum = maxFrame;
            int slide = (int)((int)slider.Value * width * height * affect);
            bitmap = YUV2RGB(ArraySplitterr(bytearr, slide + limits[0], slide + limits[1]),
                             ArraySplitterr(bytearr, slide + limits[1], slide + limits[2]),
                             ArraySplitterr(bytearr, slide + limits[2], slide + limits[3]));
            this.Title = "YUVTube - frame: " + ((int)slider.Value + 1) + "/" + (maxFrame + 1);
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var memory = new MemoryStream()) {
                encoder.Save(memory);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memory.ToArray());
                bitmapImage.EndInit();
                rgbimage.Source = bitmapImage;
            }
        }
        public void SetSize(int w, int h) {
            width = w;
            height = h;
            RenderFrame();
        }
        public void OpenFile(string reelPath) {
            if (reelPath == "") return;
            FileStream fs = new FileStream(reelPath, FileMode.Open);
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream()) {
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);
                bytearr = ms.ToArray();
            }
            fs.Close();
            slider.Value = 0;
            RenderFrame();
            sizeMenu.IsEnabled = true;
            colorMenu.IsEnabled = true;
            SetSize(width, height);
        }
        public void UnChecker(object sender, MenuItem menuWrapper) {
            foreach (Control item in menuWrapper.Items) if (item is MenuItem) ((MenuItem)item).IsChecked = false;
            (sender as MenuItem).IsChecked = true;
        }
        public MainWindow() {
            InitializeComponent();
            player.Tick += delegate (System.Object o, System.EventArgs e) {
                slider.Value = ((int)slider.Value == maxFrame ? 0 : (int)slider.Value + 1);
                RenderFrame();
            };
            player.Interval = new TimeSpan(0, 0, 0, 0, 1);
        }
        private void Open_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "YUV Files (*.yuv)|*.yuv";
            if (ofd.ShowDialog() == true) OpenFile(ofd.FileName);
        }
        private void Save_Click(object sender, RoutedEventArgs e) {
            if (bitmap == null) return;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Bitmap files (*.bmp)|*.bmp";
            if (sfd.ShowDialog() == true) {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var fileStream = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create)) {
                    encoder.Save(fileStream);
                }
            }
        }
        private void Exit_Click(object sender, RoutedEventArgs e) { System.Windows.Application.Current.Shutdown(); }
        private void ChangeSize_Click(object sender, RoutedEventArgs e) {
            var itemContent = (sender as MenuItem).Header.ToString();
            string resolution = itemContent.Substring(itemContent.IndexOf("(") + 1, itemContent.LastIndexOf(")") - itemContent.IndexOf("(") - 1);
            var resArr = resolution.Split('x').Select(Int32.Parse).ToList();
            UnChecker(sender, sizeMenu);
            SetSize(resArr[0], resArr[1]);
        }
        private void OpenSizeDialog_Click(object sender, RoutedEventArgs e) {
            SizeChanger sizeChanger = new SizeChanger(width, height);
            sizeChanger.Owner = this;
            sizeChanger.ShowDialog();
            if (sizeChanger.width != width || sizeChanger.height != height) UnChecker(sender, sizeMenu);
            SetSize(sizeChanger.width, sizeChanger.height);
        }
        private void ChangeColor_Click(object sender, RoutedEventArgs e) {
            UnChecker(sender, colorMenu);
            type = (sender as MenuItem).TabIndex;
            RenderFrame();
        }
        private void PlayStop_Click(object sender, RoutedEventArgs e) { if (player.IsEnabled) player.Stop(); else player.Start(); }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { RenderFrame(); }
    }
}