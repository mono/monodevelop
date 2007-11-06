//  TemplateCompletionDataProvider.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Drawing;
using System.Reflection;
using System.Collections;

using MonoDevelop.Core.Properties;
using MonoDevelop.Projects;
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
