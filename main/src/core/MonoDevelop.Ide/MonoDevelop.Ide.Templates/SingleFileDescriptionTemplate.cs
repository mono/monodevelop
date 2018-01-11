//
// SingleFileDescriptionTemplate.cs
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
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.StandardHeader;
using System.Text;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Projects.SharedAssetsProjects;
using MonoDevelop.Core.StringParsing;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Templates
{
	public class SingleFileDescriptionTemplate: FileDescriptionTemplate
	{
		string name;
		string defaultName;
		string defaultExtension;
		bool defaultExtensionDefined;
		string generatedFile;
		bool suppressAutoOpen = false;
		bool addStandardHeader = false;
		string dependsOn;
		string buildAction;
		string customTool;
		string customToolNamespace;
		string subType;
		List<string> references = new List<string> ();
		
		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			name = filenode.GetAttribute ("name");
			defaultName = filenode.GetAttribute ("DefaultName");
			defaultExtension = filenode.GetAttribute ("DefaultExtension");
			defaultExtensionDefined = filenode.Attributes ["DefaultExtension"] != null;
			dependsOn = filenode.GetAttribute ("DependsOn");
			customTool = filenode.GetAttribute ("CustomTool");
			customToolNamespace = filenode.GetAttribute ("CustomToolNamespace");
			subType = filenode.GetAttribute ("SubType");
			
			buildAction = BuildAction.Compile;
			buildAction = filenode.GetAttribute ("BuildAction");
			if (string.IsNullOrEmpty (buildAction))
				buildAction = null;
			
			string suppressAutoOpenStr = filenode.GetAttribute ("SuppressAutoOpen");
			if (!string.IsNullOrEmpty (suppressAutoOpenStr)) {
				try {
					suppressAutoOpen = bool.Parse (suppressAutoOpenStr);
				} catch (FormatException) {
					throw new InvalidOperationException ("Invalid value for SuppressAutoOpen in template.");
				}
			}
			
			string addStandardHeaderStr = filenode.GetAttribute ("AddStandardHeader");
			if (!string.IsNullOrEmpty (addStandardHeaderStr)) {
				try {
					addStandardHeader = bool.Parse (addStandardHeaderStr);
				} catch (FormatException) {
					throw new InvalidOperationException ("Invalid value for AddStandardHeader in template.");
				}
			}
			
			foreach (XmlElement elem in filenode.SelectNodes ("AssemblyReference")) {
				string aref = elem.InnerText.Trim ();
				if (!string.IsNullOrEmpty (aref))
					references.Add (aref);
			}
		}
		
		public override string Name {
			get { return name; } 
		}
		
		public bool AddStandardHeader {
			get { return addStandardHeader; }
			set { addStandardHeader = value; }
		}
		
		public sealed override async Task<bool> AddToProjectAsync (SolutionFolderItem policyParent, Project project, string language, string directory, string name)
		{
			return await AddFileToProject (policyParent, project, language, directory, name) != null;
		}
		
		public async Task<ProjectFile> AddFileToProject (SolutionFolderItem policyParent, Project project, string language, string directory, string name)
		{
			generatedFile = await SaveFile (policyParent, project, language, directory, name);
			if (generatedFile != null) {		
				string buildAction = this.buildAction ?? project.GetDefaultBuildAction (generatedFile);
				ProjectFile projectFile = project.AddFile (generatedFile, buildAction);
				
				if (!string.IsNullOrEmpty (dependsOn)) {
					var model = CombinedTagModel.GetTagModel (ProjectTagModel, policyParent, project, language, name, generatedFile);
					string parsedDepName = StringParserService.Parse (dependsOn, model);
					if (projectFile.DependsOn != parsedDepName)
						projectFile.DependsOn = parsedDepName;
				}
				
				if (!string.IsNullOrEmpty (customTool))
					projectFile.Generator = customTool;

				if (!string.IsNullOrEmpty (customToolNamespace)) {
					var model = CombinedTagModel.GetTagModel (ProjectTagModel, policyParent, project, language, name, generatedFile);
					projectFile.CustomToolNamespace = StringParserService.Parse (customToolNamespace, model);
				}

				if (!string.IsNullOrEmpty (subType))
					projectFile.ContentType = subType;

				DotNetProject netProject = project as DotNetProject;
				if (netProject != null) {
					// Add required references
					foreach (string aref in references) {
						string res = netProject.AssemblyContext.GetAssemblyFullName (aref, netProject.TargetFramework);
						res = netProject.AssemblyContext.GetAssemblyNameForVersion (res, netProject.TargetFramework);
						if (!ContainsReference (netProject, res))
							netProject.References.Add (ProjectReference.CreateAssemblyReference (aref));
					}
				}
				
				return projectFile;
			} else
				return null;
		}
		
		public override bool SupportsProject (Project project, string projectPath)
		{
			DotNetProject netProject = project as DotNetProject;
			if (netProject != null) {
				// Ensure that the references are valid inside the project's target framework.
				foreach (string aref in references) {
					string res = netProject.AssemblyContext.GetAssemblyFullName (aref, netProject.TargetFramework);
					if (string.IsNullOrEmpty (res))
						return false;
					res = netProject.AssemblyContext.GetAssemblyNameForVersion (res, netProject.TargetFramework);
					if (string.IsNullOrEmpty (res))
						return false;
				}
			}
			
			return true;
		}
		
		bool ContainsReference (DotNetProject project, string aref)
		{
			if (string.IsNullOrEmpty (aref))
				return false;
			string aname;
			int i = aref.IndexOf (',');
			if (i == -1)
				aname = aref;
			else
				aname = aref.Substring (0, i);
			foreach (ProjectReference pr in project.References) {
				if (pr.ReferenceType == ReferenceType.Package && (pr.Reference == aname || pr.Reference.StartsWith (aname + ",")) || 
					pr.ReferenceType != ReferenceType.Package && pr.Reference.Contains (aname))
					return true;
			}
			return false;
		}
		
		public override void Show ()
		{
			if (!suppressAutoOpen)
				IdeApp.Workbench.OpenDocument (generatedFile, project: null);
		}
		
		// Creates a file and saves it to disk. Returns the path to the new file
		// All parameters are optional (can be null)
		public async Task<string> SaveFile (SolutionFolderItem policyParent, Project project, string language, string baseDirectory, string entryName)
		{
			string file = GetFileName (policyParent, project, language, baseDirectory, entryName);
			AlertButton questionResult = null;
			
			if (File.Exists (file)) {
				questionResult = MessageService.AskQuestion (GettextCatalog.GetString ("File already exists"),
				                                             GettextCatalog.GetString ("File {0} already exists.\nDo you want to overwrite the existing file or add it to the project?", file),
				                                             AlertButton.Cancel,
				                                             AlertButton.AddExistingFile,
				                                             AlertButton.OverwriteFile);
				if (questionResult == AlertButton.Cancel)
					return null;
			}

			if (!Directory.Exists (Path.GetDirectoryName (file)))
				Directory.CreateDirectory (Path.GetDirectoryName (file));

			if (questionResult == null || questionResult == AlertButton.OverwriteFile) {
				Stream stream = CreateFileContent (policyParent, project, language, file, entryName) ?? await CreateFileContentAsync (policyParent, project, language, file, entryName);

				byte [] buffer = new byte [2048];
				int nr;
				FileStream fs = null;
				try {
					fs = File.Create (file);
					while ((nr = stream.Read (buffer, 0, 2048)) > 0)
						fs.Write (buffer, 0, nr);
				} finally {
					stream.Close ();
					if (fs != null)
						fs.Close ();
				}
			}
			return file;
		}
		
		// Returns the name of the file that this template generates.
		// All parameters are optional (can be null)
		public virtual string GetFileName (SolutionFolderItem policyParent, Project project, string language, string baseDirectory, string entryName)
		{
			if (string.IsNullOrEmpty (entryName) && !string.IsNullOrEmpty (defaultName))
				entryName = defaultName;
			
			string fileName = entryName;
			
			//substitute tags
			if ((name != null) && (name.Length > 0)) {
				var model = CombinedTagModel.GetTagModel (ProjectTagModel, policyParent, project, language, entryName ?? name, null);
				fileName = StringParserService.Parse (name, model);
			}
			
			if (fileName == null)
				throw new InvalidOperationException (GettextCatalog.GetString ("File name not provided in template"));
			
			//give it an extension if it didn't get one (compatibility with pre-substition behaviour)
			if (Path.GetExtension (fileName).Length == 0) {
				if (defaultExtensionDefined) {
					fileName = fileName + defaultExtension;
				}
				else if (!string.IsNullOrEmpty  (language)) {
					var languageBinding = GetLanguageBinding (language);
					fileName = languageBinding.GetFileName (fileName);
				} 
			}
			
			if (baseDirectory != null)
				fileName = Path.Combine (baseDirectory, fileName);

			return fileName;
		}

		protected virtual string ProcessContent (string content, IStringTagModel tags)
		{
			return StringParserService.Parse (content, tags);
		}

		[Obsolete("Use public virtual async Task<Stream> CreateFileContentAsync (SolutionFolderItem policyParent, Project project, string language, string fileName, string identifier).")]
		public virtual Stream CreateFileContent (SolutionFolderItem policyParent, Project project, string language, string fileName, string identifier)
		{
			return null;
		}

		// Returns a stream with the content of the file.
		// project and language parameters are optional
		public virtual async Task<Stream> CreateFileContentAsync (SolutionFolderItem policyParent, Project project, string language, string fileName, string identifier)
		{
			var model = CombinedTagModel.GetTagModel (ProjectTagModel, policyParent, project, language, identifier, fileName);

			//HACK: for API compat, CreateContent just gets the override, not the base model
			// but ProcessContent gets the entire model
			string content = CreateContent (project, model.OverrideTags, language);

			content = ProcessContent (content, model);

			string mime = DesktopService.GetMimeTypeForUri (fileName);
			var formatter = !string.IsNullOrEmpty (mime) ? CodeFormatterService.GetFormatter (mime) : null;
			
			if (formatter != null) {
				var formatted = formatter.FormatText (policyParent != null ? policyParent.Policies : null, content);
				if (formatted != null)
					content = formatted;
			}
			
			var ms = new MemoryStream ();
			Encoding encoding = null; 
			TextStylePolicy textPolicy = policyParent != null ? policyParent.Policies.Get<TextStylePolicy> (mime ?? "text/plain")
				: MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (mime ?? "text/plain");
			string eolMarker = TextStylePolicy.GetEolMarker (textPolicy.EolMarker);

			var ctx = await EditorConfigService.GetEditorConfigContext (fileName);
			if (ctx != null) {
				ctx.CurrentConventions.UniversalConventions.TryGetEncoding (out encoding);
				if (ctx.CurrentConventions.UniversalConventions.TryGetLineEnding (out string lineEnding))
					eolMarker = lineEnding;
			}
			if (encoding == null)
				encoding = System.Text.Encoding.UTF8;
			var bom = encoding.GetPreamble ();
			if (bom != null && bom.Length > 0)
				ms.Write (bom, 0, bom.Length);

			byte[] data;
			if (AddStandardHeader) {
				string header = StandardHeaderService.GetHeader (policyParent, fileName, true);
				data = encoding.GetBytes (header);
				ms.Write (data, 0, data.Length);
			}
			
			var doc = TextEditorFactory.CreateNewDocument ();
			doc.Text = content;
			

			byte[] eolMarkerBytes = encoding.GetBytes (eolMarker);
			
			var tabToSpaces = textPolicy.TabsToSpaces? new string (' ', textPolicy.TabWidth) : null;
			IDocumentLine lastLine = null;
			foreach (var line in doc.GetLines ()) {
				var lineText = doc.GetTextAt (line.Offset, line.Length);
				if (tabToSpaces != null)
					lineText = lineText.Replace ("\t", tabToSpaces);
				if (line.LengthIncludingDelimiter > 0) {
					data = encoding.GetBytes (lineText);
					ms.Write (data, 0, data.Length);
					ms.Write (eolMarkerBytes, 0, eolMarkerBytes.Length);
				}
				lastLine = line;
			}
			if (lastLine != null && lastLine.Length > 0) {
				if (ctx.CurrentConventions.UniversalConventions.TryGetRequireFinalNewline (out bool requireNewLine)) {
					if (requireNewLine)
						ms.Write (eolMarkerBytes, 0, eolMarkerBytes.Length);
				}
			}

			ms.Position = 0;
			return ms;
		}
		
		// Creates the text content of the file
		// The Language parameter is optional

		public virtual string CreateContent (Project project, Dictionary<string,string> tags, string language)
		{
			return CreateContent (language);
		}
		
		public virtual string CreateContent (string language)
		{
			return string.Empty;
		}
		
		// Can add tags for substitution based on project, language or filename.
		// If overriding but still want base implementation's tags, should invoke base method.
		// We supply defaults whenever it is possible, to avoid having unsubstituted tags. However,
		// do not substitute blanks when a sensible default cannot be guessed, because they result
		//in less obvious errors.
		public virtual void ModifyTags (SolutionFolderItem policyParent, Project project, string language,
			string identifier, string fileName, ref Dictionary<string,string> tags)
		{
			FileTemplateTagsModifier.ModifyTags (policyParent, project, language, identifier, fileName, ref tags);
		}
		
		static string CreateIdentifierName (string identifier)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = 0; i < identifier.Length; i++) {
				char ch = identifier[i];
				if (i != 0 && Char.IsLetterOrDigit (ch) || i == 0 && Char.IsLetter (ch) || ch == '_') {
					result.Append (ch);
				} else {
					result.Append ('_');
				}
			}
			return result.ToString ();
		}

		
		internal static LanguageBinding GetLanguageBinding (string language)
		{
			var binding = LanguageBindingService.GetBindingPerLanguageName (language);
			if (binding == null)
				throw new InvalidOperationException (GettextCatalog.GetString ("Language '{0}' not found", language));
			return binding;
		}
	}
}
