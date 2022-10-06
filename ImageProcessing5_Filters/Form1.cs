using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Drawing.Drawing2D;
using static System.Net.Mime.MediaTypeNames;
using System.Runtime.InteropServices;

namespace image_processor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Bitmap OriginalBm = null;
        private Bitmap CurrentBm = null;
        private Point StartPoint, EndPoint;

        private void Form1_Load(object sender, EventArgs e)
        {
            // Disable menu items because no image is loaded.
            SetMenusEditable(false);

            /*
            byte r = 128;
            byte g = 64;
            byte b = 255;
            byte r1, g1, b1;

            double h, s, l;

            Extensions.RGBtoHLS(r, g, b, out h, out l, out s);
            Extensions.HLStoRGB(h, l, s, out r1, out g1, out b1);
            */

        }

        // Enable or disable menu items that are
        // appropriate when an image is loaded.
        private void SetMenusEditable(bool enabled)
        {
            ToolStripMenuItem[] items =
            {
                mnuFileSaveAs,
                mnuFileReset,
                mnuGeometry,
                mnuPointOperations,
                mnuEnhancements,
                mnuFilters,
            };
            foreach (ToolStripMenuItem item in items)
                item.Enabled = enabled;
            resultPictureBox.Visible = enabled;
        }

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            ofdFile.Title = "Open Image File";
            ofdFile.Multiselect = false;
            if (ofdFile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Bitmap bm = LoadBitmapUnlocked(ofdFile.FileName);
                    OriginalBm = bm;
                    CurrentBm = (Bitmap)OriginalBm.Clone();
                    resultPictureBox.Image = CurrentBm;

                    // Enable menu items because an image is loaded.
                    SetMenusEditable(true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(
                        "Error opening file {0}.\n{1}",
                        ofdFile.FileName, ex.Message));
                }
            }
        }

        private void mnuFileSaveAs_Click(object sender, EventArgs e)
        {
            sfdFile.Title = "Save As";
            if (sfdFile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    CurrentBm.SaveImage(sfdFile.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(
                        "Error saving file {0}.\n{1}",
                        sfdFile.FileName, ex.Message));
                }
            }
        }

        // Restore the original unmodified image.
        private void mnuFileReset_Click(object sender, EventArgs e)
        {
            CurrentBm = (Bitmap)OriginalBm.Clone();
            resultPictureBox.Image = CurrentBm;
        }

        // Make a montage of files.
        private void mnuFileMontage_Click(object sender, EventArgs e)
        {
            // Let the user select the files.
            ofdFile.Title = "Select Montage Files";
            ofdFile.Multiselect = true;
            if (ofdFile.ShowDialog() == DialogResult.OK)
            {
                OriginalBm = MakeMontage(ofdFile.FileNames, Color.Black);
                if (OriginalBm != null)
                {
                    CurrentBm = (Bitmap)OriginalBm.Clone();
                    resultPictureBox.Image = CurrentBm;

                    // Enable menu items because an image is loaded.
                    SetMenusEditable(true);
                }
            }
        }

        // Make a montage of files, four per row.
        private Bitmap MakeMontage(string[] filenames, Color bgColor)
        {
            List<Bitmap> images = new List<Bitmap>();
            int maxWidth = 0;
            int maxHeight = 0;  
            foreach (string filename in filenames)
            {
                Bitmap bm = LoadBitmapUnlocked(filename);
                images.Add(bm);

                if (bm.Width > maxWidth)
                {
                    maxWidth = bm.Width;    
                }
                if (bm.Height > maxHeight)
                {
                    maxHeight = bm.Height;  
                }
            }
            int rows = 0;
            int cols = 0;
            if (images.Count > 0)
            {
                rows = (images.Count - 1) / 4 + 1;
                if (rows <= 1)
                {
                    cols = images.Count;
                }
                else
                {
                    cols = 4;
                }
                Bitmap bmMontage = new Bitmap(cols * maxWidth, rows * maxHeight);

                using (Graphics g = Graphics.FromImage(bmMontage))
                {
                    g.Clear(bgColor);
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            int index = r * cols + c;
                            if (index < images.Count)
                            {
                                Bitmap bm = images[r * cols + c];
                                g.DrawImage(bm, c * maxWidth, r * maxHeight);
                            }
                        }
                    }
                }
                return bmMontage;
            }

            return null;
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Load a bitmap without locking it.
        private Bitmap LoadBitmapUnlocked(string file_name)
        {
            using (Bitmap bm = new Bitmap(file_name))
            {
                return new Bitmap(bm);
            }
        }

        // Rotate the image.
        private void mnuGeometryRotate_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                float rotate = InputForm.GetFloat("Set the rotation angle", "Rotation angle", "90", float.NegativeInfinity, float.PositiveInfinity, "The value does not fit.");
                if (float.IsNaN(rotate))
                {
                    return;
                }
                CurrentBm = CurrentBm.RotateAtCenter(rotate, Color.Black, InterpolationMode.High);
                resultPictureBox.Image = CurrentBm;
            }
        }

        // Scale the image uniformly.
        private void mnuGeometryScale_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                float scale = InputForm.GetFloat("Set the scale factor", "Scale factor between 0.01 and 100", "2", (float)0.01, (float)100, "The value does not fit.");
                if (float.IsNaN(scale))
                {
                    return;
                }
                CurrentBm = CurrentBm.Scale(scale,InterpolationMode.High);
                resultPictureBox.Image = CurrentBm;
            }
        }

        private void mnuGeometryStretch_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                float scaleX = 1;
                float scaleY = 1;
                string scale = InputForm.GetString("Set the stretch factors", "Stretch factors as string pattern 'scale-X;scale-Y'", "0,5;2,0");
                if (scale == null)
                {
                    return;
                }
                string[] scales = scale.Split(';');
                if (scales.Length == 2)
                {
                    if (float.TryParse(scales[0], out scaleX) && float.TryParse(scales[1], out scaleY) && scaleX>0 && scaleY>0)
                    {
                        CurrentBm = CurrentBm.Scale(scaleX, scaleY, InterpolationMode.High);
                        resultPictureBox.Image = CurrentBm;
                        return;
                    }
                }
                MessageBox.Show("The stretch factors do not fit.");
            }
        }

        private void mnuGeometryRotateFlip_Click(object sender, EventArgs e)
        {
            InputForm inputForm = new InputForm();
            inputForm.Text = "Choose the Rotate/Flip-command";
            inputForm.captionLabel.Text = "1) Flip Horizontal\n2) Flip Vertical\n3) Rotate 90\n4) Rotate 180\n5) Rotate 270";
            inputForm.valueTextBox.Text = "1";
            inputForm.Size = new Size(500, 300);
            DialogResult result = inputForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                int select = -1;
                if (int.TryParse(inputForm.valueTextBox.Text, out select) && select >=1 && select <=5)
                {
                    switch(select)
                    {
                        case 1:
                            CurrentBm.RotateFlip(RotateFlipType.RotateNoneFlipX);
                            break;
                        case 2:
                            CurrentBm.RotateFlip(RotateFlipType.RotateNoneFlipY);
                            break;
                        case 3:
                            CurrentBm.RotateFlip(RotateFlipType.Rotate90FlipNone);
                            break;
                        case 4:
                            CurrentBm.RotateFlip(RotateFlipType.Rotate180FlipNone);
                            break;
                        case 5:
                            CurrentBm.RotateFlip(RotateFlipType.Rotate270FlipNone);
                            break;
                        default:
                            break;
                    }
                    resultPictureBox.Image = CurrentBm;

                }
                else
                {
                    MessageBox.Show("The select command does not fit.");
                }
            }
            
            return;

        }

        #region Cropping

        // Let the user select an area and crop to that area.
        private void mnuGeometryCrop_Click(object sender, EventArgs e)
        {
            resultPictureBox.MouseDown += crop_MouseDown;
            resultPictureBox.Cursor = Cursors.Cross;
        }

        // Let the user select an area with a desired
        // aspect ratio and crop to that area.
        private void mnuGeometryCropToAspect_Click(object sender, EventArgs e)
        {
           
        }

        private void crop_MouseDown(object sender, MouseEventArgs e)
        {
            resultPictureBox.MouseDown -= crop_MouseDown;
            resultPictureBox.MouseMove += crop_MouseMove;
            resultPictureBox.MouseUp += crop_MouseUp;
            resultPictureBox.Paint += resultPictureBox_Paint;
            StartPoint = new Point(e.X, e.Y);
            EndPoint = StartPoint;
        }
        private void crop_MouseMove(object sender, MouseEventArgs e)
        {
            EndPoint = new Point(e.X, e.Y);
            resultPictureBox.Refresh();
        }
        private void crop_MouseUp(object sender, MouseEventArgs e)
        {
            resultPictureBox.Cursor = Cursors.Default;
            resultPictureBox.MouseMove -= crop_MouseMove;
            resultPictureBox.MouseUp -= crop_MouseUp;
            resultPictureBox.Paint -= resultPictureBox_Paint;
            CurrentBm = resultPictureBox.Image.Crop(StartPoint.ToRectangle(EndPoint),InterpolationMode.High);
            resultPictureBox.Image = CurrentBm;
        }
        private void resultPictureBox_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawDashedRectangle(Color.Blue, Color.Yellow, (float)1.5, 3, StartPoint, EndPoint);
        }

        #endregion Cropping

        #region Point Processes

        // Set each color component to 255 - the original value.
        private void mnuPointInvert_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    r = (byte)(255 - r);
                    g = (byte)(255 - g);
                    b = (byte)(255 - b);
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }

        }

        // Set color components less than a specified value to 0.
        private void mnuPointColorCutoff_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                int cutoffValue = InputForm.GetInt("Set the cutoff value", "Cutoff value between 0 and 255", "128", (int)0, (int)255, "The value does not fit.");
                if (!(cutoffValue > int.MinValue))
                {
                    return;
                }
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    r = (byte)(r < cutoffValue ? 0 : r);
                    g = (byte)(g < cutoffValue ? 0 : g);
                    b = (byte)(b < cutoffValue ? 0 : b);
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }
        }

        // Set each pixel's red color component to 0.
        private void mnuPointClearRed_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    r = (byte)(0);
                    //g = g;
                    //b = b;
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }

        }

        // Set each pixel's green color component to 0.
        private void mnuPointClearGreen_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    //r = r;
                    g = (byte)(0);
                    //b = b;
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }
        }

        // Set each pixel's blue color component to 0.
        private void mnuPointClearBlue_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    //r = r;
                    //g = g;
                    b = (byte)(0);
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }
        }

        // Average each pixel's color component.
        private void mnuPointAverage_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                byte avg;
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    avg = (byte)(((int)(r + g + b)) / 3);
                    r = avg;
                    g = avg;
                    b = avg;
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }

        }

        // Convert each pixel to grayscale.
        private void mnuPointGrayscale_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                byte avg;
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    //avg = (byte)(r * 0.3 + g * 0.5 + b * 0.2);
                    avg = (byte)(r * 0.2126 + g * 0.7152 + b * 0.0722);

                    r = avg;
                    g = avg;
                    b = avg;
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }

        }

        // Convert each pixel to sepia tone.
        private void mnuPointSepiaTone_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    float newR = r * 0.393f + g * 0.769f + b * 0.189f;
                    float newG = r * 0.349f + g * 0.686f + b * 0.168f;
                    float newB = r * 0.272f + g * 0.534f + b * 0.131f;

                    r = (byte)(newR > 255 ? 255 : newR);
                    g = (byte)(newG > 255 ? 255 : newG);
                    b = (byte)(newB > 255 ? 255 : newB);
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }
        }

        // Apply a color tone to the image.
        private void mnuPointColorTone_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                ColorDialog colDlg = new ColorDialog();
                DialogResult result = colDlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    float newR = (float)colDlg.Color.R;
                    float newG = (float)colDlg.Color.G;
                    float newB = (float)colDlg.Color.B;

                    CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                    {
                        float brightness = (r + g + b) / (3f * 255f);

                        r = (byte)(brightness * newR);
                        g = (byte)(brightness * newG);
                        b = (byte)(brightness * newB);
                        //a = a;
                    });
                    resultPictureBox.Image = CurrentBm;
                }
            }
        }

        // Set non-maximal color components to 0.
        private void mnuPointSaturate_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    byte max = (byte)Math.Max(r, g);
                    max = (byte)Math.Max(max, b);
                    r = (byte)(r < max ? 0 : r);
                    g = (byte)(g < max ? 0 : g);
                    b = (byte)(b < max ? 0 : b);
                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }
        }

        #endregion Point Processes

        #region Enhancements

        private void mnuEnhancementsColor_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                float saturationValue = InputForm.GetFloat("Set the saturation value", "Saturation value between 0 and 2", "1", (float)0, (float)2, "The value does not fit.");
                if (float.IsNaN(saturationValue))
                {
                    return;
                }
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    double h, l, s;
                    Extensions.RGBtoHLS(r, g, b, out h, out l, out s);
                    s = Extensions.AdjustValue(s, saturationValue);
                    Extensions.HLStoRGB(h, l, s, out r, out g, out b);

                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }

        }

        // Use histogram stretching to modify contrast.
        private void mnuEnhancementsContrast_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                //float contrastValue = InputForm.GetFloat("Set the contrast value", "Contrast value between -10 and 10", "1", (float)-10, (float)10, "The value does not fit.");
                float contrastValue = InputForm.GetFloat("Set the contrast value", "Contrast value between 0 and 100", "1", (float)0, (float)100, "The value does not fit.");
                if (float.IsNaN(contrastValue))
                {
                    return;
                }
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    float dR = r;
                    float dG = g;
                    float dB = b;

                    float factor = Math.Abs(((float)contrastValue) / 10.0f);

                    if (contrastValue > 0)
                    {
                        // more contrast
                        // move RGB away from 128

                        /*
                        float diffR_255 = Math.Abs(r - 255);
                        float diffG_255 = Math.Abs(g - 255);
                        float diffB_255 = Math.Abs(b - 255);

                        if (r > 128)
                        {
                            dR = dR + factor * diffR_255;    
                        }
                        else if (r < 128)
                        {
                            dR = dR - factor * dR;
                        }
                        if (g > 128)
                        {
                            dG = dG + factor * diffG_255;
                        }
                        else if (g < 128)
                        {
                            dG = dG - factor * dG;
                        }
                        if (b > 128)
                        {
                            dB = dB + factor * diffB_255;
                        }
                        else if (b < 128)
                        {
                            dB = dB - factor * dB;
                        }
                        */
                        dR = 128 + (dR - 128) * contrastValue;
                        dG = 128 + (dG - 128) * contrastValue;
                        dB = 128 + (dB - 128) * contrastValue;
                    }
                    else if (contrastValue < 0)
                    {
                        // less contrast
                        // move RGB near to 128

                        float diffR = Math.Abs(r - 128);
                        float diffG = Math.Abs(g - 128);
                        float diffB = Math.Abs(b - 128);
                        
                        if (r > 128)
                        {
                            dR = 128 + (1-factor) * diffR;
                        }
                        else if (r < 128)
                        {
                            dR = 128 - (1-factor) * diffR;
                        }
                        if (g > 128)
                        {
                            dG = 128 + (1 - factor) * diffG;
                        }
                        else if (g < 128)
                        {
                            dG = 128 - (1 - factor) * diffG;
                        }
                        if (b > 128)
                        {
                            dB = 128 + (1 - factor) * diffB;
                        }
                        else if (b < 128)
                        {
                            dB = 128 - (1 - factor) * diffB;
                        }
                    }

                    r = dR.ToByte();
                    g = dG.ToByte();
                    b = dB.ToByte();

                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }

        }

        private void mnuEnhancementsBrightness_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                float brightnessValue = InputForm.GetFloat("Set the brightness value", "Brightness value between 0 and 2", "1", (float)0, (float)2, "The value does not fit.");
                if (float.IsNaN(brightnessValue))
                {
                    return;
                }
                CurrentBm.ApplyPointOp((ref byte r, ref byte g, ref byte b, ref byte a) =>
                {
                    double h, l, s;
                    Extensions.RGBtoHLS(r, g, b, out h, out l, out s);
                    l = Extensions.AdjustValue(l, brightnessValue);
                    Extensions.HLStoRGB(h,l,s,out r, out g, out b);

                    //a = a;
                });
                resultPictureBox.Image = CurrentBm;
            }
        }

        #endregion Enhancements

        #region Filters

        private void mnuFiltersBoxBlur_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                int blurradius = InputForm.GetInt("Set the blur radius", "Blur radius between 0 and 255", "1", (int)0, (int)255, "The value does not fit.");
                if (!(blurradius > int.MinValue))
                {
                    return;
                }
                CurrentBm = CurrentBm.BoxBlur(blurradius);
                resultPictureBox.Image = CurrentBm;
            }
        }

        private void mnuFiltersUnsharpMask_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                int radius = 1;
                float amount = 1;
                string unsharpMaskParams = InputForm.GetString("Set the unsharpmask parameters", "Unsharpmask parameters as string pattern 'radius;amount'", "1;10,0");
                if (unsharpMaskParams == null)
                {
                    return;
                }
                string[] umParams = unsharpMaskParams.Split(';');
                if (umParams.Length == 2)
                {
                    if (int.TryParse(umParams[0], out radius) && float.TryParse(umParams[1], out amount) && radius > 0 && amount > 0)
                    {
                        CurrentBm = CurrentBm.UnsharpMask(radius, amount);
                        resultPictureBox.Image = CurrentBm;
                        return;
                    }
                }
                MessageBox.Show("The unsharpmask parameters do not fit.");
            }

        }

        private void mnuFiltersRankFilter_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                int radius = 1;
                int rank;
                string rankParams = InputForm.GetString("Set the rank parameters", "Rank parameters as string pattern 'radius;rank'", "1;3");
                if (rankParams == null)
                {
                    return;
                }
                string[] rParams = rankParams.Split(';');
                if (rParams.Length == 2)
                {
                    if (int.TryParse(rParams[0], out radius) && int.TryParse(rParams[1], out rank) && radius > 0 && rank > 0)
                    {
                        CurrentBm = CurrentBm.RankFilter(radius, radius, rank);
                        resultPictureBox.Image = CurrentBm;
                        return;
                    }
                }
                MessageBox.Show("The rank parameters do not fit.");
            }
        }

        private void mnuFiltersMedianFilter_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                int medianradius = InputForm.GetInt("Set the median radius", "Median radius between 0 and 255", "1", (int)0, (int)255, "The value does not fit.");
                if (!(medianradius > int.MinValue))
                {
                    return;
                }
                int rank = (2 * medianradius + 1)* (2 * medianradius + 1) / 2;
                CurrentBm = CurrentBm.RankFilter(medianradius, medianradius, rank);
                resultPictureBox.Image = CurrentBm;
            }
        }

        private void mnuFiltersMinFilter_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                int minradius = InputForm.GetInt("Set the min radius", "Min radius between 0 and 255", "1", (int)0, (int)255, "The value does not fit.");
                if (!(minradius > int.MinValue))
                {
                    return;
                }
                int rank = 0;
                CurrentBm = CurrentBm.RankFilter(minradius, minradius, rank);
                resultPictureBox.Image = CurrentBm;
            }

        }

        private void mnuFiltersMaxFilter_Click(object sender, EventArgs e)
        {
            if (CurrentBm != null)
            {
                int maxradius = InputForm.GetInt("Set the max radius", "Max radius between 0 and 255", "1", (int)0, (int)255, "The value does not fit.");
                if (!(maxradius > int.MinValue))
                {
                    return;
                }
                int rank = (2 * maxradius + 1) * (2 * maxradius + 1) - 1;
                CurrentBm = CurrentBm.RankFilter(maxradius, maxradius, rank);
                resultPictureBox.Image = CurrentBm;
            }

        }

        // Display a dialog where the user can select
        // and modify a default kernel.
        // If the user clicks OK, apply the kernel.
        private void mnuFiltersCustomKernel_Click(object sender, EventArgs e)
        {
            KernelForm kernelForm = new KernelForm();
            DialogResult result = kernelForm.ShowDialog();
            if (result == DialogResult.Cancel)
            {
                return;
            }
            int weight = 0;
            int offset = 0;
            float[,] kernel = GetKernel(kernelForm.valueTextBox.Text);
            if (int.TryParse(kernelForm.weightTextBox.Text, out weight) && int.TryParse(kernelForm.offsetTextBox.Text, out offset) && kernel != null)
            {
                CurrentBm = CurrentBm.ApplyKernel(kernel, 1.0f / (float)weight, (float)offset);
                resultPictureBox.Image = CurrentBm;
            }
        }

        private static float[,] GetKernel(string kernelText)
        {
            kernelText = kernelText.Trim();
            if (kernelText.Length <= 0)
            {
                return null;
            }
            List<String> rows = new List<String>();
            int start = 0;
            int count = 0;
            int position;
            do
            {
                position = kernelText.IndexOf("\r\n", start);
                if (position >= 0)
                {
                    count++;
                    rows.Add(kernelText.Substring(start, position - start).Trim());
                    start = position + 2;
                }
            } while (position > 0);
            string rest = kernelText.Substring(start).Trim();
            if (rest.Length > 0)
            {
                rows.Add(rest);
            }

            int rowCount = rows.Count;
            int colCount = 0;

            for (int r = 0; r < rowCount; r++)
            {
                string[] cols = rows[r].Split(',');
                if (cols.Length > colCount) colCount = cols.Length;
            }

            if (rowCount <= 0 || colCount <= 0)
            {
                return null;
            }

            float[,] kernel = new float[rowCount, colCount];
            
            for (int r = 0; r < rowCount; r++)
            {
                string[] cols = rows[r].Split(',');
                for (int c = 0; c < colCount; c++)
                {
                    int val = 0;
                    if (c < cols.Length)
                    {
                        if (!int.TryParse(cols[c].Trim(),out val))
                        {
                            val = 0;
                        }
                    }
                    kernel[r,c] = (float)(val);
                }
            }
            return kernel;
        }
        #endregion Filters

    }
}


