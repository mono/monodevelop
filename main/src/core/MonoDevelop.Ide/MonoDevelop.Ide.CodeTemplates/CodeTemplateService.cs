//
// CodeTemplateService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.Ide.CodeTemplates
{
	public static class CodeTemplateService
	{
		static string FileName = "MonoDevelop-templates.xml";
		static string Version  = "2.0";
		
		static List<CodeTemplateGroup> groups = new List<CodeTemplateGroup> ();
		
		public static List<CodeTemplateGroup> TemplateGroups {
			get {
				return groups;
			}
			set {
				groups = value;
			}
		}
		
		static CodeTemplateService ()
		{
			try {
				groups = LoadTemplates ();			
			} catch (Exception e) {
				LoggingService.LogError ("CodeTemplateService: Exception while loading templates.", e);
			}
			if (groups == null)
				groups = new List<CodeTemplateGroup> ();
		}
		
		public static CodeTemplateGroup GetTemplateGroupPerFilename (string fileName)
		{
			return GetTemplateGroupPerExtension (Path.GetExtension (fileName));
		}
		
		public static CodeTemplateGroup GetTemplateGroupPerExtension (string extension)
		{
			foreach (CodeTemplateGroup group in groups) {
				if (group.Extensions.Contains (extension)) 
					return group;
			}
			return null;
		}
		
		public static void AddCompletionDataForFileName (string fileName, CompletionDataList list)
		{
			AddCompletionDataForExtension (Path.GetExtension (fileName), list);
		}
		
		public static void AddCompletionDataForExtension (string extension, CompletionDataList list)
		{
			CodeTemplateGroup group = GetTemplateGroupPerExtension (extension);
			if (group == null)
				return;
			foreach (CodeTemplate ct in group.Templates) {
				if (string.IsNullOrEmpty (ct.Shortcut))
					continue;
				list.Remove (ct.Shortcut);
				list.Add (new CompletionData (ct.Shortcut, "md-template", ct.Description));
			}
		}
		
#region I/O
		const string Node             = "CodeTemplates";
		const string VersionAttribute = "version";
		
		static void SaveTemplates (string fileName)
		{
			XmlTextWriter writer = new XmlTextWriter (fileName, System.Text.Encoding.UTF8);
			writer.Settings.Indent = true;
			try {
				writer.WriteStartDocument ();
				writer.WriteStartElement (Node);
				writer.WriteAttributeString (VersionAttribute, Version);
			
				foreach (CodeTemplateGroup group in groups)
					group.Write (writer);
				
				writer.WriteEndElement (); // Node 
			} finally {
				writer.Close ();
			}
		}
		
		public static void SaveTemplates ()
		{
			SaveTemplates (Path.Combine (PropertyService.ConfigPath, FileName));
		}
		
		static List<CodeTemplateGroup> LoadTemplates (string fileName)
		{
			if (!File.Exists (fileName))
				return null;
			List<CodeTemplateGroup> result = new List<CodeTemplateGroup> ();
			XmlReader reader = XmlTextReader.Create (fileName);
			try {
				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.LocalName) {
						case Node:
							string fileVersion = reader.GetAttribute (VersionAttribute);
							if (fileVersion != Version) 
								return null;
							break;
						case CodeTemplateGroup.Node:
							result.Add (CodeTemplateGroup.Read (reader));
							break;
						}
					}
				}
			} finally {
				reader.Close ();
			}
			return result;
		}
		
		static List<CodeTemplateGroup> LoadTemplates ()
		{
			List<CodeTemplateGroup> result = LoadTemplates (Path.Combine (PropertyService.ConfigPath, FileName));
			if (result == null) {
				LoggingService.LogInfo ("CodeTemplateService: No user templates, reading default templates.");
				result = LoadTemplates (Path.Combine (Path.Combine (PropertyService.DataPath, "options"), FileName));
			}
			return result;
		}
#endregion
	}
}
