//  LanguageBindingService.cs
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
	public class LanguageBindingService
	{
		List<LanguageBindingCodon> bindings = null;
		ILanguageBinding[] langs;
		
		public LanguageBindingService ()
		{
			bindings = new List<LanguageBindingCodon> ();
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/ProjectModel/LanguageBindings", OnExtensionChanged);
		}
		
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
