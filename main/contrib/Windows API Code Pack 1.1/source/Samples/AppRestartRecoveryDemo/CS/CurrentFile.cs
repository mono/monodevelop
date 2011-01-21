// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Microsoft.WindowsAPICodePack.Samples.AppRestartRecoveryDemo
{
    public class FileSettings
    {
        public FileSettings()
        {

        }

        public string Filename
        {
            get;
            set;
        }

        public string Contents
        {
            get;
            set;
        }

        public bool IsDirty
        {
            get;
            set;
        }

        public void Load(string path)
        {
            Contents = File.ReadAllText(path);
            Filename = path;
            IsDirty = false;
        }

        public void Save(string path)
        {
            File.WriteAllText(path, Contents);
            Filename = path;
            IsDirty = false;
        }
    }
}
