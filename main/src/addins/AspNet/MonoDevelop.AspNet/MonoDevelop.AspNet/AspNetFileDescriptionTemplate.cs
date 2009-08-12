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
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Xml;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.AspNet
{
	
	public class AspNetFileDescriptionTemplate : SingleFileDescriptionTemplate
	{
		string content;
		Hashtable codeAreas = new Hashtable ();
		
		public override void Load (XmlElement filenode)
		{			
			//pull out the main area
			XmlElement fileText = filenode ["FileText"];
			if (fileText == null)
				throw new InvalidOperationException ("Invalid ASP.NET template: FileText element not found.");
			content = fileText.InnerText;
			
			//collect all of the code substitution areas
			foreach (XmlNode xn in filenode.GetElementsByTagName ("CodeTranslationFile")) {
				XmlElement xe = xn as XmlElement;
				if (xe == null)
					continue;
				
				string name = xe.GetAttribute ("TagName");
				
				if ((name == null) || (name.Length == 0))
					throw new InvalidOperationException ("Invalid ASP.NET template: CodeTranslationFile must have valid TagName.");
				
				//This is overzealous, but better safe than sorry
				char [] forbiddenChars = "`-=[];'#,./\\Â¬!\"Â£$%^&*()_+{}:@~|<>?".ToCharArray ();
				if (name.IndexOfAny (forbiddenChars) > -1)
					throw new InvalidOperationException ("Invalid ASP.NET template: TagName must be alphanumeric.");
				
				if (codeAreas.ContainsKey (name))
					throw new InvalidOperationException ("Invalid ASP.NET template: all TagNames must be unique within the AspNetFile.");
					
				CodeTranslationFileDescriptionTemplate templ =
					FileDescriptionTemplate.CreateTemplate (xe) as CodeTranslationFileDescriptionTemplate;
					
				if (templ == null)
					throw new InvalidOperationException ("Invalid ASP.NET template: invalid CodeTranslationFile.");
					
				codeAreas [name] = templ;
			}
			
			base.Load (filenode);
		}
		
		public override string CreateContent (string language)
		{
			return content;
		}
		
		public override void ModifyTags (SolutionItem policyParent, Project project, string language, string identifier, string fileName, ref Dictionary<string,string> tags)
		{
			tags ["Doctype"] = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">";
			
			//get a language binding
			IDotNetLanguageBinding langbinding = null;
			if (project == null) {
				langbinding = (IDotNetLanguageBinding) LanguageBindingService.GetBindingPerLanguageName (language);
			} else {
				DotNetProject dnp = (DotNetProject) project;
				langbinding = dnp.LanguageBinding;
			}
			
			// work out the ASP.NET language code. Although it's  originally a hack around MD's VBNet language name 
			// not corresponding to any of the valid ASP.NET codes, we also provide an interface that
			// non-core language bindings can implement to advertise that they support ASP.NET 
			string languageCode = language;
			if (langbinding is IAspNetLanguageBinding) {
				languageCode = ((IAspNetLanguageBinding) langbinding).AspNetLanguageCode;
			} else if (language == "VBNet") {
				languageCode = "VB";
			} else if (language != "C#") {
				LoggingService.LogWarning ("The language binding '{0}' does not have explicit support for ASP.NET", language);
			}
			tags ["AspNetLanguage"] = languageCode;
			
			base.ModifyTags (policyParent, project, language, identifier, fileName, ref tags);
			
			//nothing after this point is relevant to tag substitution for filenames,
			//and some will even crash, so drop out now
			if (fileName == null)
				return;
			
			// Build tags for ${CodeRegion:#} substitutions			
			foreach (string regionName in codeAreas.Keys) {
				CodeTranslationFileDescriptionTemplate templ =
					(CodeTranslationFileDescriptionTemplate) codeAreas [regionName];
				
				//makes CodeTranslationFile's internal name substitition easier
				templ.GetFileName (policyParent, project, language, project == null? null :project.BaseDirectory, (string) tags ["Name"]);
				
				Stream stream = templ.CreateFileContent (policyParent, project, language, fileName);
				StreamReader reader = new StreamReader (stream);
				tags ["CodeRegion:"+regionName] = reader.ReadToEnd ();
				reader.Close ();
				stream.Close ();
			}
		}
	}
}
