//
// FileTemplateCondition.cs
//
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;

namespace MonoDevelop.Ide.Templates
{	
	
	public abstract class FileTemplateCondition
	{
		
		static List<FileTemplateConditionTypeCodon> conditions;
		
		public static FileTemplateCondition CreateCondition (XmlElement element)
		{
			if (conditions == null) {
				conditions = new List<FileTemplateConditionTypeCodon> ();
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/FileTemplateConditionTypes", OnExtensionChanged);
			}
			
			foreach (FileTemplateConditionTypeCodon condition in conditions) {
				if (condition.ElementName == element.Name) {
					FileTemplateCondition t = (FileTemplateCondition) condition.CreateInstance (typeof(FileTemplateCondition));
					t.Load (element);
					return t;
				}
			}
			throw new InvalidOperationException ("Unknown file template condition type: " + element.Name);
		}
		
		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				conditions.Add ((FileTemplateConditionTypeCodon) args.ExtensionNode);
			else
				conditions.Remove ((FileTemplateConditionTypeCodon) args.ExtensionNode);
		}
		
		public abstract void Load (XmlElement element);
		
		//restricts whether the template should be shown for a given path within a given project 
		public virtual bool ShouldEnableFor (Project proj, string projectPath)
		{
			return true;
		}
		
		//restricts whether a given language should be available for a given project
		//called after ShouldEnableFor (Project proj)
		public virtual bool ShouldEnableFor (Project proj, string projectPath, string language)
		{
			return true;
		}
	}
}
