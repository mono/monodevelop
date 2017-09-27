//
// CSharpSnippetInfoService.cs
//
// Author:
//       mkrueger <>
//
// Copyright (c) 2017 ${CopyrightHolder}
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
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Roslyn.Utilities;
using MonoDevelop.Ide.CodeTemplates;

namespace MonoDevelop.CSharp.Refactoring
{
	[ExportLanguageService (typeof (ISnippetInfoService), LanguageNames.CSharp), Shared]
	class CSharpSnippetInfoService : ISnippetInfoService
	{
		IEnumerable<SnippetInfo> ISnippetInfoService.GetSnippetsIfAvailable ()
		{
			foreach (var template in CodeTemplateService.GetCodeTemplates (CSharp.Formatting.CSharpFormatter.MimeType)) {
				yield return new SnippetInfo (template.Shortcut, template.Shortcut, template.Description, template.Group);
			}
		}

		bool ISnippetInfoService.ShouldFormatSnippet (SnippetInfo snippetInfo)
		{
			return true;
		}

		bool ISnippetInfoService.SnippetShortcutExists_NonBlocking (string shortcut)
		{
			foreach (var template in CodeTemplateService.GetCodeTemplates (CSharp.Formatting.CSharpFormatter.MimeType)) {
				if (template.Shortcut == shortcut)
					return true;
			}
			return false;
		}
	}
}
