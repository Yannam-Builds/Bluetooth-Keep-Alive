using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace BluetoothKeepAlive.Tools
{
    internal static class IconBuilder
    {
        private static readonly int[] IconSizes = new int[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

        private static int Main(string[] args)
        {
            string outputPath = args.Length > 0 ? args[0] : "app.ico";
            string directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            byte[][] images = new byte[IconSizes.Length][];
            for (int i = 0; i < IconSizes.Length; i++)
            {
                images[i] = RenderPng(IconSizes[i]);
            }

            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0); // reserved
                writer.Write((ushort)1); // icon type
                writer.Write((ushort)IconSizes.Length);

                int offset = 6 + (16 * IconSizes.Length);
                for (int i = 0; i < IconSizes.Length; i++)
                {
                    int size = IconSizes[i];
                    byte[] image = images[i];
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)0); // color count
                    writer.Write((byte)0); // reserved
                    writer.Write((ushort)1); // color planes
                    writer.Write((ushort)32); // bits per pixel
                    writer.Write((uint)image.Length);
                    writer.Write((uint)offset);
                    offset += image.Length;
                }

                for (int i = 0; i < images.Length; i++)
                {
                    writer.Write(images[i]);
                }
            }

            Console.WriteLine("Generated " + outputPath + " with transparent white icon layers.");
            return 0;
        }

        private static byte[] RenderPng(int size)
        {
            using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                DrawLogo(graphics, size);

                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    return memory.ToArray();
                }
            }
        }

        private static void DrawLogo(Graphics graphics, int size)
        {
            GraphicsState state = graphics.Save();

            float scale = (size / 512f) * 1.18f; // larger mark for taskbar/EXE readability
            graphics.TranslateTransform(size / 2f, size / 2f);
            graphics.ScaleTransform(scale, scale);
            graphics.TranslateTransform(-256f, -256f);

            using (SolidBrush whiteBrush = new SolidBrush(Color.White))
            using (Pen wavePen = new Pen(Color.White, 25f))
            using (Pen outerWavePen = new Pen(Color.White, 23f))
            using (Pen bluetoothPen = new Pen(Color.White, 22f))
            {
                wavePen.StartCap = LineCap.Round;
                wavePen.EndCap = LineCap.Round;
                outerWavePen.StartCap = LineCap.Round;
                outerWavePen.EndCap = LineCap.Round;
                bluetoothPen.StartCap = LineCap.Round;
                bluetoothPen.EndCap = LineCap.Round;
                bluetoothPen.LineJoin = LineJoin.Round;

                PointF[] speaker = new PointF[]
                {
                    new PointF(64f, 195f),
                    new PointF(143f, 195f),
                    new PointF(237f, 123f),
                    new PointF(237f, 389f),
                    new PointF(143f, 317f),
                    new PointF(64f, 317f)
                };
                graphics.FillPolygon(whiteBrush, speaker);

                using (GraphicsPath innerWave = new GraphicsPath())
                using (GraphicsPath outerWave = new GraphicsPath())
                using (GraphicsPath bluetooth = new GraphicsPath())
                {
                    innerWave.AddBezier(243f, 184f, 279f, 212f, 279f, 300f, 243f, 328f);
                    outerWave.AddBezier(296f, 141f, 360f, 191f, 360f, 321f, 296f, 371f);

                    bluetooth.StartFigure();
                    bluetooth.AddLine(369f, 145f, 369f, 367f);
                    bluetooth.StartFigure();
                    bluetooth.AddLine(369f, 145f, 443f, 201f);
                    bluetooth.AddLine(443f, 201f, 369f, 256f);
                    bluetooth.AddLine(369f, 256f, 443f, 312f);
                    bluetooth.AddLine(443f, 312f, 369f, 367f);
                    bluetooth.StartFigure();
                    bluetooth.AddLine(369f, 256f, 310f, 198f);
                    bluetooth.StartFigure();
                    bluetooth.AddLine(369f, 256f, 310f, 314f);

                    graphics.DrawPath(wavePen, innerWave);
                    graphics.DrawPath(outerWavePen, outerWave);
                    graphics.DrawPath(bluetoothPen, bluetooth);
                }
            }

            graphics.Restore(state);
        }
    }
}
