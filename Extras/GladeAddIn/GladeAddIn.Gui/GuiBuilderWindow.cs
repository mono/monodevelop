//
// GuiBuilderWindow.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Reflection;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Text;

namespace GladeAddIn.Gui
{
	public class GuiBuilderWindow: IDisposable
	{
		Gladeui.Widget rootWidget;
		Gladeui.Project gproject;
		GuiBuilderProject fproject;
		
		public event WindowEventHandler NameChanged;
		
		internal GuiBuilderWindow (GuiBuilderProject fproject, Gladeui.Project gproject, Gladeui.Widget rootWidget)
		{
			this.fproject = fproject;
			this.rootWidget = rootWidget;
			this.gproject = gproject;
			gproject.WidgetNameChangedEvent += new Gladeui.WidgetNameChangedEventHandler (OnWidgetNameChanged);
		}
		
		public Gladeui.Widget RootWidget {
			get { return rootWidget; }
		}
		
		public GuiBuilderProject Project {
			get { return fproject; }
		}
		
		public string Name {
			get { return rootWidget.Name; }
		}
		
		public string SourceCodeFile {
			get {
				IClass cls = GetClass ();
				if (cls != null) return cls.Region.FileName;
				else return null;
			}
		}
		
		public GuiBuilderEditSession CreateEditSession (ITextFileProvider textFileProvider)
		{
			return new GuiBuilderEditSession (this, textFileProvider);
		}
		
		internal void SetWidgetInfo (Gladeui.WidgetInfo winfo)
		{
			gproject.RemoveObject (rootWidget.Object);
			rootWidget = Gladeui.Widget.Read (gproject, winfo);
			gproject.AddObject (rootWidget.Object);
		}
		
		public void Dispose ()
		{
			gproject.WidgetNameChangedEvent -= new Gladeui.WidgetNameChangedEventHandler (OnWidgetNameChanged);
		}
		
		internal void Save ()
		{
			gproject.Save (gproject.Path);
		}
		
		void OnWidgetNameChanged (object s, Gladeui.WidgetNameChangedEventArgs args)
		{
			if (!InsideWindow (args.Widget))
				return;
			
			if (args.Widget == rootWidget && NameChanged != null)
				NameChanged (this, new WindowEventArgs (this));
		}
		
		public IClass GetClass ()
		{
			return GetClass (rootWidget.Name);
		}
		
		internal IClass GetClass (string name)
		{
			IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (fproject.Project);
			IClass[] classes = ctx.GetProjectContents ();
			foreach (IClass cls in classes) {
				if (ClassUtils.FindWidgetField (cls, name) != null)
					return cls;
			}
			return null;
		}
		
		public bool InsideWindow (Gladeui.Widget widget)
		{
			while (widget.Parent != null)
				widget = widget.Parent;
			return widget == rootWidget;
		}
	}
	
	class OpenDocumentFileProvider: ITextFileProvider
	{
		public IEditableTextFile GetEditableTextFile (string filePath)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == filePath) {
					IEditableTextFile ef = doc.Content as IEditableTextFile;
					if (ef != null) return ef;
				}
			}
			return null;
		}
	}
}
