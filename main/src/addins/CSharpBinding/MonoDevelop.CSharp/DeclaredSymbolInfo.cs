//
// DeclaredSymbolInfo.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.NavigateTo;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.CSharp
{
	class DeclaredSymbolInfoResult : SearchResult
	{
		bool useFullName;
		INavigateToSearchResult result;

		public override SearchResultType SearchResultType { get { return SearchResultType.Type; } }

		public override string File {
			get { return result.NavigableItem.Document.FilePath; }
		}

		public override Xwt.Drawing.Image Icon {
			get {
				return ImageService.GetIcon (result.GetStockIconForNavigableItem(), IconSize.Menu);
			}
		}

		public override int Offset {
			get { return result.NavigableItem.SourceSpan.Start; }
		}

		public override int Length {
			get { return result.NavigableItem.SourceSpan.Length; }
		}

		public override string PlainText {
			get {
				return result.Name;
			}
		}

		public override Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
		{
			return Task.FromResult (new TooltipInformation {
				SignatureMarkup = result.Name,
				SummaryMarkup = result.Summary,
				FooterMarkup = result.AdditionalInformation,
			});
		}

		public override string Description {
			get {
				string loc = GettextCatalog.GetString ("file {0}", File);
				return result.GetDisplayStringForNavigableItem (loc);
			}
		}

		public override string GetMarkupText (bool selected)
		{
			// use tagged markers
			return HighlightMatch (result.Name, match, selected);
		}

		public DeclaredSymbolInfoResult (string match, string matchedString, int rank, INavigateToSearchResult result)  : base (match, matchedString, rank)
		{
			this.result = result;
		}

		public override bool CanActivate {
			get {
				return result.NavigableItem.Document != null;
			}
		}

		public override async void Activate ()
		{
			var filePath = result.NavigableItem.Document.FilePath;
			var offset = result.NavigableItem.SourceSpan.Start;

			var proj = TypeSystemService.GetMonoProject (result.NavigableItem.Document.Project);
			if (proj?.ParentSolution != null) {
				string projectedName;
				int projectedOffset;
				if (TypeSystemService.GetWorkspace (proj.ParentSolution).TryGetOriginalFileFromProjection (filePath, offset, out projectedName, out projectedOffset)) {
					filePath = projectedName;
					offset = projectedOffset;
				}
			}

			await IdeApp.Workbench.OpenDocument (new FileOpenInformation (filePath, proj) {
				Offset = offset
			});
		}
	}
}
