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
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Parser;

using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Projects
{
	public class LanguageBindingService : AbstractService
	{
		List<LanguageBindingCodon> bindings = null;
		ILanguageBinding[] langs;
		
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
		
		public ILanguageBinding[] GetLanguageBindings ()
		{
			if (langs != null)
				return langs;
				
			langs = new ILanguageBinding [bindings.Count];
			for (int n=0; n<langs.Length; n++)
				langs [n] = bindings [n].LanguageBinding;

			return langs;
		}

		public IParser GetParserForFile (string fileName)
		{
			foreach (LanguageBindingCodon binding in bindings) {
				if (binding.LanguageBinding.IsSourceCodeFile (fileName) && binding.LanguageBinding.Parser != null) {
					return binding.LanguageBinding.Parser;
				}
			}
			return null;
		}
		
		public IRefactorer GetRefactorerForFile (string fileName)
		{
			foreach (LanguageBindingCodon binding in bindings) {
				if (binding.LanguageBinding.IsSourceCodeFile (fileName) && binding.LanguageBinding.Refactorer != null) {
					return binding.LanguageBinding.Refactorer;
				}
			}
			return null;
		}
		
		public IRefactorer GetRefactorerForLanguage (string languagename)
		{
			foreach (LanguageBindingCodon binding in bindings) {
				if (binding.LanguageBinding.Language == languagename && binding.LanguageBinding.Refactorer != null) {
					return binding.LanguageBinding.Refactorer;
				}
			}
			return null;
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
				if (binding.LanguageBinding.IsSourceCodeFile(filename)) {
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
			bindings = new List<LanguageBindingCodon> ();
			AddinManager.AddExtensionNodeHandler ("/SharpDevelop/Workbench/LanguageBindings", OnExtensionChanged);
		}
		
		void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				bindings.Add ((LanguageBindingCodon) args.ExtensionNode);
				
				// Make sure the langs list is re-created
				langs = null;
			}
			else
				bindings.Remove ((LanguageBindingCodon) args.ExtensionNode);
		}
	}
}
