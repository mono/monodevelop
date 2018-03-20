//
// LogMessageValidator.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using NUnit.Framework;
using System.Collections.Generic;

namespace MonoDevelop.UserInterfaceTesting
{
	public class LogMessageValidator
	{
		static List<string> invalidLogStrings = new List<string> {
			"Gtk-Critical: void gtk_container_remove(GtkContainer , GtkWidget )"
		};

		public static void Validate (string fileName)
		{
			string readIdeLog = string.Empty;
			using (FileStream fileStream = new FileStream (fileName, FileMode.Open, 
				                               FileAccess.Read, FileShare.ReadWrite)) {
				using (StreamReader streamReader = new StreamReader (fileStream)) {
					readIdeLog = streamReader.ReadToEnd ();
				}
			}

			foreach (var error in invalidLogStrings) {
				Assert.IsFalse (readIdeLog.Contains (error),
					string.Format ("GTK Error detected in Ide.log file:\n\t{0}",error));
			}
		}
	}
}

