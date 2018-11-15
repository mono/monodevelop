using System;
using System.Windows.Controls;
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
		RootWpfWidget widget;
		Xwt.Widget xwtWidget;

		public TextViewContent (TextViewImports imports, FilePath fileName, string mimeType, Project ownerProject)
		{
			this.imports = imports;
			this.fileName = fileName;
			this.mimeType = mimeType;
			this.ownerProject = ownerProject;

			var control = CreateControl (imports);
			this.widget = new RootWpfWidget (control);
			widget.HeightRequest = 50;
			widget.WidthRequest = 100;
			//this.xwtWidget = new Xwt.Label ("FOO");
			this.xwtWidget = GetXwtWidget (widget);
			xwtWidget.Show ();
			ContentName = fileName;
		}

		private Widget GetXwtWidget (RootWpfWidget widget)
		{
			return Xwt.Toolkit.CurrentEngine.WrapWidget (widget, NativeWidgetSizing.External);
		}

		private System.Windows.Controls.Control CreateControl (TextViewImports imports)
		{
			var textBuffer = imports.TextBufferFactoryService.CreateTextBuffer (@"WPF RUleZ", imports.TextBufferFactoryService.PlaintextContentType);
			var textView = imports.TextEditorFactoryService.CreateTextView (textBuffer);
			var textViewHost = imports.TextEditorFactoryService.CreateTextViewHost (textView, setFocus: true);
			return textViewHost.HostControl;
		}

		public override Widget Widget => xwtWidget;
	}
}