// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;
using Xunit;

namespace Tests
{
    public class KnownFoldersTests
    {
        [Fact]
        public void VerifyDefaultFoldersInAllCollection()
        {
            ICollection<IKnownFolder> folders = KnownFolders.All;

            PropertyInfo[] properties = typeof(KnownFolders).GetProperties(BindingFlags.Static | BindingFlags.Public);
            foreach (PropertyInfo info in properties)
            {
                if (info.PropertyType == typeof(IKnownFolder))
                {
                    IKnownFolder kf = null;
                    try
                    {
                        //if an exception is thrown, the known folder does not exist on the computer.
                        kf = (IKnownFolder)info.GetValue(null, null);
                    }
                    catch
                    {
                        continue;
                    }

                    Assert.True(folders.Any(x => kf.FolderId == x.FolderId));
                }
            }
        }

    }
}
