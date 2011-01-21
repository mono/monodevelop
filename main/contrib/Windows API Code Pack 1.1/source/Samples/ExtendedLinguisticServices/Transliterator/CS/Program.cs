// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.ExtendedLinguisticServices;

namespace Transliterator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (!MappingService.IsPlatformSupported)
            {
                MessageBox.Show("This demo requires to be run on Windows 7", "Demo needs Windows 7", MessageBoxButtons.OK, MessageBoxIcon.Error);
                System.Environment.Exit(0);
                return;
            }

            Application.Run(new Transliterator());
        }
    }
}
