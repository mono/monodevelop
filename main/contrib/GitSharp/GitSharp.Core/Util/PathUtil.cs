/*
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace GitSharp.Core.Util
{
    public static class PathUtil
    {
        public static string Combine(params string[] paths)
        {
            if (paths.Length < 2)
                throw new ArgumentException("Must have at least two paths", "paths");

            string path = paths[0];
            for (int i = 0; i < paths.Length; ++i)
            {
                path = Path.Combine(path, paths[i]);
            }
            return path;
        }


        public static DirectoryInfo CombineDirectoryPath(DirectoryInfo path, string subdir)
        {
            return new DirectoryInfo(Path.Combine(path.FullName, subdir));
        }

        public static FileInfo CombineFilePath(DirectoryInfo path, string filename)
        {
            return new FileInfo(Path.Combine(path.FullName, filename));
        }

        /// <summary>
        /// Delete file without complaining about readonly status
        /// </summary>
        /// <param name="path"></param>
        public static bool DeleteFile(this FileSystemInfo path)
        {
            return DeleteFile(path.FullName);
        }

        /// <summary>
        /// Delete file without complaining about readonly status
        /// </summary>
        /// <param name="path"></param>
        public static bool DeleteFile(string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists) return false;

            file.IsReadOnly = false;
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

		  /// <summary>
		  /// Computes relative path, where path is relative to reference_path
		  /// </summary>
		  /// <param name="reference_path"></param>
		  /// <param name="path"></param>
		  /// <returns></returns>
		  public static string RelativePath(string reference_path, string path)
		  {
			  if (reference_path == null)
				  throw new ArgumentNullException("reference_path");
			  if (path == null)
				  throw new ArgumentNullException("path");
			  //reference_path = reference_path.Replace('/', '\\');
			  //path = path.Replace('/', '\\');
			  bool isRooted = Path.IsPathRooted(reference_path) && Path.IsPathRooted(path);
			  if (isRooted)
			  {
				  bool isDifferentRoot = string.Compare(Path.GetPathRoot(reference_path), Path.GetPathRoot(path), true) != 0;
				  if (isDifferentRoot)
					  return path;
			  }
			  var relativePath = new StringCollection();
			  string[] fromDirectories = Regex.Split(reference_path, @"[/\\]+");
			  string[] toDirectories = Regex.Split( path, @"[/\\]+");
			  int length = Math.Min(fromDirectories.Length, toDirectories.Length);
			  int lastCommonRoot = -1;
			  // find common root
			  for (int x = 0; x < length; x++)
			  {
				  if (string.Compare(fromDirectories[x],
						toDirectories[x], true) != 0)
					  break;
				  lastCommonRoot = x;
			  }
			  if (lastCommonRoot == -1)
				  return string.Join(Path.DirectorySeparatorChar.ToString(), toDirectories);
			  // add relative folders in from path
			  for (int x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
				  if (fromDirectories[x].Length > 0)
					  relativePath.Add("..");
			  // add to folders to path
			  for (int x = lastCommonRoot + 1; x < toDirectories.Length; x++)
				  relativePath.Add(toDirectories[x]);
			  // create relative path
			  string[] relativeParts = new string[relativePath.Count];
			  relativePath.CopyTo(relativeParts, 0);
			  string newPath = string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);
			  return newPath;
		  }
    }
}
