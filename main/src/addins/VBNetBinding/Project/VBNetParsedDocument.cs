//
// CSharpParsedDocument.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.Editor;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;

namespace MonoDevelop.VBNetBinding
{
	class VBNetParsedDocument : ParsedDocument
	{
		static string[] tagComments;

		internal DocumentId DocumentId {
			get;
			set;
		}

		static VBNetParsedDocument ()
		{
			UpdateTags ();
			MonoDevelop.Ide.Tasks.CommentTag.SpecialCommentTagsChanged += delegate {
				UpdateTags ();
			};
		}

		static void UpdateTags ()
		{
			tagComments = MonoDevelop.Ide.Tasks.CommentTag.SpecialCommentTags.Select (t => t.Tag).ToArray ();
		}
		bool isAdHocProject;

		public VBNetParsedDocument (Ide.TypeSystem.ParseOptions options,  string fileName) : base (fileName)
		{
			isAdHocProject = options.IsAdhocProject;
			Flags |= ParsedDocumentFlags.SkipFoldings;
		}

		#region implemented abstract members of ParsedDocument

		IReadOnlyList<Comment> comments = new Comment[0];

		public override Task<IReadOnlyList<Comment>> GetCommentsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromResult (comments);
		}

		// Tags are done via Ide.Tasks.CommentTasksProvider.
		public override Task<IReadOnlyList<Tag>> GetTagCommentsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromResult<IReadOnlyList<Tag>> (null);
		}

		static readonly Task<IReadOnlyList<FoldingRegion>> foldings  = Task.FromResult((IReadOnlyList<FoldingRegion>)new FoldingRegion[0]);

		public override Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync (CancellationToken cancellationToken = default (CancellationToken)) => foldings;

		static readonly IReadOnlyList<Error> emptyErrors = Array.Empty<Error> ();

		public override async Task<IReadOnlyList<Error>> GetErrorsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			return emptyErrors;
		}
		#endregion
	}
}
