using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace BluetoothKeepAlive.Tools
{
    internal static class IconBuilder
    {
        private static readonly int[] IconSizes = new int[] { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };

        // Polygon silhouettes traced directly from the supplied 1024 x 1024 logo.
        private static readonly string[] PolygonData = new string[]
        {
            "215,220 213,221 210,226 204,233 190,254 172,285 160,309 151,330 143,351 136,373 135,380 132,387 123,426 118,460 116,480 115,498 115,533 117,563 119,580 122,599 126,618 132,643 139,666 146,686 158,715 174,747 183,763 198,786 212,805 214,806 216,804 217,804 222,798 255,766 247,756 237,741 224,719 207,684 199,664 191,640 183,610 181,595 179,590 176,569 173,528 173,498 174,479 176,459 180,433 183,419 191,388 203,353 205,351 207,344 211,337 213,330 218,322 218,320 221,314 240,281 253,263 253,262 255,260 250,254",
            "300,304 298,305 298,306 296,308 295,311 291,316 288,321 274,348 268,361 259,384 253,403 248,421 241,457 238,486 237,513 238,543 240,565 248,607 251,614 253,625 260,646 270,671 287,704 298,720 298,721 300,722 340,682 340,680 332,667 318,638 313,625 307,606 305,594 302,586 299,571 296,546 295,528 295,503 297,473 300,452 306,425 312,405 317,391 323,377 340,345 340,343 305,308",
            "379,395 375,396 370,399 366,403 364,406 361,413 361,612 365,621 370,626 375,629 378,630 389,630 396,627 416,607 418,603 419,599 418,548 419,425 417,419 396,398 392,396 388,395",
            "492,200 482,201 483,825 543,825 543,201",
            "637,395 633,396 628,399 609,418 607,421 606,425 606,601 608,606 629,627 635,630 647,630 650,629 655,626 660,621 664,612 664,413 661,406 659,403 655,399 652,397 645,395",
            "726,303 702,326 694,335 685,343 685,345 692,356 705,383 716,413 721,431 726,455 728,469 730,498 730,531 729,547 726,572 721,596 718,607 710,631 704,646 692,670 688,677 686,679 685,682 726,722 732,714 741,699 755,671 763,652 770,632 775,615 782,584 787,545 788,527 788,502 786,472 781,438 776,415 770,395 768,392 767,389 766,383 764,377 761,372 760,367 754,353 741,327 731,310 728,307",
            "812,220 801,229 771,259 771,261 774,266 782,276 793,294 802,310 812,330 816,339 818,346 820,348 824,358 833,384 834,391 837,398 839,406 839,410 842,418 845,433 850,469 852,498 852,531 851,549 847,584 842,611 833,644 822,675 818,682 816,689 804,714 787,743 781,752 771,765 771,767 793,788 803,799 813,805 833,777 851,747 865,719 874,698 881,680 884,671 886,662 888,659 897,626 904,591 909,545 910,508 909,485 906,453 901,421 896,398 891,379 878,339 864,306 852,282 835,253"
        };

        private static int Main(string[] args)
        {
            string outputPath = args.Length > 0 ? args[0] : "app.ico";
            PointF[][] polygons = ParsePolygons();
            byte[][] images = new byte[IconSizes.Length][];

            for (int i = 0; i < IconSizes.Length; i++)
            {
                images[i] = RenderPng(IconSizes[i], polygons);
            }

            using (FileStream stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)IconSizes.Length);

                int offset = 6 + (16 * IconSizes.Length);
                for (int i = 0; i < IconSizes.Length; i++)
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

            Console.WriteLine("Generated " + outputPath + " from the supplied keep-alive mark.");
            return 0;
        }

        private static PointF[][] ParsePolygons()
        {
            PointF[][] result = new PointF[PolygonData.Length][];

            for (int i = 0; i < PolygonData.Length; i++)
            {
                string[] pairs = PolygonData[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<PointF> points = new List<PointF>(pairs.Length);

                for (int j = 0; j < pairs.Length; j++)
                {
                    string[] values = pairs[j].Split(',');
                    points.Add(new PointF(
                        Single.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture),
                        Single.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture)));
                }

                result[i] = points.ToArray();
            }

            return result;
        }

        private static byte[] RenderPng(int size, PointF[][] polygons)
        {
            using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                GraphicsState state = graphics.Save();
                float scale = size / 1024f;
                graphics.ScaleTransform(scale, scale);

                for (int i = 0; i < polygons.Length; i++)
                {
                    graphics.FillPolygon(brush, polygons[i], FillMode.Winding);
                }

                graphics.Restore(state);

                using (MemoryStream memory = new MemoryStream())
                {
                    bitmap.Save(memory, ImageFormat.Png);
                    return memory.ToArray();
                }
            }
        }
    }
}
