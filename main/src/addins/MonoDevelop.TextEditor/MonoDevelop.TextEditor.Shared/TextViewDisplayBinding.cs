using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Text
{
	class TextViewDisplayBinding : IViewDisplayBinding
	{
		public string Name => GettextCatalog.GetString ("Source Code Editor");

		public bool CanUseAsDefault => true;

		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			if (fileName == null || !(IsSupportedFileExtension (fileName) || IsSupportedAndroidFileName (fileName, ownerProject))) {
				return false;
			}

			if (fileName != null)
				return DesktopService.GetFileIsText (fileName, mimeType);

			if (!string.IsNullOrEmpty (mimeType))
				return DesktopService.GetMimeTypeIsText (mimeType);

			return false;
		}

		static HashSet<string> supportedFileExtensions = new HashSet<string> (StringComparer.OrdinalIgnoreCase) {
			".cs",
			".html",
			".cshtml",
			".css",
			".json",
			".js",
			".ts"
		};

		bool IsSupportedFileExtension (FilePath fileName)
		{
			return supportedFileExtensions.Contains (fileName.Extension);
		}

		bool IsSupportedAndroidFileName (FilePath fileName, Project ownerProject)
		{
			// We only care about .xml and .axml files that are marked as AndroidResource
			if (!(fileName.HasExtension (".xml") || fileName.HasExtension (".axml")))
				return false;

			const string AndroidResourceBuildAction = "AndroidResource";
			var buildAction = ownerProject.GetProjectFile (fileName)?.BuildAction;
			return string.Equals (buildAction, AndroidResourceBuildAction, System.StringComparison.Ordinal);
		}

		public ViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject)
		{
			var imports = CompositionManager.GetExportedValue<TextViewImports> ();
			var viewContent = new TextViewContent (imports, fileName, mimeType, ownerProject);
			return viewContent;
		}
	}
}