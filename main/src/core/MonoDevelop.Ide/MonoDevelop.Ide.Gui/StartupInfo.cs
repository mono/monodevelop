//
// StartupInfo.cs:
//
// Authors:
//   Christian Hergert <christian.hergert@gmail.com>
//   Todd Berman <tberman@off.net>
//   John Luke <john.luke@gmail.com>
//
// Copyright (C) 2005, Christian Hergert
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// Software), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.Gui
{
	public class StartupInfo
	{
		static List<string> requestedFileList = new List<string> ();
		static List<string> parameterList = new List<string> ();

		/// <summary>
		/// Matches a filename string with optional line and column 
		/// (/foo/bar/blah.cs;22;31)
		/// </summary>
		public static Regex fileExpression = new Regex (@"^(?<filename>[^;]+)(;(?<line>\d+))?(;(?<column>\d+))?$", RegexOptions.Compiled);
		
		public static string[] GetParameterList()
		{
			return parameterList.ToArray ();
		}
		
		public static string[] GetRequestedFileList()
		{
			return requestedFileList.ToArray ();
		}
		
		public static bool HasFiles {
			get { return requestedFileList.Count > 0; }
		}
		
		public static void SetCommandLineArgs(string[] args)
		{
			requestedFileList.Clear();
			parameterList.Clear();
			
			foreach (string arg in args) {
				string a = arg;
				Match fileMatch = fileExpression.Match (a);
				
				// this does not yet work with relative paths
				if (a[0] == '~') {
					a = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), a.Substring (1));
				}
				
				if (fileMatch != null && fileMatch.Success) {
					string filename = fileMatch.Groups["filename"].Value;
					if (File.Exists (filename)) {
						a = a.Replace (filename, Path.GetFullPath (filename));
						requestedFileList.Add (a);
					}
				} else if (a[0] == '-' || a[0] == '/') {
					int markerLength = 1;
					
					if (a.Length >= 2 && a[0] == '-' && a[1] == '-') {
						markerLength = 2;
					}
					
					parameterList.Add(a.Substring (markerLength));
				}
			}
		}
	}
}
