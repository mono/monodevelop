// 
// LanguageBindingService.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.CodeGeneration;


namespace MonoDevelop.Projects
{
	public static class LanguageBindingService
	{
		static List<LanguageBindingCodon> languageBindingCodons = new List<LanguageBindingCodon> ();
		
		static LanguageBindingService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/LanguageBindings", delegate(object sender, ExtensionNodeEventArgs args) {
				LanguageBindingCodon languageBindingCodon = (LanguageBindingCodon)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					languageBindingCodons.Add (languageBindingCodon);
					IDotNetLanguageBinding dotNetBinding = languageBindingCodon as IDotNetLanguageBinding;
					if (dotNetBinding != null) {
						object par = dotNetBinding.CreateCompilationParameters (null);
						if (par != null)
							Services.ProjectService.DataContext.IncludeType (par.GetType ());
						par = dotNetBinding.CreateProjectParameters (null);
						if (par != null)
							Services.ProjectService.DataContext.IncludeType (par.GetType ());
					}
					break;
				case ExtensionChange.Remove:
					languageBindingCodons.Remove (languageBindingCodon);
					break;
				}
				languageBindings = null;
			});
		}
		
		static List<ILanguageBinding> languageBindings = null;
		public static IEnumerable<ILanguageBinding> LanguageBindings {
			get {
				CheckBindings ();
				return languageBindings;
			}
		}
		
		static void CheckBindings ()
		{
			if (languageBindings == null)
				languageBindings = new List<ILanguageBinding> (from codon in languageBindingCodons select codon.LanguageBinding);
		}
		
		public static ILanguageBinding GetBindingPerFileName (string fileName)
		{
			if (String.IsNullOrEmpty (fileName)) {
				MonoDevelop.Core.LoggingService.LogWarning ("Cannot get binding for null filename at {0}", Environment.StackTrace);
				return null;
			}
			CheckBindings ();
			return languageBindings.FirstOrDefault (binding => binding.IsSourceCodeFile (fileName));
		}
		
		public static ILanguageBinding GetBindingPerLanguageName (string language)
		{
			if (String.IsNullOrEmpty (language)) {
				MonoDevelop.Core.LoggingService.LogWarning ("Cannot get binding for null language at {0}", Environment.StackTrace);
				return null;
			}
			CheckBindings ();
			return languageBindings.FirstOrDefault (binding => binding.Language == language);
		}
		
		public static IRefactorer GetRefactorerForFile (string fileName)
		{
			ILanguageBinding binding = GetBindingPerFileName (fileName);
			return binding != null ? binding.Refactorer : null;
		}
		
		public static IRefactorer GetRefactorerForLanguage (string language)
		{
			ILanguageBinding binding = GetBindingPerLanguageName (language);
			return binding != null ? binding.Refactorer : null;
		}
	}
}
