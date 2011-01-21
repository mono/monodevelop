using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.ShellExtensions;

namespace HandlerSamples
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("HandlerSamples.XYZPreviewerWPF")]
    [Guid("B9E6A036-9778-4B48-BA45-33F15B9B07AF")]
    [PreviewHandler("PreviewHandlerWPFDemo", ".xyz", "{EC3E84CC-BDC5-4E9F-A67F-CC960F366497}")]
    public class WPFPreviewHandlerDemo : WpfPreviewHandler, IPreviewFromFile, IPreviewFromShellObject
    {
        public WPFPreviewHandlerDemo()
        {
            Control = new WpfPreviewHandlerDemoControl();
        }

        private void Populate(Stream stream)
        {
            XyzFileDefinition definition = new XyzFileDefinition(stream);

            if (definition != null)
            {
                ((WpfPreviewHandlerDemoControl)Control).Populate(definition);
            }
        }

        #region IPreviewFromFile Members

        public void Load(FileInfo info)
        {
            using (var stream = new FileStream(info.FullName, FileMode.Open, FileAccess.Read))
            {
                Populate(stream);
            }
        }

        #endregion

        #region IPreviewFromShellObject Members

        public void Load(ShellObject shellObject)
        {
            using (var stream = new FileStream(shellObject.ParsingName, FileMode.Open, FileAccess.Read))
            {
                Populate(stream);
            }
        }

        #endregion
    }
}
