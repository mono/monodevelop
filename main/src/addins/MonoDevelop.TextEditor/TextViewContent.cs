using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.Ide.Text
{
	class TextViewContent : AbstractXwtViewContent
	{
		TextViewImports imports;
		FilePath fileName;
		string mimeType;
		Project ownerProject;
		Label widget;

		public TextViewContent (TextViewImports imports, FilePath fileName, string mimeType, Project ownerProject)
		{
			this.imports = imports;
			this.fileName = fileName;
			this.mimeType = mimeType;
			this.ownerProject = ownerProject;
			this.widget = new Label ("Test");
		}

		public override Widget Widget => widget;
	}
}