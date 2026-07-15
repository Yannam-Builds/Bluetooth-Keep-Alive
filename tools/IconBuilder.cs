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
        private const float MarkWidthRatio = 0.78f; // ~20% larger than the source mark inside its original canvas.

        private static int Main(string[] args)
        {
            string sourcePath = args.Length > 0 ? args[0] : Path.Combine("assets", "logo.png");
            string outputPath = args.Length > 1 ? args[1] : "app.ico";

            if (!File.Exists(sourcePath))
            {
                Console.Error.WriteLine("Logo source not found: " + sourcePath);
                return 1;
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!String.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (Bitmap source = new Bitmap(sourcePath))
            {
                Rectangle visibleBounds = FindVisibleBounds(source);
                byte[][] images = new byte[IconSizes.Length][];

                for (int i = 0; i < IconSizes.Length; i++)
                {
                    images[i] = RenderPng(source, visibleBounds, IconSizes[i]);
                }

                WriteIcon(outputPath, images);
            }

            Console.WriteLine("Generated " + outputPath + " from " + sourcePath + ".");
            return 0;
        }

        private static Rectangle FindVisibleBounds(Bitmap source)
        {
            int left = source.Width;
            int top = source.Height;
            int right = -1;
            int bottom = -1;

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    if (source.GetPixel(x, y).A <= 8)
                    {
                        continue;
                    }

                    if (x < left) left = x;
                    if (x > right) right = x;
                    if (y < top) top = y;
                    if (y > bottom) bottom = y;
                }
            }

            if (right < left || bottom < top)
            {
                throw new InvalidDataException("The logo PNG contains no visible pixels.");
            }

            return Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        }

        private static byte[] RenderPng(Bitmap source, Rectangle sourceBounds, int size)
        {
            using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                float targetWidth = size * MarkWidthRatio;
                float scale = targetWidth / sourceBounds.Width;
                float targetHeight = sourceBounds.Height * scale;

                if (targetHeight > size * MarkWidthRatio)
                {
                    targetHeight = size * MarkWidthRatio;
                    scale = targetHeight / sourceBounds.Height;
                    targetWidth = sourceBounds.Width * scale;
                }

                RectangleF destination = new RectangleF(
                    (size - targetWidth) / 2f,
                    (size - targetHeight) / 2f,
                    targetWidth,
                    targetHeight);

                graphics.DrawImage(
                    source,
                    destination,
                    sourceBounds.X,
                    sourceBounds.Y,
                    sourceBounds.Width,
                    sourceBounds.Height,
                    GraphicsUnit.Pixel);

                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    return memory.ToArray();
                }
            }
        }

        private static void WriteIcon(string outputPath, byte[][] images)
        {
            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)IconSizes.Length);

                int offset = 6 + (16 * IconSizes.Length);
                for (int i = 0; i < images.Length; i++)
                {
                    int size = IconSizes[i];
                    byte[] image = images[i];

                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write((uint)image.Length);
                    writer.Write((uint)offset);

                    offset += image.Length;
                }

                for (int i = 0; i < images.Length; i++)
                {
                    writer.Write(images[i]);
                }
            }
        }
    }
}
