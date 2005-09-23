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

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Services
{
	public class CodeDOMGeneratorUtility 
	{
		AmbienceService ambienceService = Runtime.Ambience;
		
		public CodeGeneratorOptions CreateCodeGeneratorOptions {
			get {
				CodeGeneratorOptions options = new CodeGeneratorOptions();
				options.BlankLinesBetweenMembers = ambienceService.CodeGenerationProperties.GetProperty("BlankLinesBetweenMembers", true);
				options.BracingStyle             = ambienceService.CodeGenerationProperties.GetProperty("StartBlockOnSameLine", true) ? "Block" : "C";
				options.ElseOnClosing            = ambienceService.CodeGenerationProperties.GetProperty("ElseOnClosing", true);
				
				IProperties docProperties = ((IProperties)Runtime.Properties.GetProperty("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties()));
				
				if ((bool)docProperties.GetProperty("TabsToSpaces", false)) {
					StringBuilder indentationString = new StringBuilder();
					for (int i = 0; i < (int)docProperties.GetProperty("IndentationSize", 4); ++i) {
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
