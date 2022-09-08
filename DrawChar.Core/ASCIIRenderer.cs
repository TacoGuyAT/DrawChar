using DrawChar.Core.Drawables;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Numerics;

namespace DrawChar.Core {
    public static class ASCIIRenderer {
        public static List<Drawable> Drawables = new();

        public static List<string> CharSets = new List<string>();

        public static Vector2 Size;
        public static int Width {
            get {
                return (int)Size.X;
            }
            set {
                Size.X = value;
            }
        }
        public static int Height {
            get {
                return (int)Size.Y;
            }
            set {
                Size.Y = value;
            }
        }
        public static byte Counter;
        public static TimeSpan FrameTime = TimeSpan.FromMilliseconds(0.01);
        public static TimeSpan SyncTime = TimeSpan.FromMilliseconds(0);
        public static double Framerate { get { return (1000 / (FrameTime.Milliseconds + 0.00001)); } }
        public static DateTime UTCTime;
        public static List<char> Buffer = new List<char>();

        public static void AddCharSet(params string[] charsets) {
            foreach(string charset in charsets)
                CharSets.Add(charset);
        }

        public static char AToChar(byte a, byte charset = 0) {
            int f = (int)(((a / 255.0f) * -1 + 1) * (CharSets[charset].Length - 1));
            return CharSets[charset][f];
        }
        public static void PlaceChar(char c, int x, int y) {
            // We can do PlaceCharUnsafe() call, but why?
            if(x + 1 < Size.X && y + 1 < Size.Y)
                Buffer[x + y * (int)Size.X] = c;
        }
        /// <summary>
        /// This method will overflow
        /// </summary>
        public static void PlaceCharUnsafe(char c, int x, int y) {
            Buffer[x + y * (int)Size.X] = c;
        }
        public static void SetString(string s, int x, int y) {
            for(int i = 0; i < s.Length; i++)
                PlaceChar(s[i], x + i, y);
        }
        /// <summary>
        /// This method will overflow
        /// </summary>
        public static void SetStringUnsafe(string s, int x, int y) {
            for(int i = 0; i < s.Length; i++)
                PlaceCharUnsafe(s[i], x + i, y);
        }
        /// <summary>
        /// This method will overflow
        /// </summary>
        public static void FillCharUnsafe(char c, int x, int y, int w, int h) {
            for(int i = 0; i < w * h; i++)
                PlaceCharUnsafe(c, x + i % w, y + i / w);
        }
        public static void PlaceBuffer(List<char> buffer, int x, int y, int w, int h) {
            int fW = w - (x + w - (int)Size.X);
            int fH = h - (y + h - (int)Size.Y);
            for(int i = 0; i < fW * fH; i++) {
                PlaceCharUnsafe(buffer[i], x + i % fW, y + i / fW);
            }
        }
        public static void PlaceBufferUnsafe(List<char> buffer, int x, int y, int w, int h) {
            for(int i = 0; i < w * h; i++)
                PlaceCharUnsafe(buffer[i], x + i % w, y + i / w);
        }
        public static void FlushBuffer(bool prefilledBuffer = false, bool newlineMode = false) {
            Counter = (byte)(Counter == 255 ? 0 : Counter+1);
            if(newlineMode)
                Console.Write(String.Join('\n', Buffer.Chunk((int)Size.X).Select(x => String.Concat(x))));
            else
                Console.Write(String.Concat(Buffer));
            Console.SetCursorPosition(0, 0);
            ClearBuffer(prefilledBuffer);
            FrameTime = DateTime.UtcNow - UTCTime;
        }
        public static void ResetDelta() {
            UTCTime = DateTime.UtcNow;
        }
        /// <summary>
        /// Syncing can't go above 60fps, microsoft's problem
        /// </summary>
        public static void Sync(double fps = 60) {
            SyncTime = TimeSpan.FromMilliseconds(Math.Max(1000.0 / fps - FrameTime.TotalMilliseconds, 0));
            if(SyncTime.Ticks != 0)
                Thread.Sleep(SyncTime);
        }
        /// <summary>
        /// Will make your CPU die, but has perfect sync time
        /// </summary>
        public static void PerfectSync(double fps = 60) {
            SyncTime = TimeSpan.FromMilliseconds(Math.Max(1000.0 / fps - FrameTime.TotalMilliseconds, 0));
            var sw = new Stopwatch();
            sw.Start();
            while(sw.Elapsed.TotalMilliseconds < SyncTime.TotalMilliseconds) ;
        }
        public static void Init(int width, int height) {
            Size = new Vector2(width, height);
            Console.CursorVisible = false;
        }
        public static void ClearBuffer(bool fill = false) {
            Buffer.Clear();
            if(fill)
                for(int i = 0; i < Size.X * Size.Y; i++)
                    Buffer.Add(' ');
        }
        public static void Draw() {
            Drawables.ForEach(x => x.Draw());
        }
        public static void DrawUnsafe() {
            Drawables.ForEach(x => x.DrawUnsafe());
        }
        public static List<byte> ToA(Image<Rgba32> fbmp, int? width = null, int? height = null) {
            //Bitmap.FromFile(String.Join(" ", args)
            int twidth = width ?? fbmp.Width;
            int theight = height ?? fbmp.Height;
            fbmp.Mutate(x => x.Resize(twidth, theight));

            //LinearGradientBrush lgb = new LinearGradientBrush(new Point(0, 0), new Point(ASCIIRenderer.Width, ASCIIRenderer.Height), Color.Black, Color.Red);
            //g.FillRectangle(lgb, 0, 0, ASCIIRenderer.Width, ASCIIRenderer.Height);

            List<byte> image = new List<byte>();


            fbmp.ProcessPixelRows(accessor =>
            {

                for(int y = 0; y < accessor.Height; y++) {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for(int x = 0; x < pixelRow.Length; x++) {
                        ref Rgba32 pixel = ref pixelRow[x];
                        image.Add((byte)((pixel.R + pixel.G + pixel.B) / 3.0f * (pixel.A / 255.0f)));
                    }
                }
            });

            return image;
        }
    }
}
