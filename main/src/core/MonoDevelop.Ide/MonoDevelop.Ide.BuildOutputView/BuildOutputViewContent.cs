//
// BuildOutputView.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutputViewContent : ViewContent
	{
		string defaultName = "build ({0}).binlog";
		bool isTemp;

		public BuildOutputWidget Widget { get; }

		public BuildOutputViewContent (FilePath filename)
		{
			this.ContentName = filename;
			Widget = new BuildOutputWidget (filename);
		}

		public BuildOutputViewContent (BuildOutput buildOutput)
		{
			Widget = new BuildOutputWidget (buildOutput);
			//TODO: is there a better way to identify if the buildOutpu.FilePath is tmp or not? 
			isTemp = Path.GetDirectoryName (buildOutput.FilePath).TrimEnd ('/') == Path.GetTempPath ().TrimEnd ('/');
			this.ContentName = isTemp ? string.Format (defaultName, 1) : buildOutput.FilePath;
			IsDirty = isTemp;
		}

		public override Task Save (FileSaveInformation fileSaveInformation)
		{
			var result = false;

			if (File.Exists (fileSaveInformation.FileName))
				File.Delete (fileSaveInformation.FileName); //TODO: backup before removing?

			File.Copy (Widget.BuildOutput.FilePath, fileSaveInformation.FileName);

			result = File.Exists (fileSaveInformation.FileName);
			if (result) {
				Widget.BuildOutput.UpdateFilePath (fileSaveInformation.FileName);
				this.IsDirty = isTemp = false;
			}

			return Task.FromResult (result);
		}

		public override bool IsReadOnly {
			get {
				return false;
			}
		}

		public override bool IsFile {
			get {
				//if isTemp = true, the ContentName contains default naming
				return  isTemp ? true : File.Exists (ContentName);
			}
		}

		public override bool IsViewOnly {
			get {
				return false;
			}
		}

		public override string TabPageLabel {
			get {
				return ContentName ?? GettextCatalog.GetString ("Build Output");
			}
		}

		public override Control Control {
			get {
				return Widget;
			}
		}

		public override void Dispose ()
		{
			Widget.Dispose ();
			base.Dispose ();
		}

		internal async Task UpdateContent ()
		{
			if (ContentName != Widget.BuildOutput.FilePath) {
				await Save (new FileSaveInformation (ContentName, null));
			}
		}
	}
}
