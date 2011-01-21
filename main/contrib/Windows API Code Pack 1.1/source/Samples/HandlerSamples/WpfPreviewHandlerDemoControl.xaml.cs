using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;

namespace HandlerSamples
{
    /// <summary>
    /// Interaction logic for WpfPreviewHandlerDemoControl.xaml
    /// </summary>
    public partial class WpfPreviewHandlerDemoControl : UserControl
    {
        public WpfPreviewHandlerDemoControl()
        {
            InitializeComponent();
        }

        public void Populate(XyzFileDefinition definition)
        {
            MemoryStream stream = new MemoryStream(Convert.FromBase64String(definition.EncodedImage));

            BitmapDecoder coder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
            imgEncodedImage.Source = coder.Frames[0];

            txtContent.Text = definition.Content;
            lblName.Content = definition.Properties.Name;
        }
    }
}
