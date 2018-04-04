//
// DocumentContextExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis.Options;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp
{
	static class DocumentContextExtension
	{
		public static async Task<OptionSet> GetOptionsAsync (this DocumentContext ctx, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (ctx == null)
				throw new ArgumentNullException (nameof (ctx));
			try {
				if (ctx.AnalysisDocument != null) {
					var result = await ctx.AnalysisDocument.GetOptionsAsync ().ConfigureAwait (false);
					if (result != null)
						return result;
				}
				var policies = ctx.Project?.Policies;
				if (policies == null) {
					var defaultPolicy = PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (CSharpFormatter.MimeType);
					var defaultTextPolicy = PolicyService.GetDefaultPolicy<TextStylePolicy> (CSharpFormatter.MimeType);
					return defaultPolicy.CreateOptions (defaultTextPolicy);
				}
				var policy = policies.Get<CSharpFormattingPolicy> (CSharpFormatter.MimeType);
				var textpolicy = policies.Get<TextStylePolicy> (CSharpFormatter.MimeType);
				return policy.CreateOptions (textpolicy);
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting document options", e);
				return ctx.GetOptionSet ();
			}
		}
	}
}
