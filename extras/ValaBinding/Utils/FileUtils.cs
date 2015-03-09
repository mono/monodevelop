using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MonoDevelop.ValaBinding.Utils
{
    public class FileUtils
    {
        public static void RemoveBOM(string filePath)
        {
            var bomSize = GetBomSize(filePath);
            if (bomSize > 0)
            {
                var content = File.ReadAllBytes(filePath);
                WriteBytes(filePath, content, bomSize);
            }
        }

        public static int GetBomSize(string filePath)
        {
            var bom = Encoding.UTF8.GetPreamble();

            using (Stream source = File.OpenRead(filePath))
            {
                var buffer = new byte[bom.Length];
                var bytesRead = source.Read(buffer, 0, buffer.Length);
                if (bytesRead == buffer.Length)
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] != bom[i])
                        {
                            return 0;
                        }
                    }

                    return bom.Length;
                }
            }

            return 0;
        }

        public static void WriteBytes(string filePath, byte[] content, int srcOffset)
        {
            using (Stream dest = File.Open(filePath, FileMode.Create, FileAccess.Write))
            {
                dest.Write(content, srcOffset, content.Length - srcOffset);
            }
        }

        /// <summary>
        /// Converts path to correctly cased version of it.
        /// </summary>
        public static string GetExactPathName(string pathName)
        {
            if (!(File.Exists(pathName) || Directory.Exists(pathName)))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else
            {
                return di.Name.ToUpper();
            }
        }
    }
}
