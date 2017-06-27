//
// MonoDevelopTextLoader.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core.Text;
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using System;

namespace MonoDevelop.Ide.TypeSystem
{
	class MonoDevelopTextLoader : TextLoader
	{
		readonly string fileName;

		public MonoDevelopTextLoader (string fileName)
		{
			this.fileName = fileName;
		}

		#region implemented abstract members of TextLoader

		public override Task<TextAndVersion> LoadTextAndVersionAsync (Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			SourceText text;
			if (IdeApp.Workbench?.Documents.Any (doc => doc.IsFile && doc.FileName != null && FilePath.PathComparer.Compare (Path.GetFullPath (doc.FileName), fileName) == 0) == true) {
				var document = IdeApp.Workbench?.Documents.FirstOrDefault (doc => doc.IsFile && doc.FileName != null && FilePath.PathComparer.Compare (Path.GetFullPath (doc.FileName), fileName) == 0);
				text = MonoDevelopSourceText.Create (document.Editor);
			} else {
				try {
					if (File.Exists (fileName)) {
						text = SourceText.From (TextFileUtility.GetText (fileName));
					} else {
						text = SourceText.From ("");
					}
				} catch (Exception e) {
					LoggingService.LogError ($"Failed to get file text for {fileName}", e);
					text = SourceText.From ("");
				}
			}
			return Task.FromResult (TextAndVersion.Create (text, VersionStamp.Create ()));
		}

		#endregion

		public static TextLoader CreateFromText (string text)
		{
			if (text == null)
				throw new System.ArgumentNullException ("text");
			return TextLoader.From (TextAndVersion.Create (SourceText.From (text), VersionStamp.Create ()));
		}
	}
}