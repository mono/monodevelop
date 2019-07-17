// 
// Scope.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using System.Threading;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.FindInFiles
{

	abstract partial class Scope
	{
		sealed class DocumentScope : Scope
		{
			public override PathMode PathMode {
				get { return PathMode.Hidden; }
			}

			public override Task<IReadOnlyList<FileProvider>> GetFilesAsync (FindInFilesModel filterOptions, CancellationToken cancellationToken = default)
			{
				var doc = IdeApp.Workbench.ActiveDocument;
				var textBuffer = doc.GetContent<ITextBuffer> ();
				if (textBuffer != null)
					return Task.FromResult<IReadOnlyList<FileProvider>> (ImmutableArray.Create (new OpenFileProvider (textBuffer, doc.Owner as Project, doc.FileName)));
				return EmptyFileProviderTask;
			}

			public override string GetDescription (FindInFilesModel model)
			{
				return model.InReplaceMode
					? GettextCatalog.GetString ("Replacing '{0}' in current document", model.FindPattern)
					: GettextCatalog.GetString ("Looking for '{0}' in current document", model.FindPattern);
			}

		}
	}
}