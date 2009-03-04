// 
// Engine.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace MonoDevelop.AspNet.Mvc.T4
{
	
	
	public class Engine
	{
		
		public Engine ()
		{
		}
		
		public string ProcessTemplate (string content, ITextTemplatingEngineHost host)
		{
			ParsedTemplate pt = ParsedTemplate.FromText (content, host);
			if (pt.HasErrorsNotWarnings ()) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			TemplateSettings settings = GetSettings (host, pt);
			if (pt.HasErrorsNotWarnings ()) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			CodeCompileUnit ccu = GetCompileUnit (host, pt, settings);
			if (pt.HasErrorsNotWarnings ()) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			CompilerResults results = GenerateCode (host, pt, settings, ccu);
			if (pt.HasErrorsNotWarnings ()) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			if (!String.IsNullOrEmpty (settings.Extension)) {
				host.SetFileExtension (settings.Extension);
			}
			if (settings.Encoding != null) {
				//FIXME: when is this called with false?
				host.SetOutputEncoding (settings.Encoding, true);
			}
			
			AppDomain appdomain = host.ProvideTemplatingAppDomain (content);
			TransformationRunner runner;
			if (appdomain != null) {
				runner = (TransformationRunner)
					appdomain.CreateInstanceAndUnwrap (typeof (TransformationRunner).Assembly.FullName,
					                                   typeof (TransformationRunner).FullName);
			} else {
				runner = new TransformationRunner ();
			}
			
			return runner.Run (results.PathToAssembly, settings.Namespace + "." + settings.Name, host, settings.Culture);
		}
		
		static CompilerResults GenerateCode (ITextTemplatingEngineHost host, ParsedTemplate pt, TemplateSettings settings, CodeCompileUnit ccu)
		{
			CompilerParameters pars = new CompilerParameters ();
			pars.GenerateExecutable = false;
			pars.GenerateInMemory = false;
			pars.IncludeDebugInformation = settings.Debug;
			
			//resolve and add assembly references
			foreach (string assem in settings.Assemblies) {
				string resolvedAssem = host.ResolveAssemblyReference (assem);
				if (!String.IsNullOrEmpty (resolvedAssem)) {
					pars.ReferencedAssemblies.Add (resolvedAssem);
				} else {
					pt.LogError ("Could not resolve assembly reference '" + assem + "'");
					return null;
				}
			}
			
			CompilerResults results = settings.Provider.CompileAssemblyFromDom (pars, ccu);
			pt.Errors.AddRange (results.Errors);
			return results;
		}
		
		static TemplateSettings GetSettings (ITextTemplatingEngineHost host, ParsedTemplate pt)
		{
			string language = null;
			
			TemplateSettings settings = new TemplateSettings ();
			foreach (Directive dt in pt.Directives) {
				switch (dt.Name) {
				case "template":
					string val = dt.Extract ("language");
					if (val != null)
						language = val;
					val = dt.Extract ("debug");
					if (val != null)
						settings.Debug = string.Compare (val, "true", StringComparison.OrdinalIgnoreCase) == 0;
					val = dt.Extract ("inherits");
					if (val != null)
						settings.Inherits = val;
					val = dt.Extract ("culture");
					if (val != null) {
						System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo (val);
						if (culture == null)
							pt.LogError ("Could not find culture '" + val + "'", dt).IsWarning = true;
						else
							settings.Culture = culture;
					}
					break;
					
				case "assembly":
					string name = dt.Extract ("name");
					if (name == null)
						pt.LogError ("Missing name attribute in assembly directive", dt);
					else
						settings.Assemblies.Add (name);
					break;
					
				case "import":
					string namespac = dt.Extract ("namespace");
					if (namespac == null)
						pt.LogError ("Missing namespace attribute in import directive", dt);
					else
						settings.Assemblies.Add (namespac);
					break;
					
				case "output":
					settings.Extension = dt.Extract ("extension");
					string encoding = dt.Extract ("encoding");
					if (encoding != null)
						settings.Encoding = Encoding.GetEncoding ("encoding");
					break;
				
				case "include":
					throw new InvalidOperationException ("Include is handled in the parser");
					
				default:
					throw new NotImplementedException ("Custom directives are not supported yet");
				}
				ComplainExcessAttributes (dt, pt);
			}
			
			if (settings.Name == null)
				settings.Name = string.Format ("GeneratedTextTransformation{0:x}", new System.Random ());
			if (settings.Namespace == null)
				settings.Namespace = typeof (TextTransformation).Namespace;
			
			//resolve the CodeDOM provider
			if (String.IsNullOrEmpty (language)) {
				pt.LogError ("No language was specified for the template");
				return settings;
			}
			
			//FIXME: handle versions correctly: C# VB C#v3.5 VBv3.5
			if (language == "C#v3.5")
				language = "C#";
			settings.Provider = System.CodeDom.Compiler.CodeDomProvider.CreateProvider (language);
			if (settings.Provider == null) {
				pt.LogError ("A provider could not be found for the language '" + language + "'");
				return settings;
			}
			
			return settings;
		}
		
		static bool ComplainExcessAttributes (Directive dt, ParsedTemplate pt)
		{
			if (dt.Attributes.Count == 0)
				return false;
			StringBuilder sb = new StringBuilder ("Unknown attributes ");
			bool first = true;
			foreach (string key in dt.Attributes.Keys) {
				if (!first) {
					sb.Append (", ");
				} else {
					first = false;
				}
				sb.Append (key);
			}
			sb.Append (" found in ");
			sb.Append (dt.Name);
			sb.Append (" directive.");
			pt.LogError (sb.ToString (), dt).IsWarning = true;
			return false;
		}
		
		static CodeCompileUnit GetCompileUnit (ITextTemplatingEngineHost host, ParsedTemplate pt, TemplateSettings settings)
		{
			//prep the compile unit
			CodeCompileUnit ccu = new CodeCompileUnit ();
			CodeNamespace namespac = new CodeNamespace (settings.Namespace);
			ccu.Namespaces.Add (namespac);
			
			foreach (string ns in settings.Imports)
				namespac.Imports.Add (new CodeNamespaceImport (ns));
			
			//prep the type
			CodeTypeDeclaration type = new CodeTypeDeclaration (settings.Name);
			if (!String.IsNullOrEmpty (settings.Inherits))
				type.BaseTypes.Add (new CodeTypeReference (settings.Inherits));
			else
				type.BaseTypes.Add (new CodeTypeReference (typeof (TextTransformation)));
			namespac.Types.Add (type);
			
			//prep the transform method
			CodeMemberMethod transformMeth = new CodeMemberMethod ();
			transformMeth.Name = "TransformText";
			transformMeth.ReturnType = new CodeTypeReference (typeof (String));
			transformMeth.Attributes = MemberAttributes.Public | MemberAttributes.Override;
			
			//method references that will need to be used multiple times
			CodeMethodReferenceExpression writeMeth =
				new CodeMethodReferenceExpression (new CodeThisReferenceExpression (), "Write",
				                                   new CodeTypeReference (typeof (String)));
			CodeMethodReferenceExpression toStringMeth =
				new CodeMethodReferenceExpression (new CodeTypeReferenceExpression (typeof (ToStringHelper)), "ToStringWithCulture");
			
			//build the code from the segments
			foreach (TemplateSegment seg in pt.Segments) {
				CodeStatement st = null;
				switch (seg.Type) {
				case SegmentType.Block:
					st = new CodeSnippetStatement (seg.Text);
					break;
				case SegmentType.Expression:
					st = new CodeExpressionStatement (
						new CodeMethodInvokeExpression (writeMeth,
							new CodeMethodInvokeExpression (toStringMeth, new CodeSnippetExpression (seg.Text))));
					break;
				case SegmentType.Content:
					st = new CodeExpressionStatement (new CodeMethodInvokeExpression (writeMeth, new CodePrimitiveExpression (seg.Text)));
					break;
				case SegmentType.Helper:
					CodeTypeMember mem = new CodeSnippetTypeMember (seg.Text);
					mem.LinePragma = new CodeLinePragma (host.TemplateFile, seg.Line);
					type.Members.Add (mem);
					break;
				default:
					throw new InvalidOperationException ();
				}
				if (st != null) {
					st.LinePragma = new CodeLinePragma (host.TemplateFile, seg.Line);
					transformMeth.Statements.Add (st);
				}
			}
			
			//complete the transform method
			transformMeth.Statements.Add (new CodeMethodReturnStatement (
				new CodeMethodInvokeExpression (
					new CodePropertyReferenceExpression (
						new CodeThisReferenceExpression (),
						"GenerationEnvironment"),
						"ToString")));
			type.Members.Add (transformMeth);
			
			//generate the Host property if needed
			if (settings.HostSpecific) {
				CodeMemberField hostField = new CodeMemberField (new CodeTypeReference (typeof (ITextTemplatingEngineHost)), "hostValue");
				hostField.Attributes = (hostField.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
				type.Members.Add (hostField);
				CodeMemberProperty hostProp = new CodeMemberProperty ();
				hostProp.Name = "Host";
				hostProp.Attributes = MemberAttributes.Public;
				hostProp.HasGet = hostProp.HasGet = true;
				hostProp.Type = hostField.Type;
				CodeFieldReferenceExpression hostFieldRef = new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), "hostValue");
				hostProp.SetStatements.Add (new CodeAssignStatement (hostFieldRef, new CodePropertySetValueReferenceExpression ()));
				hostProp.GetStatements.Add (new CodeMethodReturnStatement (hostFieldRef));
				type.Members.Add (hostProp);
			}
			
			return ccu;
		}
		
		class TemplateSettings
		{
			public TemplateSettings ()
			{
				Imports = new List<string> ();
				Assemblies = new List<string> ();
			}
			
			public bool HostSpecific { get; set; }
			public bool Debug { get; set; }
			public string Inherits { get; set; }
			public string Name { get; set; }
			public string Namespace { get; set; }
			public List<string> Imports { get; private set; }
			public List<string> Assemblies { get; private set; }
			public System.CodeDom.Compiler.CodeDomProvider Provider { get; set; }
			public Encoding Encoding { get; set; }
			public string Extension { get; set; }
			public System.Globalization.CultureInfo Culture { get; set; }
		}
		
		class TransformationRunner : MarshalByRefObject
		{
			public string Run (string assemblyPath, string type, ITextTemplatingEngineHost host, System.Globalization.CultureInfo culture)
			{
				
				System.Reflection.Assembly assem = System.Reflection.Assembly.Load (assemblyPath);
				Type transformType = assem.GetType (type);
				TextTransformation tt = (TextTransformation)Activator.CreateInstance (transformType);
				
				//set the host rpoperty if it exists
				System.Reflection.PropertyInfo hostProp = transformType.GetProperty ("Host", typeof (ITextTemplatingEngineHost));
				if (hostProp != null && hostProp.CanWrite)
					hostProp.SetValue (tt, host, null);
				
				//set the culture
				if (culture != null)
					ToStringHelper.FormatProvider = culture;
				else
					ToStringHelper.FormatProvider = System.Globalization.CultureInfo.InvariantCulture;
				
				tt.Initialize ();
				string output = tt.TransformText ();
				host.LogErrors (tt.Errors);
				ToStringHelper.FormatProvider = System.Globalization.CultureInfo.InvariantCulture;
				return output;
			}
		}
	}
	
	public interface ITextTemplatingEngine
	{
		string ProcessTemplate (string content, ITextTemplatingEngineHost host);
	}
}
