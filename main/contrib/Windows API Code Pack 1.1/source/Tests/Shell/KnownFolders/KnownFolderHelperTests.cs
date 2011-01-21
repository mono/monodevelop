// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;
using Xunit;
using Xunit.Extensions;

namespace Tests
{
    public class KnownFolderHelperTests
    {
        [Theory]
        [PropertyData("KnownFoldersFromReflection")]
        public void FromPathNameTest(IKnownFolder folder)
        {
            IKnownFolder test = KnownFolderHelper.FromPath(folder.Path);
            Assert.True(folder.FolderId == test.FolderId);
        }

        [Theory]
        [PropertyData("KnownFoldersFromReflection")]
        public void FromParsingNameTest(IKnownFolder folder)
        {
            IKnownFolder test = KnownFolderHelper.FromParsingName(folder.ParsingName);
            Assert.True(folder.FolderId == test.FolderId);
        }

        [Theory]
        [PropertyData("KnownFoldersFromReflection")]
        public void FromCanonicalNameTest(IKnownFolder folder)
        {
            IKnownFolder test = KnownFolderHelper.FromCanonicalName(folder.CanonicalName);
            Assert.True(folder.FolderId == test.FolderId);
        }

        public static IEnumerable<object[]> KnownFoldersFromReflection
        {
            get
            {
                PropertyInfo[] staticKnownFolders = typeof(KnownFolders).GetProperties(BindingFlags.Static | BindingFlags.Public);
                foreach (PropertyInfo info in staticKnownFolders)
                {
                    if (info.PropertyType == typeof(IKnownFolder))
                    {
                        IKnownFolder folder = null;
                        try
                        {
                            // the exception this can raise is caused by the path of the known folder
                            // not being found.
                            folder = (IKnownFolder)info.GetValue(null, null);
                        }
                        catch
                        {
                            continue;
                        }

                        yield return new object[] { folder };
                    }
                }
            }
        }

    }
}
