//  CodeDOMGeneratorUtility.cs
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
using System.Collections;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;

using MonoDevelop.Core;

namespace MonoDevelop.Projects.Ambience
{
	public class CodeDOMGeneratorUtility 
	{
		static AmbienceService ambienceService = Services.Ambience;
		
		public static CodeGeneratorOptions CodeGeneratorOptions {
			get {
				CodeGeneratorOptions options = new CodeGeneratorOptions();
				options.BlankLinesBetweenMembers = ambienceService.CodeGenerationProperties.Get("BlankLinesBetweenMembers", true);
				options.BracingStyle             = ambienceService.CodeGenerationProperties.Get("StartBlockOnSameLine", true) ? "Block" : "C";
				options.ElseOnClosing            = ambienceService.CodeGenerationProperties.Get("ElseOnClosing", true);
				
				Properties docProperties = PropertyService.Get("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new Properties());
				
				if ((bool)docProperties.Get("TabsToSpaces", false)) {
					StringBuilder indentationString = new StringBuilder();
					for (int i = 0; i < (int)docProperties.Get("IndentationSize", 4); ++i) {
						indentationString.Append(' ');
					}
					options.IndentString = indentationString.ToString();
				} else {
					options.IndentString = "\t";
				}
				return options;
			}
		}
		
		public CodeTypeReference GetTypeReference(string type)
		{
			if (ambienceService.UseFullyQualifiedNames) {
				return new CodeTypeReference(type);
			} else {
				string[] arr = type.Split('.');
				string shortName = arr[arr.Length - 1];
				if (type.Length - shortName.Length - 1 > 0) {
					string n = type.Substring(0, type.Length - shortName.Length - 1);
					namespaces[n] = "";
				}
				return new CodeTypeReference(shortName);
			}
		}
		
		public CodeTypeReference GetTypeReference(Type type)
		{
			if (ambienceService.UseFullyQualifiedNames) {
				return new CodeTypeReference(type.FullName);
			} else {
				namespaces[type.Namespace] = "";
				return new CodeTypeReference(type.Name);
			}
		}
		
		public CodeTypeReferenceExpression GetTypeReferenceExpression(string type)
		{
			return new CodeTypeReferenceExpression(GetTypeReference(type));
		}

		public CodeTypeReferenceExpression GetTypeReferenceExpression(Type type)
		{
			return new CodeTypeReferenceExpression(GetTypeReference(type));
		}
		
		/// <summary>
		/// Adds a namespace import to the namespace import list.
		/// </summary>
		public void AddNamespaceImport(string ns)
		{
			namespaces[ns] = "";
		}
		
		/// <summary>
		/// Generates the namespace imports that caused of the usage of short type names
		/// </summary>
		public void GenerateNamespaceImports(CodeNamespace cnamespace)
		{
			foreach (string ns in namespaces.Keys) {
				cnamespace.Imports.Add(new CodeNamespaceImport(ns));
			}
		}
		
		Hashtable namespaces = new Hashtable();
	}	
}
