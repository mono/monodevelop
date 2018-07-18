//
// Util.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using System.Reflection;
using System.Linq;

namespace MonoDevelop.UserInterfaceTesting
{
	public static class Util
	{
		public static void PrintData (this object data)
		{
			if (data != null)
				TestService.Session.DebugObject.Debug (data.ToString ());
		}

		public static string ToPathSafeString (this string str, char replaceWith = '-')
		{
			var invalids = Path.GetInvalidFileNameChars ().Concat (Path.GetInvalidPathChars ()).Distinct ().ToArray ();
			return new string (str.Select (c => invalids.Contains (c) ? replaceWith : c).ToArray ());
		}

		public static string ToBoldText (this string str)
		{
			return str != null ? string.Format ("<b>{0}</b>", str) : null;
		}

		public static FilePath CreateTmpDir (string hint = null)
		{
			var cwd = new FileInfo (Assembly.GetExecutingAssembly ().Location).DirectoryName;
			string tempDirectory = Path.Combine (cwd, Path.GetRandomFileName());
			tempDirectory = hint != null ? Path.Combine (tempDirectory, hint) : tempDirectory;

			if (!Directory.Exists (tempDirectory))
				Directory.CreateDirectory (tempDirectory);
			return tempDirectory;
		}

		public static Action GetAction (this BeforeBuildAction action)
		{
			switch (action) {
			case BeforeBuildAction.None:
				return Ide.EmptyAction;
			case BeforeBuildAction.WaitForPackageUpdate:
				return Ide.WaitForPackageUpdate;
			case BeforeBuildAction.WaitForSolutionCheckedOut:
				return Ide.WaitForSolutionCheckedOut;
			default:
				return Ide.EmptyAction;
			}
		}

		public static Action<string> GetNonNullAction (Action<string> action)
		{
			return action ?? delegate { };
		}

		public static string StripBold (this string value)
		{
			return value != null ? value.Replace ("<b>", string.Empty).Replace ("</b>", string.Empty) : null;
		}
	}
}
