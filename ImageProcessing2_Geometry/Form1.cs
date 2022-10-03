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

        private void Form1_Load(object sender, EventArgs e)
        {
            // Disable menu items because no image is loaded.
            SetMenusEditable(false);
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

        }

        // Let the user select an area with a desired
        // aspect ratio and crop to that area.
        private void mnuGeometryCropToAspect_Click(object sender, EventArgs e)
        {

        }

        #endregion Cropping

        #region Point Processes

        // Set each color component to 255 - the original value.
        private void mnuPointInvert_Click(object sender, EventArgs e)
        {

        }

        // Set color components less than a specified value to 0.
        private void mnuPointColorCutoff_Click(object sender, EventArgs e)
        {

        }

        // Set each pixel's red color component to 0.
        private void mnuPointClearRed_Click(object sender, EventArgs e)
        {

        }

        // Set each pixel's green color component to 0.
        private void mnuPointClearGreen_Click(object sender, EventArgs e)
        {

        }

        // Set each pixel's blue color component to 0.
        private void mnuPointClearBlue_Click(object sender, EventArgs e)
        {

        }

        // Average each pixel's color component.
        private void mnuPointAverage_Click(object sender, EventArgs e)
        {

        }

        // Convert each pixel to grayscale.
        private void mnuPointGrayscale_Click(object sender, EventArgs e)
        {

        }

        // Convert each pixel to sepia tone.
        private void mnuPointSepiaTone_Click(object sender, EventArgs e)
        {

        }

        // Apply a color tone to the image.
        private void mnuPointColorTone_Click(object sender, EventArgs e)
        {

        }

        // Set non-maximal color components to 0.
        private void mnuPointSaturate_Click(object sender, EventArgs e)
        {

        }

        #endregion Point Processes

        #region Enhancements

        private void mnuEnhancementsColor_Click(object sender, EventArgs e)
        {

        }

        // Use histogram stretching to modify contrast.
        private void mnuEnhancementsContrast_Click(object sender, EventArgs e)
        {

        }

        private void mnuEnhancementsBrightness_Click(object sender, EventArgs e)
        {

        }

        #endregion Enhancements

        #region Filters

        private void mnuFiltersBoxBlur_Click(object sender, EventArgs e)
        {

        }

        private void mnuFiltersUnsharpMask_Click(object sender, EventArgs e)
        {

        }

        private void mnuFiltersRankFilter_Click(object sender, EventArgs e)
        {

        }

        private void mnuFiltersMedianFilter_Click(object sender, EventArgs e)
        {

        }

        private void mnuFiltersMinFilter_Click(object sender, EventArgs e)
        {

        }

        private void mnuFiltersMaxFilter_Click(object sender, EventArgs e)
        {

        }

        // Display a dialog where the user can select
        // and modify a default kernel.
        // If the user clicks OK, apply the kernel.
        private void mnuFiltersCustomKernel_Click(object sender, EventArgs e)
        {

        }

        #endregion Filters

    }
}
