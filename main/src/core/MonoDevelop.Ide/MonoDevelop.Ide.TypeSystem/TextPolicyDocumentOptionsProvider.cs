//
// TextPolicyDocumentOptionsProvider.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2017 Microsoft Corp.
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
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.Gui.Content;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace MonoDevelop.Ide.TypeSystem
{
	[Export (typeof (IDocumentOptionsProviderFactory))]
	class TextPolicyDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
	{
		public IDocumentOptionsProvider Create (Workspace workspace)
		{
			return new TextPolicyDocumentOptionsProvider ();
		}
	}

	class TextPolicyDocumentOptionsProvider : IDocumentOptionsProvider
	{
		public async Task<IDocumentOptions> GetOptionsForDocumentAsync (Document document, CancellationToken cancellationToken)
		{
			var mimeChain = DesktopService.GetMimeTypeInheritanceChainForRoslynLanguage (document.Project.Language);
			if (mimeChain == null) {
				return null;
			}

			var project = TypeSystemService.GetMonoProject (document.Project);
			var policy = project.Policies.Get<TextStylePolicy> (mimeChain);
			return new TextDocumentOptions (policy);
		}

		class TextDocumentOptions : IDocumentOptions
		{
			TextStylePolicy policy;

			public TextDocumentOptions (TextStylePolicy policy)
			{
				this.policy = policy;
			}

			public bool TryGetDocumentOption (Document document, OptionKey option, out object value)
			{
				if (option.Option == FormattingOptions.UseTabs) {
					value = !policy.TabsToSpaces;
					return true;
				}

				if (option.Option == FormattingOptions.TabSize) {
					value = policy.TabWidth;
					return true;
				}

				if (option.Option == FormattingOptions.IndentationSize) {
					value = policy.IndentWidth;
					return true;
				}

				if (option.Option == FormattingOptions.NewLine) {
					value = policy.GetEolMarker ();
					return true;
				}

				value = null;
				return false;
			}
		}
	}
}
