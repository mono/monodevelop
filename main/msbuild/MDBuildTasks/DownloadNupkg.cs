//
// Copyright (c) Microsoft Corp (https://www.microsoft.com)
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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using System.Text;

namespace MDBuildTasks
{
	public class DownloadNupkg : ToolTask
	{
		[Required]
		public ITaskItem[] Packages { get; set; }

		[Required]
		public string PackagesDirectory { get; set; }

		[Required]
		public string StampFile { get; set; }

		[Required]
		public string PackagesConfigFile { get; set; }

		protected override string ToolName => "nuget";

		protected override string GenerateFullPathToTool () => ToolExe;

		protected override string GenerateCommandLineCommands ()
		{
			var builder = new StringBuilder ();
			void AppendSpace ()
			{
				if (builder.Length > 0 && builder[builder.Length - 1] != ' ') {
					builder.Append (' ');
				}
			}
			void Append(string text)
			{
				AppendSpace ();
				builder.Append (text);
			}
			void AppendQuoted (string text)
			{
				AppendSpace ();
				builder.Append ('"');
				builder.Append (text.Replace ("\"", "\\\"").TrimEnd ('\\'));
				builder.Append ('"');
			}

			Append ("restore");

			AppendQuoted (PackagesConfigFile);

			Append ("-PackagesDirectory");
			AppendQuoted (PackagesDirectory);

			//Append ("-PackageSaveMode");
			//Append ("nuspec;nupkg");

			return builder.ToString ();
		}

		public override bool Execute ()
		{
			Directory.CreateDirectory (Path.GetDirectoryName (PackagesConfigFile));

			using (var f = File.CreateText (PackagesConfigFile)) {
				f.WriteLine ("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
				f.WriteLine ("<packages>");
				foreach (var package in Packages) {
					var version = package.GetMetadata ("Version");
					if (string.IsNullOrEmpty (version)) {
						Log.LogError ($"Package {package.ItemSpec} has no version");
					}
					f.WriteLine ($"<package id=\"{package.ItemSpec}\" version=\"{version}\" />");
				}
				f.WriteLine ("</packages>");
			}

			var success = base.Execute ();

			if (success && !string.IsNullOrEmpty (StampFile)) {
				Directory.CreateDirectory (Path.GetDirectoryName (StampFile));
				File.WriteAllText (StampFile, "");
			} else {
				if (File.Exists (StampFile)) {
					File.Delete (StampFile);
				}
			}

			return success;
		}
	}
}
