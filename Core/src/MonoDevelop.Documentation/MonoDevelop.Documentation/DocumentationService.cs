using System;
using System.Xml;

using Monodoc;

using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Documentation;

namespace MonoDevelop.Documentation
{

	public class DocumentationService : IDocumentationService
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
