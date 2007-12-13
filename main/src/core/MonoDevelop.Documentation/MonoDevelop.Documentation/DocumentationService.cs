// DocumentationService.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//
// Copyright (c) 2004 Todd Berman  <tberman@off.net>
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
//
//

using System;
using System.Xml;

using Monodoc;

using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Documentation;

namespace MonoDevelop.Documentation
{
	internal class DocumentationService : IDocumentationService
	{

		RootTree helpTree;

		public void InitializeService ()
		{
			helpTree = RootTree.LoadTree ();
		}

		public void UnloadService()
		{
		}
		
		public event EventHandler Initialize;
		public event EventHandler Unload;
		
		public RootTree HelpTree {
			get { return helpTree; }
		}

		public XmlDocument GetHelpXml (string type) {
			try {
				return helpTree.GetHelpXml ("T:" + type);
			}
			catch (Exception ex) {
				// If something goes wrong, just report the error
				LoggingService.LogError (ex.ToString ());
				return null;
			}
		}

		public string GetHelpUrl(ILanguageItem languageItem)
		{
			if (languageItem is IClass)
				return "T:" + ((IClass)languageItem).FullyQualifiedName;
				
				if (languageItem is IEvent)
				return "F:" + ((IEvent)languageItem).FullyQualifiedName;
				
			if (languageItem is IField)
				return "F:" + ((IField)languageItem).FullyQualifiedName;
				
			if (languageItem is IIndexer) {
				IIndexer indexer = (IIndexer)languageItem;
				
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				sb.Append ("P:");
				sb.Append (indexer.DeclaringType.FullyQualifiedName);
				sb.Append (".Item(");
				
				for (int i = 0; i < indexer.Parameters.Count; ++i) {
					sb.Append (indexer.Parameters[i].ReturnType.FullyQualifiedName);
					if (i + 1 != indexer.Parameters.Count)
						sb.Append (",");
				}
				
				sb.Append (")");
				return sb.ToString ();
			}
 			
			if (languageItem is IMethod) {
				IMethod m = (IMethod)languageItem;
				
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
			
				if (m.IsConstructor) {
					sb.Append ("C:");
					sb.Append (m.DeclaringType.FullyQualifiedName);
				}
				else {
					sb.Append ("M:");
					sb.Append (m.FullyQualifiedName);
				}				
				
				sb.Append ("(");
				
				for (int i = 0; i < m.Parameters.Count; ++i) {
					sb.Append (m.Parameters[i].ReturnType.FullyQualifiedName);
					if (i + 1 != m.Parameters.Count)
						sb.Append (",");
				}
				
				sb.Append (")");
				return sb.ToString ();
			}
			
			if (languageItem is IProperty)
				return "P:" + ((IProperty)languageItem).FullyQualifiedName;

			if (languageItem is Namespace)
				return "N:" + ((Namespace)languageItem).Name;
			
			return string.Empty;
		}
	}
}
