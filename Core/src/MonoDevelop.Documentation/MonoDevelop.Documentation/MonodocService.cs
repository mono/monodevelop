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
			return helpTree.GetHelpXml ("T:" + type);
		}

		public string GetHelpUrl(ILanguageItem languageItem)
		{
			if (languageItem is IClass)
				return "T:" + ((IClass)languageItem).FullyQualifiedName;
			
			if (languageItem is IProperty)
				return "P:" + ((IProperty)languageItem).FullyQualifiedName;

			if (languageItem is IField)
				return "F:" + ((IField)languageItem).FullyQualifiedName;

			if (languageItem is Namespace)
				return "N:" + ((Namespace)languageItem).Name;
			
			return string.Empty;
		}
	}
}
