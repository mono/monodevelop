//
// UpdateInfo.cs
//
// Author:
//       jason <jaimison@microsoft.com>
//
// Copyright (c) 2018 
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

namespace MonoDevelop.Ide.Updater
{
	public class UpdateInfo
	{
		UpdateInfo ()
		{
		}

		public string File { get; }
		public string AppId { get; }
		public long VersionId { get; }
		public string SourceUrl { get; }

		public UpdateInfo (string file, string appId, long versionId, string sourceUrl)
		{
			SourceUrl = sourceUrl;
			VersionId = versionId;
			AppId = appId;
			File = file;
		}

		public UpdateInfo (string appId, Version version)
			: this (appId, GetVersionId (version))
		{
		}

		public UpdateInfo (string appId, long versionId)
			: this (null, appId, versionId, null)
		{
		}

		const string sourceUrl = "source-url:";

		public static UpdateInfo FromFile (string fileName)
		{
			using (var f = System.IO.File.OpenText (fileName)) {
				var s = f.ReadLine ();
				var parts = s.Split (' ');
				string url = null;
				s = f.ReadLine ();
				if (s != null) {
					if (s.StartsWith (sourceUrl)) {
						url = s.Substring (sourceUrl.Length).Trim ();
					}
				}
				return new UpdateInfo (fileName, parts [0], long.Parse (parts [1]), url);//, xamarinProduct);
			}
		}

		static long GetVersionId (Version version)
		{
			string versionId = GetVersionIdText (version.Major, version.Minor, version.Build, version.Revision);
			return long.Parse (versionId);
		}

		static string GetVersionIdText (int major, int minor, int build, int revision)
		{
			return FixVersionNumber (major) +
				FixVersionNumber (minor).ToString ("00") +
				FixVersionNumber (build).ToString ("00") +
				FixVersionNumber (revision).ToString ("0000");
		}

		/// <summary>
		/// Unspecified version numbers can be -1 so map this to 0 instead.
		/// </summary>
		static int FixVersionNumber (int number)
		{
			if (number == -1)
				return 0;

			return number;
		}
	}
}
