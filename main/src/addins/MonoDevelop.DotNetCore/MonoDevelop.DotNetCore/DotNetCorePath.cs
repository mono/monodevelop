//
// DotNetCorePath.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.DotNetCore
{
	class DotNetCorePath
	{
		public DotNetCorePath ()
		{
			FileName = GetDotNetFileName ();
			Exists = !IsMissing;
		}

		public DotNetCorePath (string fileName)
		{
			FileName = fileName;
			Exists = File.Exists (fileName);
			IsMissing = !Exists;
		}

		public string FileName { get; private set; }
		public bool IsMissing { get; private set; }
		public bool Exists { get; private set; }

		public string ParentDirectory {
			get {
				if (!string.IsNullOrEmpty (FileName)) {
					return Path.GetDirectoryName (FileName);
				}
				return null;
			}
		}

		string GetDotNetFileName ()
		{
			if (Platform.IsWindows) {
				return GetDotNetFileNameOnWindows ();
			} else {
				string dotnetFileName = GetDefaultPath ();
				if (File.Exists (dotnetFileName)) {
					return dotnetFileName;
				}

				IsMissing = true;
			}

			return "dotnet";
		}

		/// <summary>
		/// IDE runs as a 32-bit process on Windows. Look for the 64 bit .NET Core SDK
		/// first by using the ProgramW6432 environment variable. Then fallback to
		/// looking in Program Files (x86).
		/// </summary>
		string GetDotNetFileNameOnWindows ()
		{
			const string executableName = "dotnet.exe";
			string dotnetFileName;

			string programFiles = Environment.GetEnvironmentVariable ("ProgramW6432");
			if (!string.IsNullOrEmpty (programFiles)) {
				dotnetFileName = Path.Combine (programFiles, "dotnet", executableName);
				if (File.Exists (dotnetFileName))
					return dotnetFileName;
			}

			programFiles = Environment.GetFolderPath (Environment.SpecialFolder.ProgramFiles);
			dotnetFileName = Path.Combine (programFiles, "dotnet", executableName);
			if (File.Exists (dotnetFileName))
				return dotnetFileName;

			IsMissing = true;

			return executableName;
		}

		public bool IsDefault ()
		{
			FilePath defaultPath = GetDefaultPath ();
			var currentPath = new FilePath (FileName);
			return currentPath == defaultPath;
		}

		static string GetDefaultPath ()
		{
			if (Platform.IsMac)
				return "/usr/local/share/dotnet/dotnet";

			if (Platform.IsLinux)
				return "/usr/share/dotnet/dotnet";

			// Windows.
			return "dotnet.exe";
		}
	}
}
