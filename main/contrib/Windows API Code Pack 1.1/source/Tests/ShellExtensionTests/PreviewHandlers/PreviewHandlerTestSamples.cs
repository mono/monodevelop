using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.ShellExtensions;
using ShellExtensionTests;

namespace Tests.ShellExtensions.PreviewHandlers
{
    public class WinformsPreviewHandlerTestSample : WinFormsPreviewHandler, IPreviewFromStream
    {
        public WinformsPreviewHandlerTestSample()
        {
            Control = new WinFormsPreviewHandlerSampleForm();
        }


        #region IPreviewFromStream Members

        public void Load(System.IO.Stream stream)
        {
            
        }

        #endregion
    }

}
