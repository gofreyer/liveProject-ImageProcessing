using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace image_processor
{
    public partial class InputForm : Form
    {
        public InputForm()
        {
            InitializeComponent();
        }
        internal static float GetFloat(string title, string prompt, string defaultValue,float min, float max, string errString)
        {
            InputForm inputForm = new InputForm();
            inputForm.Text = title;
            inputForm.captionLabel.Text = prompt;
            inputForm.valueTextBox.Text = defaultValue; 
            DialogResult result = inputForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                float value;
                if (float.TryParse(inputForm.valueTextBox.Text, out value))
                {
                    if (value < min || value > max)
                    {
                        MessageBox.Show(errString);
                        return float.NaN;
                    }
                    return value;
                }
                else
                {
                    MessageBox.Show(errString);
                    return float.NaN;
                }
            }
            else if (result == DialogResult.Cancel)
            {
                return float.NaN;
            }
            return float.NaN;
        }
        internal static int GetInt(string title, string prompt, string defaultValue, int min, int max, string errString)
        {
            InputForm inputForm = new InputForm();
            inputForm.Text = title;
            inputForm.captionLabel.Text = prompt;
            inputForm.valueTextBox.Text = defaultValue;
            DialogResult result = inputForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                int value;
                if (int.TryParse(inputForm.valueTextBox.Text, out value))
                {
                    if (value < min || value > max)
                    {
                        MessageBox.Show(errString);
                        return int.MinValue;
                    }
                    return value;
                }
                else
                {
                    MessageBox.Show(errString);
                    return int.MinValue;
                }
            }
            else if (result == DialogResult.Cancel)
            {
                return int.MinValue;
            }
            return int.MinValue;
        }
        internal static string GetString(string title, string prompt, string defaultValue)
        {
            InputForm inputForm = new InputForm();
            inputForm.Text = title;
            inputForm.captionLabel.Text = prompt;
            inputForm.valueTextBox.Text = defaultValue;
            DialogResult result = inputForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                return inputForm.valueTextBox.Text;
            }
            else if (result == DialogResult.Cancel)
            {
                return null;
            }
            return null;
        }
    }
}
