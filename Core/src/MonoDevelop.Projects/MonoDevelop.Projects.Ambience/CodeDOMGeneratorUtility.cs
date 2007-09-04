// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
