using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VanillaExtract {
    public static class ASCIIRenderer {

        public static List<string> CharSet = new List<string> {
            // Standart ASCII gradient
            "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'. ",
            // Different one
            "@MBHENR#KWXDFPQASUZbdehx*8Gm&04LOVYkpq5Tagns69owz$CIu23Jcfry%1v7l+it[]{}?j|()=~!-/<>\\\"^_';,:`. ",
            // Full ASCII charset
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜø£Ø×ƒáíóúñÑªº¿®¬½¼¡«»░▒▓│┤ÁÂÀ©╣║╗╝¢¥┐└┴┬├─┼ãÃ╚╔╩╦╠═╬¤ðÐÊËÈıÍÎÏ┘┌█▄¦Ì▀ÓßÔÒõÕµþÞÚÛÙýÝ¯´­±‗¾¶§÷¸°¨·¹³²■"
        };

        public static int Width;
        public static int Height;
        public static byte Counter;
        public static TimeSpan FrameTime = TimeSpan.FromMilliseconds(0.01);
        public static TimeSpan SyncTime = TimeSpan.FromMilliseconds(0);
        public static double Framerate { get { return (1000 / (FrameTime.Milliseconds + 0.00001)); } }
        public static DateTime UTCTime;
        public static List<char> Buffer = new List<char>();

        public static char AToChar(byte a, byte charset = 0) {
            int f = (int)(((a / 255.0f) * -1 + 1) * (CharSet[charset].Length - 1));
            return CharSet[charset][f];
        }
        public static void SetChar(char c, int x, int y) {
            Buffer.RemoveAt(x + y * Width);
            Buffer.Insert(x + y * Width, c);
        }
        public static void SetString(string s, int x, int y) {
            for(int i = 0; i < s.Length; i++)
                SetChar(s[i], x + i, y);
        }
        public static void FlushBuffer(bool fill = false, bool newlineMode = false) {
            Counter = (byte)(Counter == 255 ? 0 : Counter+1);
            if(newlineMode)
                Console.Write(String.Join('\n', Buffer.Chunk(Width).Select(x => String.Concat(x))));
            else
                Console.Write(String.Concat(Buffer));
            Console.SetCursorPosition(0, 0);
            ClearBuffer(fill);
            FrameTime = DateTime.UtcNow - UTCTime;
        }
        public static void ResetDelta() {
            UTCTime = DateTime.UtcNow;
        }
        public static void Sync(double fps = 60) {
            SyncTime = TimeSpan.FromMilliseconds(Math.Max(1000.0 / fps - FrameTime.TotalMilliseconds, 0));
            if(SyncTime.Ticks != 0)
                Thread.Sleep(SyncTime);
        }
        public static void Init(int width, int height) {
            Width = width;
            Height = height;
            Console.CursorVisible = false;
        }
        public static void ClearBuffer(bool fill = false) {
            Buffer.Clear();
            if(fill)
                for(int i = 0; i < Width * Height; i++)
                    Buffer.Add(' ');
        }
        public static List<byte> CreateImage(Bitmap fbmp) {
            //Bitmap.FromFile(String.Join(" ", args)
            Bitmap bmp = new Bitmap(fbmp, Width, Height);

            //LinearGradientBrush lgb = new LinearGradientBrush(new Point(0, 0), new Point(ASCIIRenderer.Width, ASCIIRenderer.Height), Color.Black, Color.Red);
            //g.FillRectangle(lgb, 0, 0, ASCIIRenderer.Width, ASCIIRenderer.Height);

            List<byte> image = new List<byte>();
            for(int y = 0; y < ASCIIRenderer.Height; y++)
                for(int x = 0; x < ASCIIRenderer.Width; x++) {
                    var pixel = bmp.GetPixel(x, y);
                    var alpha = (pixel.A / 255);
                    //alpha = 1;
                    image.Add((byte)((pixel.R + pixel.G + pixel.B) / 3 * alpha));
                }
            return image;
        }
    }
}
