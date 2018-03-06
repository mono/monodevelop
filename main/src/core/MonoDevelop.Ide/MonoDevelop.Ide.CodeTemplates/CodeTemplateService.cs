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
using MonoDevelop.Ide.CodeCompletion;
using Mono.Addins;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Text;
using MonoDevelop.Ide.Editor.TextMate;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.CodeTemplates
{
	public static class CodeTemplateService
	{
		static string Version  = "3.0";
		
		static List<CodeTemplate> templates;
		
		public static List<CodeTemplate> Templates {
			get {
				return templates;
			}
			set {
				templates = value ?? new List<CodeTemplate> ();
				OnTemplatesChanged (EventArgs.Empty);
			}
		}

		public static event EventHandler TemplatesChanged;

		static void OnTemplatesChanged (EventArgs e)
		{
			var handler = TemplatesChanged;
			if (handler != null)
				handler (null, e);
		}
		
		static CodeTemplateService ()
		{
			try {
				Templates = LoadTemplates ();
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/CodeTemplates", delegate (object sender, ExtensionNodeEventArgs args) {
					var codon = (CodeTemplateCodon)args.ExtensionNode;
					switch (args.Change) {
					case ExtensionChange.Add:
						using (XmlReader reader = codon.Open ()) {
							LoadTemplates (reader).ForEach (t => templates.Add (t));
						}
						break;
					}
				});
			} catch (Exception e) {
				LoggingService.LogError ("CodeTemplateService: Exception while loading templates.", e);
			}
		}
		
		public static IEnumerable<CodeTemplate> GetCodeTemplates (string mimeType)
		{
			var savedTemplates = templates;
			if (savedTemplates == null || string.IsNullOrEmpty (mimeType))
				return new CodeTemplate [0];
			return savedTemplates.ToArray ().Where (delegate (CodeTemplate t) {
				try {
					return t != null && DesktopService.GetMimeTypeIsSubtype (mimeType, t.MimeType);
				} catch (Exception) {
					// required for some unit tests
					return t != null && mimeType == t.MimeType;
				}	
			});
		}

		public static async Task<IEnumerable<CodeTemplate>> GetCodeTemplatesAsync (TextEditor editor, CancellationToken cancellationToken = default(CancellationToken))
		{
			var result = new List<CodeTemplate> ();
			result.AddRange (GetCodeTemplates (editor.MimeType));
			var scope = await editor.SyntaxHighlighting.GetScopeStackAsync (editor.CaretOffset, cancellationToken);
			foreach (var setting in TextMateLanguage.Create (scope).Snippets) {
				var convertedTemplate = ConvertToTemplate (setting);
				if (convertedTemplate != null)
					result.Add (convertedTemplate);
			}
			return result;
		}

		static CodeTemplate ConvertToTemplate (TmSnippet setting)
		{
			var result = new CodeTemplate ();
			result.Shortcut = setting.TabTrigger;
			var sb = StringBuilderCache.Allocate ();
			var nameBuilder = StringBuilderCache.Allocate ();
			bool readDollar = false;
			bool inBracketExpression = false;
			bool inExpressionContent = false;
			bool inVariable = false;
			int number = 0;
			foreach (var ch in setting.Content) {
				if (inVariable) {
					if (char.IsLetter (ch)) {
						nameBuilder.Append (ch);
					} else {
						sb.Append (ConvertVariable (nameBuilder.ToString ()));
						nameBuilder.Length = 0;
						inVariable = false;
					}
				}

				if (ch == '$') {
					readDollar = true;
					continue;
				}
				if (readDollar) {
					if (ch == '{') {
						number = 0;
						inBracketExpression = true;
						readDollar = false;
						continue;
					} else if (char.IsLetter (ch)) {
						inVariable = true;
					} else {
						sb.Append ("$$");
						readDollar = false;
					}	
				}
				if (inBracketExpression) {
					if (ch == ':') {
						inBracketExpression = false;
						inExpressionContent = true;
						continue;
					}
					number = number * 10 + (ch - '0');
					continue;
				}


				if (inExpressionContent) {
					if (ch == '}') {
						if (number == 0) {
							sb.Append ("$end$");
							sb.Append (nameBuilder);
						} else {
							sb.Append ("$");
							sb.Append (nameBuilder);
							sb.Append ("$");
							result.AddVariable (new CodeTemplateVariable (nameBuilder.ToString ()) { Default = nameBuilder.ToString (), IsEditable = true });
						}
						nameBuilder.Length = 0;
						number = 0;
						inExpressionContent = false;
						continue;
					}
					nameBuilder.Append (ch);
					continue;
				}
				sb.Append (ch);
			}
			if (inVariable) {
				sb.Append (ConvertVariable (nameBuilder.ToString ()));
				nameBuilder.Length = 0;
				inVariable = false;
			}
			StringBuilderCache.Free (nameBuilder);
			result.Code = StringBuilderCache.ReturnAndFree (sb);
			result.CodeTemplateContext = CodeTemplateContext.Standard;
			result.CodeTemplateType = CodeTemplateType.Expansion;
			result.Description = setting.Name;
			return result;
		}

		static string ConvertVariable (string textmateVariable)
		{
			switch (textmateVariable) {
			case "SELECTION":
			case "TM_SELECTED_TEXT":
				return "$selected$";
			case "TM_CURRENT_LINE":
			case "TM_CURRENT_WORD":
			case "TM_FILENAME":
			case "TM_FILEPATH":
			case "TM_FULLNAME":
			case "TM_LINE_INDEX":
			case "TM_LINE_NUMBER":
			case "TM_SOFT_TABS":
			case "TM_TAB_SIZE":
				return "$" + textmateVariable + "$";
			}
			return "";
		}

		public static IEnumerable<CodeTemplate> GetCodeTemplatesForFile (string fileName)
		{
			return GetCodeTemplates (DesktopService.GetMimeTypeForUri (fileName));
		}
		
		public static void AddCompletionDataForFileName (string fileName, CompletionDataList list)
		{
			AddCompletionDataForMime (DesktopService.GetMimeTypeForUri (fileName), list);
		}
		
		public static void AddCompletionDataForMime (string mimeType, CompletionDataList list)
		{
			foreach (CodeTemplate ct in GetCodeTemplates (mimeType)) {
				if (string.IsNullOrEmpty (ct.Shortcut) || ct.CodeTemplateContext != CodeTemplateContext.Standard)
					continue;
				list.Remove (ct.Shortcut);
				list.Add (new CompletionData (ct.Shortcut, ct.Icon , ct.Shortcut + Environment.NewLine + GettextCatalog.GetString (ct.Description)));
			}
		}
		
		public static ExpansionObject GetExpansionObject (CodeTemplate template)
		{
			// TODO: Add more expansion objects.
			return new ExpansionObject ();
		}
		
#region I/O
		const string Node             = "CodeTemplates";
		const string VersionAttribute = "version";
		
		static void SaveTemplate (CodeTemplate template, string fileName)
		{
			XmlTextWriter writer = new XmlTextWriter (fileName, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			
			try {
				writer.WriteStartDocument ();
				writer.WriteStartElement (Node);
				writer.WriteAttributeString (VersionAttribute, Version);
				template.Write (writer);
				writer.WriteEndElement (); // Node 
			} finally {
				writer.Close ();
			}
		}

		public static void DeleteTemplate (CodeTemplate template)
		{
			try {
				var fileName = Path.Combine (TemplatePath, template.Shortcut + ".template.xml");
				if (File.Exists (fileName))
					File.Delete (fileName);
			} catch (Exception e) {
				LoggingService.LogError ("Error while removing template file", e);
			}
		}
		
		public static void SaveTemplate (CodeTemplate template)
		{
			if (!Directory.Exists (TemplatePath))
				Directory.CreateDirectory (TemplatePath);
			SaveTemplate (template, Path.Combine (TemplatePath, template.Shortcut + ".template.xml"));
		}
		/*
		public static void SaveTemplates ()
		{
			if (!Directory.Exists (TemplatePath))
				Directory.CreateDirectory (TemplatePath);
			foreach (string templateFile in Directory.GetFiles (TemplatePath, "*.xml")) {
				File.Delete (templateFile);
			}
			foreach (CodeTemplate template in templates) {
				if (string.IsNullOrEmpty (template.Shortcut)) {
					LoggingService.LogUserError ("CodeTemplateService: Can't save unnamed template " + template);
					continue;
				}
				SaveTemplate (template, Path.Combine (TemplatePath, template.Shortcut + ".template.xml"));
			}
		}*/
		
		static List<CodeTemplate> LoadTemplates (XmlReader reader)
		{
			List<CodeTemplate> result = new List<CodeTemplate> ();
			
			try {
				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.LocalName) {
						case Node:
							string fileVersion = reader.GetAttribute (VersionAttribute);
							if (fileVersion != Version) 
								return null;
							break;
						case CodeTemplate.Node:
							result.Add (CodeTemplate.Read (reader));
							break;
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("CodeTemplateService: Exception while loading template.", e);
				return null;
			} finally {
				reader.Close ();
			}
			return result;
		}
		
		static string TemplatePath {
			get {
				return UserProfile.Current.UserDataRoot.Combine ("Snippets");
			}
		}
		
		static List<CodeTemplate> LoadTemplates ()
		{
			const string ManifestResourceName = "MonoDevelop-templates.xml";
			var builtinTemplates = LoadTemplates (XmlReader.Create (typeof (CodeTemplateService).Assembly.GetManifestResourceStream (ManifestResourceName)));
			if (Directory.Exists (TemplatePath)) {
				var result = new List<CodeTemplate> ();
				foreach (string templateFile in Directory.GetFiles (TemplatePath, "*.xml")) {
					result.AddRange (LoadTemplates (XmlReader.Create (templateFile)));
				}
				// merge user templates with built in templates
				for (int i = 0; i < builtinTemplates.Count; i++) {
					var curTemplate = builtinTemplates[i];
					if (!result.Any (t => t.Shortcut == curTemplate.Shortcut))
						result.Add (curTemplate);
				}

				return result;
			}
			
			LoggingService.LogInfo ("CodeTemplateService: No user templates, reading default templates.");
			return builtinTemplates;
		}
#endregion
	}
}
