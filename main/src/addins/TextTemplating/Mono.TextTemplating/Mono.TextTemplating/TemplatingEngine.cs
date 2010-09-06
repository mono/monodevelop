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
using Microsoft.CSharp;
using Microsoft.VisualStudio.TextTemplating;
using System.Linq;
using System.Reflection;

namespace Mono.TextTemplating
{
	public class TemplatingEngine : MarshalByRefObject, Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngine
	{
		public string ProcessTemplate (string content, ITextTemplatingEngineHost host)
		{
			var tpl = CompileTemplate (content, host);
			if (tpl != null)
				return tpl.Process ();
			return null;
		}
		
		public string PreprocessTemplate (string content, ITextTemplatingEngineHost host, string className, 
			string classNamespace, out string language, out string[] references)
		{
			if (content == null)
				throw new ArgumentNullException ("content");
			if (host == null)
				throw new ArgumentNullException ("host");
			if (className == null)
				throw new ArgumentNullException ("className");
			if (classNamespace == null)
				throw new ArgumentNullException ("classNamespace");
			
			language = null;
			references = null;
			
			var pt = ParsedTemplate.FromText (content, host);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			var settings = GetSettings (host, pt);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			settings.Name = className;
			settings.Namespace = classNamespace;
			settings.IncludePreprocessingHelpers = string.IsNullOrEmpty (settings.Inherits);
			language = settings.Language;
			
			var ccu = GenerateCompileUnit (host, content, pt, settings);
			references = ProcessReferences (host, pt, settings).ToArray ();
			
			host.LogErrors (pt.Errors);
			if (pt.Errors.HasErrors) {
				return null;
			}
			
			var options = new CodeGeneratorOptions ();
			using (var sw = new StringWriter ()) {
				settings.Provider.GenerateCodeFromCompileUnit (ccu, sw, options);
				return sw.ToString ();
			};
		}

		public CompiledTemplate CompileTemplate (string content, ITextTemplatingEngineHost host)
		{
			if (content == null)
				throw new ArgumentNullException ("content");
			if (host == null)
				throw new ArgumentNullException ("host");
			
			var pt = ParsedTemplate.FromText (content, host);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			var settings = GetSettings (host, pt);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			if (!string.IsNullOrEmpty (settings.Extension)) {
				host.SetFileExtension (settings.Extension);
			}
			if (settings.Encoding != null) {
				//FIXME: when is this called with false?
				host.SetOutputEncoding (settings.Encoding, true);
			}
			
			var ccu = GenerateCompileUnit (host, content, pt, settings);
			var references = ProcessReferences (host, pt, settings);
			if (pt.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				return null;
			}
			
			var results = GenerateCode (host, references, settings, ccu);
			if (results.Errors.HasErrors) {
				host.LogErrors (pt.Errors);
				host.LogErrors (results.Errors);
				return null;
			}
			
			var templateClassFullName = settings.Namespace + "." + settings.Name;
			AppDomain domain = host.ProvideTemplatingAppDomain (content);
			if (domain != null) {
				domain.DoCallBack (delegate {
					
				});
				var type = typeof (CompiledTemplate);
				references.Add (type.Assembly.Location);
				var obj = domain.CreateInstanceAndUnwrap (type.Assembly.FullName, type.FullName, false,
					BindingFlags.CreateInstance, null,
					new object[] { host, results, templateClassFullName, settings.Culture, references.ToArray () },
					null, null, null);
				return (CompiledTemplate) obj;
			} else {
				return new CompiledTemplate (host, results, templateClassFullName, settings.Culture, references.ToArray ());
			}
		}
		
		static CompilerResults GenerateCode (ITextTemplatingEngineHost host, IEnumerable<string> references, TemplateSettings settings, CodeCompileUnit ccu)
		{
			CompilerParameters pars = new CompilerParameters () {
				GenerateExecutable = false,
				CompilerOptions = settings.CompilerOptions,
				IncludeDebugInformation = settings.Debug,
				GenerateInMemory = false,
			};
			
			foreach (var r in references)
				pars.ReferencedAssemblies.Add (r);
			
			if (settings.Debug)
				pars.TempFiles.KeepFiles = true;
			
			return settings.Provider.CompileAssemblyFromDom (pars, ccu);
		}
		
		static HashSet<string> ProcessReferences (ITextTemplatingEngineHost host, ParsedTemplate pt, TemplateSettings settings)
		{
			var resolved = new HashSet<string> ();
			
			foreach (string assem in settings.Assemblies.Union (host.StandardAssemblyReferences)) {
				if (resolved.Contains (assem))
					continue;
				
				string resolvedAssem = host.ResolveAssemblyReference (assem);
				if (!string.IsNullOrEmpty (resolvedAssem)) {
					resolved.Add (resolvedAssem);
				} else {
					pt.LogError ("Could not resolve assembly reference '" + assem + "'");
					return null;
				}
			}
			return resolved;
		}
		
		public static TemplateSettings GetSettings (ITextTemplatingEngineHost host, ParsedTemplate pt)
		{
			var settings = new TemplateSettings ();
			
			foreach (Directive dt in pt.Directives) {
				switch (dt.Name) {
				case "template":
					string val = dt.Extract ("language");
					if (val != null)
						settings.Language = val;
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
							pt.LogWarning ("Could not find culture '" + val + "'", dt.StartLocation);
						else
							settings.Culture = culture;
					}
					val = dt.Extract ("hostspecific");
					if (val != null) {
						settings.HostSpecific = string.Compare (val, "true", StringComparison.OrdinalIgnoreCase) == 0;
					}
					val = dt.Extract ("CompilerOptions");
					if (val != null) {
						settings.CompilerOptions = val;
					}
					break;
					
				case "assembly":
					string name = dt.Extract ("name");
					if (name == null)
						pt.LogError ("Missing name attribute in assembly directive", dt.StartLocation);
					else
						settings.Assemblies.Add (name);
					break;
					
				case "import":
					string namespac = dt.Extract ("namespace");
					if (namespac == null)
						pt.LogError ("Missing namespace attribute in import directive", dt.StartLocation);
					else
						settings.Imports.Add (namespac);
					break;
					
				case "output":
					settings.Extension = dt.Extract ("extension");
					string encoding = dt.Extract ("encoding");
					if (encoding != null)
						settings.Encoding = Encoding.GetEncoding (encoding);
					break;
				
				case "include":
					throw new InvalidOperationException ("Include is handled in the parser");
					
				case "parameter":
					AddDirective (settings, host, "ParameterDirectiveProcessor", dt);
					continue;
					
				default:
					bool isParameterProcessor = dt.Name == "parameter";
					string processorName = dt.Extract ("Processor");
					if (processorName == null)
						throw new InvalidOperationException ("Custom directive '" + dt.Name + "' does not specify a processor");
					
					AddDirective (settings, host, processorName, dt);
					continue;
				}
				ComplainExcessAttributes (dt, pt);
			}
			
			//initialize the custom processors
			foreach (var kv in settings.DirectiveProcessors) {
				kv.Value.Initialize (host);
				var hs = kv.Value as IRecognizeHostSpecific;
				if (hs == null)
					continue;
				if (hs.RequiresProcessingRunIsHostSpecific && !settings.HostSpecific)
					settings.HostSpecific = true;
					pt.LogWarning ("Directive processor '" + kv.Key + "' requires hostspecific=true, forcing on.");
				hs.SetProcessingRunIsHostSpecific (settings.HostSpecific);
			}
			
			if (settings.Name == null)
				settings.Name = string.Format ("GeneratedTextTransformation{0:x}", new System.Random ().Next ());
			if (settings.Namespace == null)
				settings.Namespace = typeof (TextTransformation).Namespace;
			
			//resolve the CodeDOM provider
			if (String.IsNullOrEmpty (settings.Language)) {
				pt.LogError ("No language was specified for the template");
				return settings;
			}
			
			if (settings.Language == "C#v3.5") {
				Dictionary<string, string> providerOptions = new Dictionary<string, string> ();
				providerOptions.Add ("CompilerVersion", "v3.5");
				settings.Provider = new CSharpCodeProvider (providerOptions);
			}
			else {
				settings.Provider = CodeDomProvider.CreateProvider (settings.Language);
			}
			
			if (settings.Provider == null) {
				pt.LogError ("A provider could not be found for the language '" + settings.Language + "'");
				return settings;
			}
			
			return settings;
		}
		
		static void AddDirective (TemplateSettings settings, ITextTemplatingEngineHost host, string processorName, Directive directive)
		{
			DirectiveProcessor processor;
			if (!settings.DirectiveProcessors.TryGetValue (processorName, out processor)) {
				switch (processorName) {
				case "ParameterDirectiveProcessor":
					processor = new ParameterDirectiveProcessor ();
					break;
				default:
					Type processorType = host.ResolveDirectiveProcessor (processorName);
					processor = (DirectiveProcessor) Activator.CreateInstance (processorType);
					break;
				}
				if (!processor.IsDirectiveSupported (directive.Name))
					throw new InvalidOperationException ("Directive processor '" + processorName + "' does not support directive '" + directive.Name + "'");
				
				settings.DirectiveProcessors [processorName] = processor;
			}
			settings.CustomDirectives.Add (new CustomDirective (processorName, directive));
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
			pt.LogWarning (sb.ToString (), dt.StartLocation);
			return false;
		}
		
		static void ProcessDirectives (ITextTemplatingEngineHost host, string content, ParsedTemplate pt, TemplateSettings settings)
		{
			foreach (var processor in settings.DirectiveProcessors.Values) {
				processor.StartProcessingRun (settings.Provider, content, pt.Errors);
			}
			
			foreach (var dt in settings.CustomDirectives) {
				var processor = settings.DirectiveProcessors[dt.ProcessorName];
				
				if (processor is RequiresProvidesDirectiveProcessor)
					throw new NotImplementedException ("RequiresProvidesDirectiveProcessor");
				
				processor.ProcessDirective (dt.Directive.Name, dt.Directive.Attributes);
			}
			
			foreach (var processor in settings.DirectiveProcessors.Values) {
				processor.FinishProcessingRun ();
				
				var imports = processor.GetImportsForProcessingRun ();
				if (imports != null)
					settings.Imports.UnionWith (imports);
				var references = processor.GetReferencesForProcessingRun ();
				if (references != null)
					settings.Assemblies.UnionWith (references);
			}
		}
		
		public static CodeCompileUnit GenerateCompileUnit (ITextTemplatingEngineHost host, string content, ParsedTemplate pt, TemplateSettings settings)
		{
			ProcessDirectives (host, content, pt, settings);

			//prep the compile unit
			var ccu = new CodeCompileUnit ();
			var namespac = new CodeNamespace (settings.Namespace);
			ccu.Namespaces.Add (namespac);
			
			foreach (string ns in settings.Imports.Union (host.StandardImports))
				namespac.Imports.Add (new CodeNamespaceImport (ns));
			
			//prep the type
			var type = new CodeTypeDeclaration (settings.Name);
			type.IsPartial = true;
			if (!string.IsNullOrEmpty (settings.Inherits))
				type.BaseTypes.Add (new CodeTypeReference (settings.Inherits));
			else if (!settings.IncludePreprocessingHelpers)
				type.BaseTypes.Add (new CodeTypeReference (typeof (TextTransformation)));
			namespac.Types.Add (type);
			
			//prep the transform method
			var transformMeth = new CodeMemberMethod () {
				Name = "TransformText",
				ReturnType = new CodeTypeReference (typeof (String)),
				Attributes = MemberAttributes.Public,
			};
			if (!settings.IncludePreprocessingHelpers)
				transformMeth.Attributes |= MemberAttributes.Override;
			
			//method references that will need to be used multiple times
			var writeMeth = new CodeMethodReferenceExpression (new CodeThisReferenceExpression (), "Write");
			var toStringMeth = new CodeMethodReferenceExpression (new CodeTypeReferenceExpression (typeof (ToStringHelper)), "ToStringWithCulture");
			bool helperMode = false;
			
			//build the code from the segments
			foreach (TemplateSegment seg in pt.Content) {
				CodeStatement st = null;
				var location = new CodeLinePragma (seg.StartLocation.FileName ?? host.TemplateFile, seg.StartLocation.Line);
				switch (seg.Type) {
				case SegmentType.Block:
					if (helperMode)
						//TODO: are blocks permitted after helpers?
						throw new ParserException ("Blocks are not permitted after helpers", seg.StartLocation);
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
					type.Members.Add (new CodeSnippetTypeMember (seg.Text) { LinePragma = location });
					helperMode = true;
					break;
				default:
					throw new InvalidOperationException ();
				}
				if (st != null) {
					if (helperMode) {
						//convert the statement into a snippet member and attach it to the top level type
						//TODO: is there a way to do this for languages that use indentation for blocks, e.g. python?
						using (var writer = new StringWriter ()) {
							settings.Provider.GenerateCodeFromStatement (st, writer, null);
							type.Members.Add (new CodeSnippetTypeMember (writer.ToString ()) { LinePragma = location });
						}
					} else {
						st.LinePragma = location;
						transformMeth.Statements.Add (st);
						continue;
					}
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
			
			//class code from processors
			foreach (var processor in settings.DirectiveProcessors.Values) {
				string classCode = processor.GetClassCodeForProcessingRun ();
				if (classCode != null)
					type.Members.Add (new CodeSnippetTypeMember (classCode));
			}
			
			//generate the Host property if needed
			if (settings.HostSpecific) {
				GenerateHostProperty (type, settings);
			}
			
			GenerateInitializationMethod (type, settings);
			
			if (settings.IncludePreprocessingHelpers)
				GenerateProcessingHelpers (type, settings);
			
			return ccu;
		}
		
		static void GenerateHostProperty (CodeTypeDeclaration type, TemplateSettings settings)
		{
			var hostField = new CodeMemberField (new CodeTypeReference (typeof (ITextTemplatingEngineHost)), "hostValue");
			hostField.Attributes = (hostField.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
			type.Members.Add (hostField);
			
			var hostProp = GenerateGetterSetterProperty ("Host", hostField);
			hostProp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			type.Members.Add (hostProp);
		}
		
		static CodeMemberProperty GenerateGetterSetterProperty (string propertyName, CodeMemberField field)
		{
			var prop = new CodeMemberProperty () {
				Name = propertyName,
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				HasGet = true,
				HasSet = true,
				Type = field.Type
			};
			var fieldRef = new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), field.Name);
			prop.SetStatements.Add (new CodeAssignStatement (fieldRef, new CodePropertySetValueReferenceExpression ()));
			prop.GetStatements.Add (new CodeMethodReturnStatement (fieldRef));
			return prop;
		}
		
		static CodeMemberProperty GenerateGetterProperty (string propertyName, CodeMemberField field)
		{
			var prop = new CodeMemberProperty () {
				Name = propertyName,
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				HasGet = true,
				HasSet = false,
				Type = field.Type
			};
			var fieldRef = new CodeFieldReferenceExpression (new CodeThisReferenceExpression (), field.Name);
			prop.GetStatements.Add (new CodeMethodReturnStatement (fieldRef));
			return prop;
		}
		
		static void GenerateInitializationMethod (CodeTypeDeclaration type, TemplateSettings settings)
		{
			//initialization method
			var initializeMeth = new CodeMemberMethod () {
				Name = "Initialize",
				ReturnType = new CodeTypeReference (typeof (void)),
				Attributes = MemberAttributes.Family 
			};
			if (!settings.IncludePreprocessingHelpers)
				initializeMeth.Attributes |= MemberAttributes.Override;
			
			//pre-init code from processors
			foreach (var processor in settings.DirectiveProcessors.Values) {
				string code = processor.GetPreInitializationCodeForProcessingRun ();
				if (code != null)
					initializeMeth.Statements.Add (new CodeSnippetStatement (code));
			}
			
			//base call
			if (!settings.IncludePreprocessingHelpers) {
				initializeMeth.Statements.Add (
					new CodeMethodInvokeExpression (
						new CodeMethodReferenceExpression (
							new CodeBaseReferenceExpression (),
							"Initialize")));
			}
			
			//post-init code from processors
			foreach (var processor in settings.DirectiveProcessors.Values) {
				string code = processor.GetPostInitializationCodeForProcessingRun ();
				if (code != null)
					initializeMeth.Statements.Add (new CodeSnippetStatement (code));
			}
			
			type.Members.Add (initializeMeth);
		}
		
		static CodeStatement ArgNullCheck (CodeExpression value, params CodeExpression[] argNullExcArgs)
		{
			return new CodeConditionStatement (
				new CodeBinaryOperatorExpression (value,
					CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression (null)),
				new CodeThrowExceptionStatement (new CodeObjectCreateExpression (typeof (ArgumentNullException), argNullExcArgs)));
		}
		
		static void GenerateProcessingHelpers (CodeTypeDeclaration type, TemplateSettings settings)
		{
			var thisRef = new CodeThisReferenceExpression ();
			var stringTypeRef = new CodeTypeReference (typeof (string));
			var intTypeRef = new CodeTypeReference (typeof (int));
			var nullPrim = new CodePrimitiveExpression (null);
			var minusOnePrim = new CodePrimitiveExpression (-1);
			var zeroPrim = new CodePrimitiveExpression (0);
			var stringEmptyRef = new CodeFieldReferenceExpression (new CodeTypeReferenceExpression (typeof (string)), "Empty");
			
			var indentsFieldName = settings.Provider.CreateValidIdentifier ("__indents");
			var currentIndentFieldName = settings.Provider.CreateValidIdentifier ("__currentIndent");
			var errorsFieldName = settings.Provider.CreateValidIdentifier ("__errors");
			var builderFieldName = settings.Provider.CreateValidIdentifier ("__builder");
			var sessionFieldName = settings.Provider.CreateValidIdentifier ("__session");
			
			var indentsField = new CodeMemberField (new CodeTypeReference (typeof (Stack<int>)), indentsFieldName) {
				Attributes = MemberAttributes.Private,
			};
			var currentIndentField = new CodeMemberField (stringTypeRef, currentIndentFieldName) {
				Attributes = MemberAttributes.Private,
			};
			var errorsField = new CodeMemberField (new CodeTypeReference (typeof (CompilerErrorCollection)), errorsFieldName) {
				Attributes = MemberAttributes.Private,
			};
			var builderField = new CodeMemberField (new CodeTypeReference (typeof (StringBuilder)), builderFieldName) {
				Attributes = MemberAttributes.Private,
			};
			var sessionField = new CodeMemberField (new CodeTypeReference (typeof (IDictionary<string,object>)), sessionFieldName) {
				Attributes = MemberAttributes.Private,
			};
			
			indentsField.InitExpression = new CodeObjectCreateExpression (indentsField.Type);
			currentIndentField.InitExpression = stringEmptyRef;
			errorsField.InitExpression = new CodeObjectCreateExpression (errorsField.Type);
			builderField.InitExpression = new CodeObjectCreateExpression (builderField.Type);
			
			var indentsFieldRef = new CodeFieldReferenceExpression (thisRef, indentsFieldName);
			var currentIndentFieldRef = new CodeFieldReferenceExpression (thisRef, currentIndentFieldName);
			var errorsFieldRef = new CodeFieldReferenceExpression (thisRef, errorsFieldName);
			var builderFieldRef = new CodeFieldReferenceExpression (thisRef, builderFieldName);
			
			var sessionProp = GenerateGetterSetterProperty ("Session", sessionField);
			sessionProp.Attributes = MemberAttributes.Public;
			
			var compilerErrorTypeRef = new CodeTypeReference (typeof (CompilerError));
			var errorMeth = new CodeMemberMethod () {
				Name = "Error",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			errorMeth.Parameters.Add (new CodeParameterDeclarationExpression (stringTypeRef, "message"));
			errorMeth.Statements.Add (new CodeMethodInvokeExpression (errorsFieldRef, "Add",
				new CodeObjectCreateExpression (compilerErrorTypeRef, nullPrim, minusOnePrim, minusOnePrim, nullPrim,
					new CodeArgumentReferenceExpression ("message"))));
			
			var warningMeth = new CodeMemberMethod () {
				Name = "Warning",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			warningMeth.Parameters.Add (new CodeParameterDeclarationExpression (stringTypeRef, "message"));
			warningMeth.Statements.Add (new CodeVariableDeclarationStatement (compilerErrorTypeRef, "val",
				new CodeObjectCreateExpression (compilerErrorTypeRef, nullPrim, minusOnePrim, minusOnePrim, nullPrim,
					new CodeArgumentReferenceExpression ("message"))));
			warningMeth.Statements.Add (new CodeAssignStatement (new CodePropertyReferenceExpression (
				new CodeVariableReferenceExpression ("val"), "IsWarning"), new CodePrimitiveExpression (true)));
			warningMeth.Statements.Add (new CodeMethodInvokeExpression (errorsFieldRef, "Add",
				new CodeVariableReferenceExpression ("val")));
			
			var errorsProp = GenerateGetterProperty ("Errors", errorsField);
			errorsProp.Attributes = MemberAttributes.Family | MemberAttributes.Final;
			
			var popIndentMeth = new CodeMemberMethod () {
				Name = "PopIndent",
				ReturnType = stringTypeRef,
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			popIndentMeth.Statements.Add (new CodeConditionStatement (
				new CodeBinaryOperatorExpression (new CodePropertyReferenceExpression (indentsFieldRef, "Count"),
					CodeBinaryOperatorType.ValueEquality, zeroPrim),
				new CodeMethodReturnStatement (stringEmptyRef)));
			popIndentMeth.Statements.Add (new CodeVariableDeclarationStatement (intTypeRef, "lastPos",
				new CodeBinaryOperatorExpression (
					new CodePropertyReferenceExpression (currentIndentFieldRef, "Length"),
					CodeBinaryOperatorType.Subtract,
					new CodeMethodInvokeExpression (indentsFieldRef, "Pop"))));
			popIndentMeth.Statements.Add (new CodeVariableDeclarationStatement (stringTypeRef, "last",
				new CodeMethodInvokeExpression (currentIndentFieldRef, "Substring", new CodeVariableReferenceExpression ("lastPos"))));
			popIndentMeth.Statements.Add (new CodeAssignStatement (currentIndentFieldRef,
				new CodeMethodInvokeExpression (currentIndentFieldRef, "Substring", zeroPrim, new CodeVariableReferenceExpression ("lastPos"))));
			popIndentMeth.Statements.Add (new CodeMethodReturnStatement (new CodeVariableReferenceExpression ("last")));
			
			var pushIndentMeth = new CodeMemberMethod () {
				Name = "PushIndent",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			pushIndentMeth.Parameters.Add (new CodeParameterDeclarationExpression (stringTypeRef, "indent"));
			pushIndentMeth.Statements.Add (new CodeMethodInvokeExpression (indentsFieldRef, "Push",
				new CodePropertyReferenceExpression (new CodeArgumentReferenceExpression ("indent"), "Length")));
			pushIndentMeth.Statements.Add (new CodeAssignStatement (currentIndentFieldRef,
				new CodeBinaryOperatorExpression (currentIndentFieldRef, CodeBinaryOperatorType.Add, new CodeArgumentReferenceExpression ("indent"))));
			
			var clearIndentMeth = new CodeMemberMethod () {
				Name = "ClearIndent",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			clearIndentMeth.Statements.Add (new CodeAssignStatement (currentIndentFieldRef, stringEmptyRef));
			clearIndentMeth.Statements.Add (new CodeMethodInvokeExpression (indentsFieldRef, "Clear"));
			
			var currentIndentProp = GenerateGetterProperty ("CurrentIndent", currentIndentField);
			
			var generationEnvironmentProp = GenerateGetterSetterProperty ("GenerationEnvironment", builderField);
			generationEnvironmentProp.SetStatements.Insert (0, ArgNullCheck (new CodePropertySetValueReferenceExpression ()));
			var genEnvPropRef = new CodePropertyReferenceExpression (thisRef, generationEnvironmentProp.Name);
			
			var textToAppendParam = new CodeParameterDeclarationExpression (stringTypeRef, "textToAppend");
			var formatParam = new CodeParameterDeclarationExpression (stringTypeRef, "format");
			var argsParam = new CodeParameterDeclarationExpression (typeof (object[]), "args");
			argsParam.CustomAttributes.Add (new CodeAttributeDeclaration ("System.ParamArrayAttribute"));
			
			var textToAppendParamRef = new CodeArgumentReferenceExpression ("textToAppend");
			var formatParamRef = new CodeArgumentReferenceExpression ("format");
			var argsParamRef = new CodeArgumentReferenceExpression ("args");
			
			var writeMeth = new CodeMemberMethod () {
				Name = "Write",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			writeMeth.Parameters.Add (textToAppendParam);
			writeMeth.Statements.Add (new CodeMethodInvokeExpression (genEnvPropRef, "Append", new CodeArgumentReferenceExpression ("textToAppend")));
			
			var writeArgsMeth = new CodeMemberMethod () {
				Name = "Write",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			writeArgsMeth.Parameters.Add (formatParam);
			writeArgsMeth.Parameters.Add (argsParam);
			writeArgsMeth.Statements.Add (new CodeMethodInvokeExpression (genEnvPropRef, "AppendFormat", formatParamRef, argsParamRef));
			
			var writeLineMeth = new CodeMemberMethod () {
				Name = "WriteLine",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			writeLineMeth.Parameters.Add (textToAppendParam);
			writeLineMeth.Statements.Add (new CodeMethodInvokeExpression (genEnvPropRef, "Append", currentIndentFieldRef));
			writeLineMeth.Statements.Add (new CodeMethodInvokeExpression (genEnvPropRef, "AppendLine", textToAppendParamRef));
			
			var writeLineArgsMeth = new CodeMemberMethod () {
				Name = "WriteLine",
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
			};
			writeLineArgsMeth.Parameters.Add (formatParam);
			writeLineArgsMeth.Parameters.Add (argsParam);
			writeLineArgsMeth.Statements.Add (new CodeMethodInvokeExpression (genEnvPropRef, "Append", currentIndentFieldRef));
			writeLineArgsMeth.Statements.Add (new CodeMethodInvokeExpression (genEnvPropRef, "AppendFormat", formatParamRef, argsParamRef));
			writeLineArgsMeth.Statements.Add (new CodeMethodInvokeExpression (genEnvPropRef, "AppendLine"));
			
			type.Members.Add (indentsField);
			type.Members.Add (currentIndentField);
			type.Members.Add (errorsField);
			type.Members.Add (builderField);
			type.Members.Add (sessionField);
			type.Members.Add (sessionProp);
			type.Members.Add (errorMeth);
			type.Members.Add (warningMeth);
			type.Members.Add (errorsProp);
			type.Members.Add (popIndentMeth);
			type.Members.Add (pushIndentMeth);
			type.Members.Add (clearIndentMeth);
			type.Members.Add (currentIndentProp);
			type.Members.Add (generationEnvironmentProp);
			type.Members.Add (writeMeth);
			type.Members.Add (writeArgsMeth);
			type.Members.Add (writeLineMeth);
			type.Members.Add (writeLineArgsMeth);
		}
	}
}
