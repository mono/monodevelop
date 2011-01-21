// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using System;
using System.Globalization;
using System.IO;
using Microsoft.WindowsAPICodePack.DirectX;
using System.Drawing;
using Brush = Microsoft.WindowsAPICodePack.DirectX.Direct2D1.Brush;


namespace D2DPaint
{
    enum BrushType
    {
        None,
        Solid,
        Bitmap,
        LinearGradiant,
        RadialGradient
    }

    public partial class BrushDialog : Form
    {
        private readonly Paint2DForm parent;
        private readonly RenderTarget renderTarget;
        private ColorF color1 = new ColorF(Color.Black.ToArgb());
        private ColorF color2 = new ColorF(Color.White.ToArgb());
        private float opacity = 1.0f;
        private string imageFilename;

        public BrushDialog(Paint2DForm parent, RenderTarget renderTarget)
        {
            this.renderTarget = renderTarget;
            this.parent = parent;
            InitializeComponent();
            for (int i = 0; i < transparencyValues.Items.Count; i++)
                transparencyValues.Items[i] = ((string)transparencyValues.Items[i]).Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator);
            FillBrushesListBox();
        }

        private void FillBrushesListBox()
        {
            this.brushesList.Items.Clear();
            foreach (Brush brush in parent.brushes)
            {
                if (brush is SolidColorBrush)
                {
                    SolidColorBrush solidBrush = brush as SolidColorBrush;
                    this.brushesList.Items.Add(
                        string.Format("Solid: R={0}, G={1}, B={2}, A={3}, Opacity={4}", solidBrush.Color.Red, solidBrush.Color.Green, solidBrush.Color.Blue, solidBrush.Color.Alpha, solidBrush.Opacity));
                }
                else if (brush is BitmapBrush)
                {
                    BitmapBrush bitmapBrush = brush as BitmapBrush;
                    this.brushesList.Items.Add(
                        string.Format("Bitmap Brush: Extended Mode X={0}, Extended Mode Y={1}, Inter. Mode={2}", bitmapBrush.ExtendModeX, bitmapBrush.ExtendModeY, bitmapBrush.InterpolationMode));
                }
                else
                {
                    this.brushesList.Items.Add(brush);
                }
            }
            brushesList.SelectedIndex = parent.currentBrushIndex;
            
        }

        private void SelectColorClick(object sender, EventArgs e)
        {
            colorDialog1.Color = System.Drawing.Color.Black;
            if (colorDialog1.ShowDialog() != DialogResult.Cancel)
            {
                color1 = new ColorF(
                    colorDialog1.Color.R / 255F,
                    colorDialog1.Color.G / 255F,
                    colorDialog1.Color.B / 255F,
                    colorDialog1.Color.A / 255F);

                colorLabel.Text = string.Format("R = {0}, G = {1}, B = {2}, A = {3}", color1.Red, color1.Green, color1.Blue, color1.Alpha);
            }
        }

        private void addBrushButton_Click(object sender, EventArgs e)
        {
            parent.brushes.Add(
                        renderTarget.CreateSolidColorBrush(
                        color1,
                        new BrushProperties(opacity, Matrix3x2F.Identity)));

            parent.currentBrushIndex = parent.brushes.Count - 1;

            FillBrushesListBox();
        }

        private void transparencyValues_SelectedIndexChanged(object sender, EventArgs e)
        {
            float f;
            if (float.TryParse(transparencyValues.Text, out f))
            {
                
                this.opacity = f;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            parent.currentBrushIndex = brushesList.SelectedIndex;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog {DefaultExt = "*.jpg;*.png"};
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.imageFilename = dialog.FileName;
                imageFileLabel.Text = Path.GetFileName(imageFilename);
                
            }
        }

        private void addBitmapBrushBotton_Click(object sender, EventArgs e)
        {
            ExtendMode ex = extendedModeXComboBox.SelectedIndex > 0 ? (ExtendMode)extendedModeXComboBox.SelectedIndex : ExtendMode.Wrap;
            ExtendMode ey = extendedModeYComboBox.SelectedIndex > 0 ? (ExtendMode)extendedModeYComboBox.SelectedIndex : ExtendMode.Wrap;

            D2DBitmap brushBitmap = BitmapUtilities.LoadBitmapFromFile(renderTarget, parent.wicFactory, imageFilename);
            BitmapBrush brush = renderTarget.CreateBitmapBrush(
                brushBitmap, 
                new BitmapBrushProperties(
                    ex, ey, 
                    BitmapInterpolationMode.NearestNeighbor), 
                    new BrushProperties(
                        opacity,
                        Matrix3x2F.Identity));
            parent.brushes.Add(brush);
            parent.currentBrushIndex = parent.brushes.Count - 1;
            FillBrushesListBox();
        }

        private void CloseButtonClicked(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OpacityButtonClicked(object sender, EventArgs e)
        {
            float f;
            if (float.TryParse(comboBox2.Text, out f))
            {
                this.opacity = f;
            }
        }

        private void gradiantBrushColor1button_Click(object sender, EventArgs e)
        {

            colorDialog1.Color = System.Drawing.Color.Black;
            if (colorDialog1.ShowDialog() != DialogResult.Cancel)
            {
                color1 = new ColorF(
                    colorDialog1.Color.R / 255F,
                    colorDialog1.Color.G / 255F,
                    colorDialog1.Color.B / 255F,
                    colorDialog1.Color.A / 255F);

                gradBrushColor1Label.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color1.Red, color1.Green, color1.Blue, color1.Alpha);
            }
        }

        private void gradiantBrushColor2Button_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = System.Drawing.Color.Black;
            if (colorDialog1.ShowDialog() != DialogResult.Cancel)
            {
                color2 = new ColorF(
                    colorDialog1.Color.R / 255F,
                    colorDialog1.Color.G / 255F,
                    colorDialog1.Color.B / 255F,
                    colorDialog1.Color.A / 255F);

                gradBrushColor2Label.Text = string.Format("R = {0}, G = {1}, B = {2}, A = {3}", color2.Red, color2.Green, color2.Blue, color2.Alpha);
            }
        }

        private void LinearGradientBrushAddClicked(object sender, EventArgs e)
        {
            ExtendMode ex = gradBrushExtendModeCombo.SelectedIndex > 0 ? (ExtendMode)gradBrushExtendModeCombo.SelectedIndex : ExtendMode.Clamp;
            Gamma gamma;

            switch (gammaComboBox.SelectedIndex)
            {
                case 0:
                    gamma = Gamma.Linear;
                    break;
                case 1:
                    gamma = Gamma.StandardRgb;
                    break;
                default:
                    throw new InvalidOperationException("Unknown gamma selected");
            }

            GradientStop[] stops = 
            {
                new GradientStop(0.0f, color1),
                new GradientStop(1.0f, color2),
            };

            GradientStopCollection stopCollection = renderTarget.CreateGradientStopCollection(
                stops, gamma, ex);

            LinearGradientBrushProperties properties;
            if (ex == ExtendMode.Clamp)
            {
                properties = new LinearGradientBrushProperties(
                    new Point2F(50, 50), new Point2F(600, 400));
            }
            else
            {
                properties = new LinearGradientBrushProperties(
                    new Point2F(50, 50), new Point2F(0, 0));
            }
            

            LinearGradientBrush brush = renderTarget.CreateLinearGradientBrush(
                properties, stopCollection);

            parent.brushes.Add(brush);
            parent.currentBrushIndex = parent.brushes.Count - 1;
            FillBrushesListBox();

        }

        private void RadialGradientBrushAddClicked(object sender, EventArgs e)
        {
            ExtendMode ex = radialExtendCombo.SelectedIndex > 0 ? (ExtendMode)radialExtendCombo.SelectedIndex : ExtendMode.Clamp;
            Gamma gamma;

            switch (gammaComboBox.SelectedIndex)
            {
                case 0:
                    gamma = Gamma.Linear;
                    break;
                case 1:
                    gamma = Gamma.StandardRgb;
                    break;
                default:
                    throw new InvalidOperationException("Unknown gamma selected");
            }

            GradientStop[] stops = 
            {
                new GradientStop(0, color1),
                new GradientStop(1.0f, color2),
            };

            GradientStopCollection stopCollection = renderTarget.CreateGradientStopCollection(
                stops, gamma, ex);

            RadialGradientBrushProperties properties;

            if (ex == ExtendMode.Clamp)
            {
                properties = new RadialGradientBrushProperties(
                    new Point2F(50, 50), new Point2F(600, 400), 600, 600);
            }
            else
            {
                properties = new RadialGradientBrushProperties(
                    new Point2F(50, 50), new Point2F(0, 0), 50, 50);
            }
            
            RadialGradientBrush brush = renderTarget.CreateRadialGradientBrush(
                properties, stopCollection);

            parent.brushes.Add(brush);
            parent.currentBrushIndex = parent.brushes.Count - 1;
            FillBrushesListBox();
        }

        private void SelectRadialColor1_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = System.Drawing.Color.Black;
            if (colorDialog1.ShowDialog() != DialogResult.Cancel)
            {
                color1 = new ColorF(
                    colorDialog1.Color.R / 255F,
                    colorDialog1.Color.G / 255F,
                    colorDialog1.Color.B / 255F,
                    colorDialog1.Color.A / 255F);

                radialBrushColor1Label.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color1.Red, color1.Green, color1.Blue, color1.Alpha);
            }
        }

        private void SelectRadialColor2_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = System.Drawing.Color.Black;
            if (colorDialog1.ShowDialog() != DialogResult.Cancel)
            {
                color2 = new ColorF(
                    colorDialog1.Color.R / 255F,
                    colorDialog1.Color.G / 255F,
                    colorDialog1.Color.B / 255F,
                    colorDialog1.Color.A / 255F);

                radialBrushColor2Label.Text = String.Format("R = {0}, G = {1}, B = {2}, A = {3}", color2.Red, color2.Green, color2.Blue, color2.Alpha);
            }
        }
    }
}
