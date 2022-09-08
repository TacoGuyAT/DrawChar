using DrawChar._3D;
using DrawChar.Core;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
///  VanillaExtract is an ASCII adventure game, that focuses on the story with JRPG-ish and text adventure gameplay sections
namespace VanillaExtract {
    public class Game {
#if DEBUG
        static void Main(string[] debugArgs) {
            string[] args = new string[] { @"C:\fbx test\generic test models\ravenholm.fbx" };
#else
        static void Main(string[] args) {
#endif
            ASCIIRenderer.AddCharSet(
                // Standart ASCII gradient
                "$@B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'. ",
                // Different one
                "@MBHENR#KWXDFPQASUZbdehx*8Gm&04LOVYkpq5Tagns69owz$CIu23Jcfry%1v7l+it[]{}?j|()=~!-/<>\\\"^_';,:`. ",
                // Full ASCII charset
                " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ÇüéâäàåçêëèïîìÄÅÉæÆôöòûùÿÖÜø£Ø×ƒáíóúñÑªº¿®¬½¼¡«»░▒▓│┤ÁÂÀ©╣║╗╝¢¥┐└┴┬├─┼ãÃ╚╔╩╦╠═╬¤ðÐÊËÈıÍÎÏ┘┌█▄¦Ì▀ÓßÔÒõÕµþÞÚÛÙýÝ¯´­±‗¾¶§÷¸°¨·¹³²■"
            );

#if !DEBUG
            if(args.Length < 1) {
                Console.WriteLine("Drag and drop 3D model on the file. Press any key to continue.");
                Console.ReadKey();
                Environment.Exit(-1);
            }
#endif
            // use console width and height
            // Console.WindowWidth = 238;
            // Console.WindowHeight = 64;
            bool newlineMode = false;
            ASCIIRenderer.Init(Console.WindowWidth - (newlineMode == true ? 1 : 0), Console.WindowHeight);

            ASCIIRenderer.FlushBuffer(true);
            
            byte charset = 0;
            int sync = 60;
            List<double> fts = new List<double>();
            List<double> sts = new List<double>();

            var _3D = new _3DRenderer(new GameWindowSettings { RenderFrequency = sync }, new NativeWindowSettings { Size = new Vector2i(ASCIIRenderer.Width, ASCIIRenderer.Height), StartVisible = true });
            _3D.LoadModel(String.Join(' ', args));

            new Thread(() => { 
                while(true) { // TODO: dt is required for logic stuff, using 3d and locked@60 rn
                    if(fts.Count > 100) fts.RemoveRange(100, fts.Count - 100);
                    fts.Insert(0, ASCIIRenderer.FrameTime.TotalMilliseconds);
                    if(sts.Count > 100) sts.RemoveRange(100, sts.Count - 100);
                    sts.Insert(0, ASCIIRenderer.SyncTime.TotalMilliseconds);
                    ASCIIRenderer.ResetDelta();
                    //*
                    int i = 0;
                    _3D.Buffer.ForEach(x => {
                        ASCIIRenderer.PlaceCharUnsafe(ASCIIRenderer.AToChar(_3D.Buffer[i], charset), i, 0);
                        i++;
                    });

                    //*/
                    //for(int i = 0; i < ASCIIRenderer.Width * ASCIIRenderer.Height; i++)
                    //ASCIIRenderer.Buffer.Add(ASCIIRenderer.AToChar(image[i], charset));
                    //ASCIIRenderer.Buffer.Add(ASCIIRenderer.AToChar(ASCIIRenderer.Counter, 1));
                    //Random r = new Random();
                    //for(int i = 0; i < 450; i++) {
                    //var ch = ASCIIRenderer.CharSet[charset][r.Next(ASCIIRenderer.CharSet[charset].Length)];
                    //for(int i = 0; i < ASCIIRenderer.Counter; i++)
                    //ASCIIRenderer.SetChar(ch, i, 1);
                    //    //ASCIIRenderer.SetChar(ch, r.Next(ASCIIRenderer.Width), r.Next(ASCIIRenderer.Height));
                    //}
                    DrawDebug(fts, sts, sync, 70, 15);
                    ASCIIRenderer.FlushBuffer(true, newlineMode);
                    ASCIIRenderer.PerfectSync(sync);
                }
            }).Start();
            var _3DCamera = new Camera(Vector3.Zero, ASCIIRenderer.Width / ASCIIRenderer.Height);
            _3D.Run();
        }
        public static void DrawDebug(List<double> fts, List<double> sts, double framerate, byte length = 50, byte height = 20) {
            double afts = fts.Sum() / fts.Count;
            double asts = sts.Sum() / sts.Count;
            ASCIIRenderer.FillCharUnsafe(' ', 0, ASCIIRenderer.Height - height - 2, length + 1, height + 2);
            ASCIIRenderer.SetStringUnsafe(new string(' ', ASCIIRenderer.Width), 0, 0);
            ASCIIRenderer.SetStringUnsafe(new string('─', ASCIIRenderer.Width), 0, 1);
            ASCIIRenderer.SetStringUnsafe($"Framerate: {(1000 / (afts + asts)).ToString("0.0")}FPS | Frametime: {afts.ToString("0.00")}ms | Counter: {ASCIIRenderer.Counter}", 0, 0);
            string border_horizontal = new string('─', length) + '┐';
            ASCIIRenderer.SetStringUnsafe(border_horizontal, 0, ASCIIRenderer.Height - height);
            for(int i = 0; i < height - 1; i++)
                ASCIIRenderer.PlaceCharUnsafe('│', length, ASCIIRenderer.Height - i - 1);
            int x = 0;
            foreach(double dt in fts) {
                var dtF = dt / (1000 / framerate);
                var dtFC = Math.Min(dtF, 1);
                for(int i = 0; i < (int)(dtFC * (height - 1)); i++) {
                    if(dtF > 1)
                        ASCIIRenderer.PlaceCharUnsafe('█', x, ASCIIRenderer.Height - i - 1);
                    else
                        ASCIIRenderer.PlaceCharUnsafe('#', x, ASCIIRenderer.Height - i - 1);
                }
                x++;
                if(x > length - 1) break;
            }
            ASCIIRenderer.SetStringUnsafe($"Min: {fts.Min().ToString("0.00")}", 0, ASCIIRenderer.Height - height - 1);
            ASCIIRenderer.SetStringUnsafe($"Max: {fts.Max().ToString("0.00")}", 0, ASCIIRenderer.Height - height - 2);
        }
    }
}