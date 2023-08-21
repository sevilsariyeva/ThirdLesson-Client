using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Image = System.Windows.Controls.Image;
using System.IO;
using Point = System.Windows.Point;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;
using ThirdLesson_Client.Commands;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Encoder = System.Drawing.Imaging.Encoder;
using System.IO.Compression;
using ImageMagick;

namespace ThirdLesson_Client.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private Image currentImage;

        public Image CurrentImage
        {
            get { return currentImage; }
            set { currentImage = value; OnPropertyChanged(); }
        }
        //public byte[] ImageToByteArray(BitmapSource image)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        var encoder = new PngBitmapEncoder(); 
        //        encoder.Frames.Add(BitmapFrame.Create(image));
        //        encoder.Save(ms);
        //        return ms.ToArray();
        //    }
        //}
        static byte[] ImageToByteArray(Bitmap image)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                return memoryStream.ToArray();
            }
        }
        private Bitmap TakeScreen()
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
            Bitmap screenshot = new Bitmap(screenWidth, screenHeight);

            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));
            }

            string newFolder = "ScreenShots";
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                newFolder
            );

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return null;
            }

            string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}.png";
            string fullPath = Path.Combine(path, fileName);

            screenshot.Save(fullPath, ImageFormat.Png);
            return screenshot;
        }
        //public BitmapImage CompressImage(Bitmap sourceBitmap, long targetFileSizeInBytes)
        //{
        //    // Create a parameterized encoder for quality setting
        //    EncoderParameters encoderParameters = new EncoderParameters(1);
        //    EncoderParameter encoderParameter = new EncoderParameter(Encoder.Quality, 50L); // Adjust quality as needed
        //    encoderParameters.Param[0] = encoderParameter;

        //    ImageCodecInfo jpegCodecInfo = GetEncoderInfo(ImageFormat.Jpeg);

        //    MemoryStream compressedStream = new MemoryStream();
        //    sourceBitmap.Save(compressedStream, jpegCodecInfo, encoderParameters);

        //    BitmapImage bitmapImage = new BitmapImage();
        //    bitmapImage.BeginInit();
        //    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        //    bitmapImage.StreamSource = new MemoryStream(compressedStream.ToArray());
        //    bitmapImage.EndInit();
        //    bitmapImage.Freeze(); // Freeze the image to make it accessible from other threads

        //    return bitmapImage;
        //}

        private ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        private byte[] CompressImage(byte[] imageBytes, int targetSize)
        {
            using (MemoryStream memoryStream = new MemoryStream(imageBytes))
            {
                using (var image = new MagickImage(memoryStream))
                {
                    image.Quality = 50; // Adjust the quality level as needed

                    while (image.ToByteArray().Length > targetSize)
                    {
                        image.Quality -= 5; // Reduce quality iteratively

                        if (image.Quality <= 0)
                        {
                            break;
                        }
                    }

                    return image.ToByteArray();
                }
            }
        }
        private Bitmap ByteArrayToImage(byte[] byteArray)
        {
            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            {
                return new Bitmap(memoryStream);
            }
        }

        // Resize image
        private Bitmap ResizeImage(Bitmap image, double scaleFactor)
        {
            int newWidth = (int)(image.Width * scaleFactor);
            int newHeight = (int)(image.Height * scaleFactor);

            Bitmap resizedImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                graphics.DrawImage(image, new Rectangle(0, 0, newWidth, newHeight));
            }

            return resizedImage;
        }
        public RelayCommand ConnectClickCommand { get; set; }
        public MainViewModel()
        {
            ConnectClickCommand = new RelayCommand(async (obj) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                var ip = IPAddress.Parse("192.168.0.106");
                var port = 27001;

                EndPoint ep = new IPEndPoint(ip, port);
                while (true)
                {
                    await Task.Run(() =>
                    {
                        var img = TakeScreen();
                        try
                        {
                            var bytes = ImageToByteArray(img);
                            var compressed = CompressImage(bytes,50000);

                            socket.SendTo(compressed, ep);
                          
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        Task.Delay(20);
                    });
                }

            });

        }
    }
}
