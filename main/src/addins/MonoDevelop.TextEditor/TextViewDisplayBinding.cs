using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Text
{
	class TextViewDisplayBinding : IViewDisplayBinding
	{
		public string Name => "Text View";

		public bool CanUseAsDefault => true;

		public bool CanHandle (FilePath fileName, string mimeType, Project ownerProject)
		{
			if (fileName == null || !fileName.HasExtension(".cs")) {
				return false;
			}

			if (fileName != null)
				return DesktopService.GetFileIsText (fileName, mimeType);

			if (!string.IsNullOrEmpty (mimeType))
				return DesktopService.GetMimeTypeIsText (mimeType);

			return false;
		}

		public ViewContent CreateContent (FilePath fileName, string mimeType, Project ownerProject)
		{
			var imports = CompositionManager.GetExportedValue<TextViewImports> ();
			var viewContent = new TextViewContent (imports, fileName, mimeType, ownerProject);
			return viewContent;
		}
	}
}