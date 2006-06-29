//
// AspNetFileDescriptionTemplate.cs: Template that translates regions of C# 
//     into the current .NET language, and substitutes them into the template.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.Collections;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Xml;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Projects.CodeGeneration;

namespace AspNetAddIn
{
	
	public class AspNetFileDescriptionTemplate : SingleFileDescriptionTemplate
	{
		string content;
		string namePattern;
		string entryName;
		Hashtable codeAreas = new Hashtable ();
		
		public override void Load (XmlElement filenode)
		{
			namePattern = filenode.GetAttribute ("NamePattern");			
			
			//collect all of the CodeDom areas
			foreach (XmlNode xn in filenode.GetElementsByTagName ("CodeRegion")) {
				XmlElement xe = xn as XmlElement;
				if (xe == null)
					continue;
				
				string name = xe.GetAttribute ("name");
				
				if ((name == null) || (name.Length == 0))
					throw new InvalidOperationException ("Invalid ASP.NET template: CodeRegions must have valid names.");
				
				//This is overzealous, but better safe than sorry
				char [] forbiddenChars = "`-=[];'#,./\\¬!\"£$%^&*()_+{}:@~|<>?".ToCharArray ();
				if (name.IndexOfAny (forbiddenChars) > -1)
					throw new InvalidOperationException ("Invalid ASP.NET template: CodeRegion names must be alphanumeric.");
				
				if (codeAreas.ContainsKey (name))
					throw new InvalidOperationException ("Invalid ASP.NET template: CodeRegions must have different names.");
				
				codeAreas [name] = xe.InnerText;
			}
			
			//pull out the main area
			XmlElement fileText = filenode ["FileText"];
			if (fileText == null)
				throw new InvalidOperationException ("Invalid ASP.NET template: FileText element not found.");
			content = fileText.InnerText;
			
			base.Load (filenode);
		}
			
		public override bool IsValidName (string name, string language)
		{
			if (name.Length > 0) {
				IDotNetLanguageBinding binding = GetDotNetLanguageBinding (language);
				CodeDomProvider provider = binding.GetCodeDomProvider ();
				if (provider == null)
					throw new InvalidOperationException ("The language '" + language + "' does not have support for CodeDom.");
				
				return provider.IsValidIdentifier (name);
			}
			else
				return false;
		}
		
		public override string GetFileName (Project project, string language, string baseDirectory, string entryName)
		{
			this.entryName = entryName;
			
			if (namePattern == "")
				return base.GetFileName (project, language, baseDirectory, entryName);
			
			StringParserService sps = (StringParserService) ServiceManager.GetService (typeof (StringParserService));
			
			Hashtable tags = new Hashtable ();
			ModifyTags (project, language, entryName, ref tags);
			string fileName = sps.Parse (namePattern, HashtableToStringArray (tags));
			
			if (baseDirectory != null)
				fileName = Path.Combine (baseDirectory, fileName);
			
			return fileName;
		}
		
		public override string CreateContent (string language)
		{
			return content;
		}
		
		public override void ModifyTags (Project project, string language, string fileName, ref Hashtable tags)
		{
			string languageExtension = Path.GetExtension (GetDotNetLanguageBinding (language).GetFileName ("Default")).Remove (0, 1);
			
			tags ["Language"] = language;
			tags ["Doctype"] = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">";
			tags ["LanguageExtension"] = languageExtension;
			tags ["FileName"] = fileName;
			
			base.ModifyTags (project, language, fileName, ref tags);
			
			if ((tags ["Namespace"] == null) && (((string) tags ["Namespace"]).Length < 1))
				tags ["Namespace"] = "Default";
			
			//nothing after this point is relevant to tag substitution for filenames,
			//and some will even crash, so drop out now
			if (fileName == null)
				return;
			
			//our more complex file name generation means that we can't just cut the extension off the end like
			//the base class does, so we overwrite its name tag with a better one
			
			if ((entryName != null) && (entryName.Length > 0))
				tags ["Name"] = entryName;
			else if ((tags ["Name"] != null) && (((string)tags ["Name"]).Length > 0))
				tags ["Name"] = Path.GetFileNameWithoutExtension ((string) tags ["Name"]);
			else {
				string f = Path.GetFileName (fileName);
				while (Path.GetExtension (f).Length > 0) {
					f = Path.GetFileNameWithoutExtension (f);
				}
				tags ["Name"] = f;
			}
			
			// Build tags for ${CodeRegion:#} substitutions.
			// This is a bit hacky doing it here instead of in CreateContent, but need to substitute all tags in all code
			// fragments before language is translated, because language translation gets confused by substitution tokens.
			StringParserService sps = (StringParserService) ServiceManager.GetService (typeof (StringParserService));
			
			string [,] tagsArr = HashtableToStringArray (tags);
			
			foreach (string regionName in codeAreas.Keys) {
				string s = sps.Parse ((string) codeAreas [regionName], tagsArr);
				tags ["CodeRegion:"+regionName]  = TranslateCode (s, language);
			}
		}
		
		// Adapted from CodeDomFileDescriptionTemplate.cs
		// TODO: Refactor this code out, and allow for different source languages: maybe a TranslationService
		// TODO: Remove need to have a namespace and type (i.e. translate fragments)
		private string TranslateCode (string csCode, string language) {
			if (language == null || language == "")
				throw new InvalidOperationException ("Language not defined in CodeDom based template.");
			
			IDotNetLanguageBinding binding = GetDotNetLanguageBinding (language);
			
			CodeDomProvider provider = binding.GetCodeDomProvider ();
			if (provider == null)
				throw new InvalidOperationException ("The language '" + language + "' does not have support for CodeDom.");
			
			IDotNetLanguageBinding csBinding = GetDotNetLanguageBinding ("C#");
			CodeDomProvider csProvider = csBinding.GetCodeDomProvider ();
			StringReader sr = new StringReader (csCode);
			CodeCompileUnit ccu = csProvider.Parse (sr);
			
			//parser seems to insist on adding extra empty namespace
			for (int j = 0; j < ccu.Namespaces.Count; j++) {
				CodeNamespace cn = ccu.Namespaces [j];
				if ((cn.Name == "Global") && (cn.Types.Count == 0)) {
					ccu.Namespaces.RemoveAt (j);
					break;
				}
			}
			
			ICodeGenerator generator = provider.CreateGenerator();
			
			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.IndentString = "\t";
			options.BracingStyle = "C";
			
			StringWriter sw = new StringWriter ();
			generator.GenerateCodeFromCompileUnit (ccu, sw, options);
			sw.Close();
			
			string txt = sw.ToString ();
			int i = txt.IndexOf ("</autogenerated>");
			if (i == -1) return txt;
			i = txt.IndexOf ('\n', i);
			if (i == -1) return txt;
			i = txt.IndexOf ('\n', i + 1);
			if (i == -1) return txt;
			
			return txt.Substring (i+1);		
		}
	}
}
