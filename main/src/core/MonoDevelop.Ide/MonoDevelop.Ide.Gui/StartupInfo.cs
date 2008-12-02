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
using System.Collections;

namespace MonoDevelop.Ide.Gui
{
	public class StartupInfo
	{
		static ArrayList requestedFileList = new ArrayList();
		static ArrayList parameterList     = new ArrayList();

		public static string[] GetParameterList()
		{
			return GetStringArray(parameterList);
		}
		
		public static string[] GetRequestedFileList()
		{
			return GetStringArray(requestedFileList);
		}
		
		static string[] GetStringArray(ArrayList list)
		{
			return (string[])list.ToArray(typeof(string));
		}
		
		public static void SetCommandLineArgs(string[] args)
		{
			requestedFileList.Clear();
			parameterList.Clear();
			
			foreach (string arg in args) {
				string a = arg;
				// this does not yet work with relative paths
				if (a[0] == '~') {
					a = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), a.Substring (1));
				}
				
				if (System.IO.File.Exists (a)) {
					a = System.IO.Path.GetFullPath (a);
					requestedFileList.Add (a);
					return;
				}
	
				if (a[0] == '-' || a[0] == '/') {
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
