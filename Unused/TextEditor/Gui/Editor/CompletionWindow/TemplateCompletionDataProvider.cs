// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Reflection;
using System.Collections;

using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Internal.Project;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Gui.CompletionWindow;

namespace MonoDevelop.DefaultEditor.Gui.Editor
{
	public class TemplateCompletionDataProvider : ICompletionDataProvider
	{
		public Gdk.Pixbuf[] ImageList {
			get {
				return null;
			}
		}
		
		public ICompletionData[] GenerateCompletionData(IProject project, string fileName, TextArea textArea, char charTyped)
		{
			CodeTemplateGroup templateGroup = CodeTemplateLoader.GetTemplateGroupPerFilename(fileName);
			if (templateGroup == null) {
				return null;
			}
			ArrayList completionData = new ArrayList();
			foreach (CodeTemplate template in templateGroup.Templates) {
				completionData.Add(new TemplateCompletionData(template));
			}
			
			return (ICompletionData[])completionData.ToArray(typeof(ICompletionData));
		}
		
		class TemplateCompletionData : ICompletionData
		{
			CodeTemplate template;
			
			public int ImageIndex {
				get {
					return 0;
				}
			}
			
			public string[] Text {
				get {
					return new string[] { template.Shortcut, template.Description };
				}
			}
			
			public string Description {
				get {
					return template.Text;
				}
			}
			
			public void InsertAction(TextEditorControl control)
			{
				((SharpDevelopTextAreaControl)control).InsertTemplate(template);
			}
			
			public TemplateCompletionData(CodeTemplate template) 
			{
				this.template = template;
			}
		}
	}
}
