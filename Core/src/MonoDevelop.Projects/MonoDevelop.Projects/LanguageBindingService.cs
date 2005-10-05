// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Reflection;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Projects
{
	public class LanguageBindingService : AbstractService
	{
		LanguageBindingCodon[] bindings = null;
		
		public ILanguageBinding GetBindingPerLanguageName(string languagename)
		{
			LanguageBindingCodon codon = GetCodonPerLanguageName(languagename);
			return codon == null ? null : codon.LanguageBinding;
		}
		
		public ILanguageBinding GetBindingPerFileName(string filename)
		{
			LanguageBindingCodon codon = GetCodonPerFileName(filename);
			return codon == null ? null : codon.LanguageBinding;
		}
		
		public ILanguageBinding GetBindingPerProjectFile(string filename)
		{
			LanguageBindingCodon codon = GetCodonPerProjectFile(filename);
			return codon == null ? null : codon.LanguageBinding;
		}

		internal LanguageBindingCodon GetCodonPerLanguageName(string languagename)
		{
			foreach (LanguageBindingCodon binding in bindings) {
				if (binding.LanguageBinding.Language == languagename) {
					return binding;
				}
			}
			return null;
		}
		
		internal LanguageBindingCodon GetCodonPerFileName(string filename)
		{
			foreach (LanguageBindingCodon binding in bindings) {
				if (binding.LanguageBinding.CanCompile(filename)) {
					return binding;
				}
			}
			return null;
		}
		
		internal LanguageBindingCodon GetCodonPerProjectFile(string filename)
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(filename);
			return GetCodonPerLanguageName(doc.DocumentElement.Attributes["projecttype"].InnerText);
		}
		
		public override void InitializeService ()
		{
			base.InitializeService ();
			bindings = (LanguageBindingCodon[]) Runtime.AddInService.GetTreeItems ("/SharpDevelop/Workbench/LanguageBindings", typeof(LanguageBindingCodon));
		}
	}
}
