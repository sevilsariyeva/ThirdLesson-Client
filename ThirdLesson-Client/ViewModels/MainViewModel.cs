﻿using System;
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
                    image.Quality = 50; 

                    while (image.ToByteArray().Length > targetSize)
                    {
                        image.Quality -= 5; 

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
