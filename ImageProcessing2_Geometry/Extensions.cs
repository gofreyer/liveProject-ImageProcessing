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

namespace image_processor
{
    public static class Extensions
    {
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