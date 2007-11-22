//
// StandardHeaderService.cs
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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.StandardHeaders
{
	public static class StandardHeaderService
	{
		const string version          = "1.1";
		const string templateFileName = "StandardHeader.xml";
		
		static string header;
		static bool   generateComments = true;
		static bool   emitStandardHeader = true;
		
		static List<KeyValuePair<string, string>> headerTemplates = new List<KeyValuePair<string, string>> ();
		static List<KeyValuePair<string, string>> customTemplates = new List<KeyValuePair<string, string>> ();
		
		public static ReadOnlyCollection<KeyValuePair<string, string>> HeaderTemplates {
			get {
				return headerTemplates.AsReadOnly ();
			}
		}
		
		public static ReadOnlyCollection<KeyValuePair<string, string>> CustomTemplates {
			get {
				return customTemplates.AsReadOnly ();
			}
		}
		
		public static string Header {
			get { 
				return header; 
			}
			set {
				if (header != value) {
					header = value;
				}
			}
		}
		
		public static bool GenerateComments {
			get {
				return generateComments;
			}
			set {
				if (generateComments != value) {
					generateComments = value;
				}
			}
		}
		
		public static bool EmitStandardHeader {
			get {
				return emitStandardHeader;
			}
			set {
				if (emitStandardHeader != value) {
					emitStandardHeader = value;
				}
			}
		}
		
		
		
		static string ConfigLocation {
			get {
				return Path.Combine (PropertyService.ConfigPath, templateFileName);
			}
		}
		
		static string GetComment (string language)
		{
			LanguageBindingService languageBindingService = MonoDevelop.Projects.Services.Languages;
			ILanguageBinding binding = languageBindingService.GetBindingPerLanguageName (language);
			if (binding != null)
				return binding.CommentTag;
			return null;
		}
		
		public static string GetHeader (string language, string fileName)
		{
			if (String.IsNullOrEmpty (Header) || GetComment (language) == null) {
				return "";
			}
			StringBuilder result = new StringBuilder ();
			
			char ch = Environment.NewLine.ToCharArray () [0];
			string[] lines = Header.Split (ch);
			foreach (string line in lines) {
				if (generateComments)
					result.Append (GetComment (language));
				result.Append (line);
				result.Append (Environment.NewLine);
			}
			
			return StringParserService.Parse (result.ToString(), new string[,] { 
				{ "FileName", Path.GetFileName (fileName) }, 
				{ "FileNameWithoutExtension", Path.GetFileNameWithoutExtension (fileName) }, 
				{ "Directory", Path.GetDirectoryName (fileName) }, 
				{ "FullFileName", fileName },
			
			});
		}
		
		static void LoadHeaderTemplates ()
		{
			Stream stream = typeof (StandardHeaderService).Assembly.GetManifestResourceStream ("StandardHeaderTemplates.xml");
			if (stream != null) {
				XmlTextReader reader = new XmlTextReader (stream);
				try {
					while (reader.Read ()) {
						if (reader.IsStartElement ()) {
							switch (reader.LocalName) {
							case HeaderNode:
								string name   = reader.GetAttribute (NameAttribute);
								string header = reader.ReadString ();
								headerTemplates.Add (new KeyValuePair<string, string> (name, header));
								break;
							}
						}
					}
				} finally {
					reader.Close ();
				}
			}
		}
		
		static StandardHeaderService ()
		{
			try {
				LoadHeaderTemplates ();
				
				if (File.Exists (ConfigLocation)) {
					if (Load (ConfigLocation))
						return;
				}
				string file = Path.Combine (Path.Combine (PropertyService.DataPath, "options"), templateFileName);
				if (File.Exists (file))
					Load (file);
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}
		
		public static void RemoveTemplate (string name)
		{
			for (int i = 0; i < customTemplates.Count; ++i) {
				Console.WriteLine (customTemplates[i].Key + " -- " + name + " : " + (customTemplates[i].Key == name));
				if (customTemplates[i].Key == name) {
					customTemplates.RemoveAt (i);
					i--;
					continue;
				}
			}
		}
		
		public static void AddTemplate (string name, string header)
		{
			customTemplates.Add (new KeyValuePair<string, string> (name, header));
		}
		
		public static void CommitChanges ()
		{
			Save (ConfigLocation);
		}
		
#region I/O
		const string Node             = "StandardHeader";
		const string HeaderNode       = "Header";
		const string NameAttribute    = "_name";
		const string VersionAttribute = "version";
		const string GenerateCommentsAttribute = "generateComments";
		const string EmitStandardHeaderAttribute = "emitStandardHeader";
		
		static bool Load (string fileName)
		{
			customTemplates.Clear ();
			XmlReader reader = null;
			try {
				reader = XmlTextReader.Create (fileName);
				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.LocalName) {
						case Node:
							if (!String.IsNullOrEmpty (reader.GetAttribute (GenerateCommentsAttribute)))
								generateComments = Boolean.Parse (reader.GetAttribute (GenerateCommentsAttribute));
							
							if (!String.IsNullOrEmpty (reader.GetAttribute (EmitStandardHeaderAttribute)))
								emitStandardHeader = Boolean.Parse (reader.GetAttribute (EmitStandardHeaderAttribute));
							
							string fileVersion = reader.GetAttribute (VersionAttribute);
							if (fileVersion != version) 
								return false;
							break;
						case HeaderNode:
							string name       = reader.GetAttribute (NameAttribute);
							string headerText = reader.ReadString ();
							
							if (String.IsNullOrEmpty (name)) {
								// Default header
								header = headerText;
							} else {
								customTemplates.Add (new KeyValuePair<string, string> (name, headerText));
							}
							
							break;
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError (e.ToString ());
			} finally {
				if (reader != null)
					reader.Close ();
			}
			return true;
		}
		
		static void Save (string fileName)
		{
			Stream stream = new FileStream (fileName, FileMode.Create);
			XmlWriter writer = new XmlTextWriter (stream, Encoding.UTF8);
			try {
				writer.Settings.Indent = true;
				writer.WriteStartElement (Node);
				writer.WriteAttributeString (VersionAttribute, version);
				writer.WriteAttributeString (GenerateCommentsAttribute, generateComments.ToString ());
				writer.WriteAttributeString (EmitStandardHeaderAttribute, emitStandardHeader.ToString ());
				
				
				writer.WriteStartElement (HeaderNode);
				writer.WriteString (header);
				writer.WriteEndElement (); // HeaderNode
				
				foreach (KeyValuePair<string, string> template in customTemplates) { 
					writer.WriteStartElement (HeaderNode);
					writer.WriteAttributeString (NameAttribute, template.Key);
					writer.WriteString (template.Value);
					writer.WriteEndElement (); // HeaderNode
				}
				
				writer.WriteEndElement (); // Node
			} finally {
				writer.Close ();
				stream.Close ();
			}
		}
#endregion
	}
}
