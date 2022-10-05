using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace image_processor
{
    public static class Extensions
    {
        public static byte ToByte(this float val)
        {
            if (val < 0) return (byte)0;
            if (val > 255) return (byte)255;
            return (byte)Math.Round(val);
        }

        public delegate void PointOp(ref byte r, ref byte g, ref byte b, ref byte a);
        public static void ApplyPointOp(this Bitmap bm, PointOp op)
        {
            int width = bm.Width;
            int height = bm.Height;
            byte r, g, b, a;
            Bitmap32 bitmap = new Bitmap32(bm);
            bitmap.LockBitmap();
            for (int row=0; row < height; row++)
            {
                for (int col=0; col < width; col++)
                {
                    bitmap.GetPixel(col, row, out r, out g, out b, out a);
                    op(ref r, ref g, ref b, ref a);
                    bitmap.SetPixel(col, row, r, g, b, a);
                }
            }
            bitmap.UnlockBitmap();
        }
        public static void RGBtoHLS(byte r, byte g, byte b, out double h, out double l, out double s)
        {
            double dR = ((double)r) / 255.0;
            double dG = ((double)g) / 255.0;
            double dB = ((double)b) / 255.0;

            byte max = Math.Max(r, g);
            max = Math.Max(max,b);
            byte min = Math.Min(r, g);
            min = Math.Min(min, b);

            double dMax = Math.Max(dR, dG);
            dMax = Math.Max(dMax, dB);

            double dMin = Math.Min(dR, dG);
            dMin = Math.Min(dMin, dB);

            h = 0;
            if (max == min)
            {
                h = 0;
            }
            else if (max == r)
            {
                h = 60.0 * (0 + (dG - dB) / (dMax - dMin));
            }
            else if (max == g)
            {
                h = 60.0 * (2 + (dB - dR) / (dMax - dMin));
            }
            else if (max == b)
            {
                h = 60.0 * (4 + (dR - dG) / (dMax - dMin));
            }
            if (h < 0)
            {
                h = h + 360;
            }

            s = 0;
            if (max == 0 || min == 1)
            {
                s = 0;
            }
            else
            {
                s = (dMax - dMin) / (1 - Math.Abs(dMax+dMin-1));
            }

            l = (dMax + dMin) / 2.0;

            while (h < 0) h = h + 360;
            while (h > 360) h = h - 360;
            if (s < 0) s = 0;
            if (s > 1) s = 1;
            if (l < 0) l = 0;
            if (l > 1) l = 1;
        }
        public static void HLStoRGB(double h, double l, double s, out byte r, out byte g, out byte b)
        {
            double dR = 0;
            double dG = 0;
            double dB = 0;

            double dC = (1-Math.Abs(2.0*l-1)) * s;
            double h1 = h / 60.0;
            double dX = dC * (1 - Math.Abs(h1 % 2 - 1));
            double dM = l - dC / 2.0;

            if (0 <= h1 && h1 < 1)
            {
                dR = dC;
                dG = dX;
                dB = 0;
            }
            else if (1 <= h1 && h1 < 2)
            {
                dR = dX;
                dG = dC;
                dB = 0;
            }
            else if (2 <= h1 && h1 < 3)
            {
                dR = 0;
                dG = dC;
                dB = dX;
            }
            else if (3 <= h1 && h1 < 4)
            {
                dR = 0;
                dG = dX;
                dB = dC;
            }
            else if (4 <= h1 && h1 < 5)
            {
                dR = dX;
                dG = 0;
                dB = dC;
            }
            else if (5 <= h1 && h1 < 6)
            {
                dR = dC;
                dG = 0;
                dB = dX;
            }

            dR = (dR + dM)*255.0;
            dG = (dG + dM)*255.0;
            dB = (dB + dM)*255.0;

            r = ((float)dR).ToByte();
            g = ((float)dG).ToByte();
            b = ((float)dB).ToByte();
        }

        // Adjust the value closer to 0 or 1.
        // Factor should be between 0 and 2.
        // 0 <= factor < 1 adjusts towards 0.
        // 1 < factor <= 2 adjusts towards 1.
        public static double AdjustValue(double value, double factor)
        {
            if (0 <= factor && factor < 1)
            {
                value = value * factor;
            }
            else if (1 < factor && factor <= 2)
            {
                value = value + (factor - 1) * (1 - value);
            }

            return value;
        }
        
        // Save the file with the appropriate format.
        public static void SaveImage(this Image image, string filename)
        {
            string extension = Path.GetExtension(filename);
            switch (extension.ToLower())
            {
                case ".bmp":
                    image.Save(filename, ImageFormat.Bmp);
                    break;
                case ".exif":
                    image.Save(filename, ImageFormat.Exif);
                    break;
                case ".gif":
                    image.Save(filename, ImageFormat.Gif);
                    break;
                case ".jpg":
                case ".jpeg":
                    image.Save(filename, ImageFormat.Jpeg);
                    break;
                case ".png":
                    image.Save(filename, ImageFormat.Png);
                    break;
                case ".tif":
                case ".tiff":
                    image.Save(filename, ImageFormat.Tiff);
                    break;
                default:
                    throw new NotSupportedException(
                        "Unknown file extension " + extension);
            }
        }
        // Rotate an image around its center.
        public static Bitmap RotateAtCenter(this Bitmap bm,float angle, Color bgColor, InterpolationMode mode)
        {
            Bitmap bmRotated = new Bitmap(bm.Width,bm.Height);
            using (Graphics g = Graphics.FromImage(bmRotated))
            {
                g.Clear(bgColor);
                g.TranslateTransform(-bm.Width / 2, -bm.Height / 2, MatrixOrder.Append);
                g.RotateTransform(angle,MatrixOrder.Append);
                g.TranslateTransform(bm.Width / 2, bm.Height / 2, MatrixOrder.Append);
                g.InterpolationMode = mode;
                g.DrawImage(bm, new PointF());
            }
            return bmRotated;
        }
        public static Bitmap Scale(this Bitmap bm,float scale, InterpolationMode mode)
        {
            return bm.Scale(scale, scale, mode);
        }
        public static Bitmap Scale(this Bitmap bm,float xscale, float yscale, InterpolationMode mode)
        {
            float newWidth = (float)bm.Width * xscale;
            float newHeight = (float)bm.Height * yscale;
            Bitmap bmScaled = new Bitmap((int)newWidth, (int)newHeight);
            PointF[] destPoints = GetPoints(new Rectangle(0,0,(int)newWidth,(int)newHeight));

            using (Graphics g = Graphics.FromImage(bmScaled))
            {
                g.Clear(Color.Black);
                
                g.DrawImage(bm,destPoints);
            }
            return bmScaled;
        }
        public static Bitmap Crop(this Image image,Rectangle rect, InterpolationMode mode)
        {
            Bitmap bmCropped = new Bitmap(rect.Width,rect.Height);
            PointF[] destPoints = GetPoints(new Rectangle(0, 0, rect.Width, rect.Height));
            using (Graphics g = Graphics.FromImage(bmCropped))
            {
                g.Clear(Color.Black);
                g.DrawImage(image, destPoints,rect,GraphicsUnit.Pixel);
            }
            return bmCropped;
        }
        private static PointF[] GetPoints(RectangleF rectangle)
        {
            return new PointF[3]
            {
            new PointF(rectangle.Left, rectangle.Top),
            new PointF(rectangle.Right, rectangle.Top),
            new PointF(rectangle.Left, rectangle.Bottom)
            };
        }
        public static Rectangle ToRectangle(this Point ptFrom, Point ptTo)
        {
            int left = Math.Min(ptFrom.X, ptTo.X);
            int right = Math.Max(ptFrom.X, ptTo.X);
            int top = Math.Min(ptFrom.Y, ptTo.Y);
            int bottom = Math.Max(ptFrom.Y, ptTo.Y);
            int Width = right - left;
            int Height = bottom - top;
            return new Rectangle(left, top, Width, Height);
        }
        public static void DrawDashedRectangle(this Graphics gr,Color color1, Color color2, float thickness, float dashSize,Point point1, Point point2)
        {
            Rectangle rect = point1.ToRectangle(point2);
            Pen pen = new Pen(color1, thickness);
            gr.DrawRectangle(pen, rect);
            pen.DashPattern = new float[] { dashSize,dashSize };
            pen.Color = color2;
            gr.DrawRectangle(pen, rect);
        }
    }
}