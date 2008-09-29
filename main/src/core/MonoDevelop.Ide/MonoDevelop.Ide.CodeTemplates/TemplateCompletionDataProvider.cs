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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using Stock = MonoDevelop.Core.Gui.Stock;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.CodeTemplates;

namespace MonoDevelop.Ide.CodeTemplates
{
	public class TemplateCompletionDataProvider : ICompletionDataProvider
	{
		string fileName;
		
		public TemplateCompletionDataProvider (string fileName)
		{
			this.fileName = fileName;
		}
		
		public Gdk.Pixbuf[] ImageList
		{
			get {
				return null;
			}
		}
		
		public string DefaultCompletionString {
			get { return null; }
		}
		bool autoCompleteUniqueMatch = false;
		public bool AutoCompleteUniqueMatch {
			get {
				return autoCompleteUniqueMatch;
			}
			set {
				autoCompleteUniqueMatch = value;
			}
		}
		
		public ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			CodeTemplateGroup templateGroup = CodeTemplateService.GetTemplateGroupPerFilename (fileName);
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
			
			public string Image
			{
				get {
					return Stock.Method;
				}
			}

			public string CompletionString
			{
				get 
				{
					return "";
				}
			}
			
			public string[] Text
			{
				get {
					return new string[] { template.Shortcut, template.Description };
				}
			}
			
			public string Description
			{
				get {
					return template.Text;
				}
			}
			
			public void InsertAction(ICompletionWidget widget)
			{
				//((SharpDevelopTextAreaControl)control).InsertTemplate(template);
			}
			
			public TemplateCompletionData(CodeTemplate template) 
			{
				this.template = template;
			}
			public virtual int CompareTo (ICompletionData x)
			{
				return String.Compare (Text[0], x.Text[0], true);
			}
		}
		
		public void Dispose ()
		{
		}
	}
}
