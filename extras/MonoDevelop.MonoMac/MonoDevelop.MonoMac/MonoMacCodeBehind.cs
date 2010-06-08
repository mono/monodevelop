// 
// MonoMacCodeBehind.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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
using System.CodeDom;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using MonoDevelop.MacDev.InterfaceBuilder;
using System.CodeDom.Compiler;
using System.IO;
using MonoDevelop.MacDev;
using MonoDevelop.Projects;

namespace MonoDevelop.MonoMac
{
	public class MonoMacCodeBehind : XibCodeBehind
	{
		const string NSOBJECT = "MonoMac.Foundation.NSObject";
		
		static Dictionary<string,string> typeNameMap = new Dictionary<string, string> ();
		
		//FIXME: would prefer to look these up inside the MD type DB, if possible, instead of using reflection
		static void InitializeTypeNameMap ()
		{
			var mmdll = Mono.Addins.AddinManager.CurrentAddin.GetFilePath ("MonoMac.dll");
			var asm = System.Reflection.Assembly.LoadFile (mmdll);
			var nsobj = asm.GetType ("MonoMac.Foundation.NSObject");
			var registerAtt = asm.GetType ("MonoMac.Foundation.RegisterAttribute");
			var prop = registerAtt.GetProperty ("Name");
			foreach (var t in asm.GetTypes ()) {
				if (!t.IsSubclassOf (nsobj))
					continue;
				var attrs = t.GetCustomAttributes (registerAtt, false);
				if (attrs != null && attrs.Length == 1)  {
					var objCName = prop.GetValue (attrs[0], null) as string;
					if (objCName != null)
						typeNameMap [objCName] = t.FullName;
				}
			}
		}
		
		static MonoMacCodeBehind ()
		{
			try {
				InitializeTypeNameMap ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public MonoMacCodeBehind (MonoMacProject project) : base (project)
		{
		}
		
		public override CodeCompileUnit Generate (ProjectFile xibFile, CodeDomProvider provider, CodeGeneratorOptions options)
		{
			var xibDoc = XDocument.Load (xibFile.FilePath);
			var ibDoc = IBDocument.Deserialize (xibDoc);
			var ccu = new CodeCompileUnit ();
			
			object outVar;
			UnknownIBObject classDescriber;
			if (!ibDoc.Properties.TryGetValue ("IBDocument.Classes", out outVar) || (classDescriber = outVar as UnknownIBObject) == null)
				return ccu;
			
			NSMutableArray arr;
			if (!classDescriber.Properties.TryGetValue ("referencedPartialClassDescriptions", out outVar) || (arr = outVar as NSMutableArray) == null)
				return ccu;
			
			var project = (DotNetProject)xibFile.Project;
			var defaultNS = project.GetDefaultNamespace (xibFile.FilePath);
			var nsDict = new Dictionary<string,CodeNamespace> ();
			
			foreach (var cls in arr.Values.OfType<IBPartialClassDescription> ()) {
				if (string.IsNullOrEmpty (cls.ClassName))
					continue;
				
				var si = cls.SourceIdentifier.Value;
				if (si == null || si.MajorKey != "IBUserSource")
					continue;
				
				var type = new CodeTypeDeclaration (cls.ClassName) {
					IsPartial = true,
				};
				type.CustomAttributes.Add (
					new CodeAttributeDeclaration ("MonoMac.Foundation.Register",
						new CodeAttributeArgument (new CodePrimitiveExpression (cls.ClassName))));
				
				var sc = GetTypeReference (cls.SuperclassName ?? "id");
				type.Comments.Add (new CodeCommentStatement ("Should subclass " + sc.BaseType));
				//type.BaseTypes.Add (sc);
				
				if (cls.Actions != null) {
					foreach (var action in cls.Actions.Values) {
						AddWarningDisablePragmas (type, provider);
						StringWriter actionStubWriter = null;
						
						var sender = GetTypeReference ((string)action.Value);
						GenerateAction (type, (string)action.Key, sender, provider, options, ref actionStubWriter);
						
						if (actionStubWriter != null) {
							type.Comments.Add (new CodeCommentStatement (actionStubWriter.ToString ()));
							actionStubWriter.Dispose ();
						}
					}
				}
				
				if (cls.Outlets != null) {
					foreach (var outlet in cls.Outlets.Values) {
						AddWarningDisablePragmas (type, provider);
						AddOutletProperty (type, (string)outlet.Key, GetTypeReference ((string)outlet.Value));
					}
				}
				
				//FIXME: use _ as namespace separator
				var ns = defaultNS;
				CodeNamespace cn;
				if (!nsDict.TryGetValue (ns, out cn)) {
					cn = new CodeNamespace (ns);
					nsDict.Add (ns, cn);
					ccu.Namespaces.Add (cn);
				}
				cn.Types.Add (type);
			}
			
			return ccu;
		}
		
		//FIXME: resolve in the context of the whole project?
		CodeTypeReference GetTypeReference (string objcName)
		{
			if (objcName == "id") {
				return new CodeTypeReference (NSOBJECT) {
					Options = CodeTypeReferenceOptions.GlobalReference
				};
			}
			
			string typeName;
			if (typeNameMap.TryGetValue (objcName, out typeName)) {
				return new CodeTypeReference (typeName) {
					Options = CodeTypeReferenceOptions.GlobalReference
				};
			}
			
			return new CodeTypeReference (objcName);
		}
		
		static void AddWarningDisablePragmas (CodeTypeDeclaration type, CodeDomProvider provider)
		{
			if (type.Members.Count > 0)
				return;
			
			if (provider is Microsoft.CSharp.CSharpCodeProvider) {
				type.Members.Add (new CodeSnippetTypeMember ("#pragma warning disable 0169")); // unused member
			}
		}

		static void GenerateAction (CodeTypeDeclaration type, string name, CodeTypeReference senderType, CodeDomProvider provider,
		                            CodeGeneratorOptions generatorOptions, ref StringWriter actionStubWriter)
		{	
			if (provider is Microsoft.CSharp.CSharpCodeProvider) {
				type.Members.Add (new CodeSnippetTypeMember ("[MonoMac.Foundation.Export(\"" + name + "\")]"));
				type.Members.Add (new CodeSnippetTypeMember (
					String.Format ("partial void {1} ({2} sender);\n",
					               name, provider.CreateValidIdentifier (name.TrimEnd (':')), senderType.BaseType)));
				return;
			}
			else if (provider.FileExtension == "pas") {
				var m = new CodeMemberMethod ();
				m.Name = provider.CreateValidIdentifier (name.TrimEnd (':'));
				m.Parameters.Add (new CodeParameterDeclarationExpression (senderType.BaseType, "sender"));
				m.UserData ["OxygenePartial"] = "YES";
				m.UserData ["OxygeneEmpty"] = "YES";
				var a = new CodeAttributeDeclaration ("MonoMac.Foundation.Export");
				a.Arguments.Add (new CodeAttributeArgument (new CodePrimitiveExpression (name)));
				m.CustomAttributes.Add (a);
				type.Members.Add (m);
				return;
			}
			
			
			var meth = CreateEventMethod (name, senderType);
			
			bool actionStubWriterCreated = false;
			if (actionStubWriter == null) {
				actionStubWriterCreated = true;
				actionStubWriter = new StringWriter ();
				actionStubWriter.WriteLine ("Action method stubs:");
				actionStubWriter.WriteLine ();
			}
			try {
				provider.GenerateCodeFromMember (meth, actionStubWriter, generatorOptions);
				actionStubWriter.WriteLine ();
			} catch {
				//clear the header if generation failed
				if (actionStubWriterCreated)
					actionStubWriter = null;
			}
		}
		
		public static void AddOutletProperty (CodeTypeDeclaration type, string name, CodeTypeReference typeRef)
		{
			var fieldName = "__mt_" + name;
			var field = new CodeMemberField (typeRef, fieldName);
			
			var prop = new CodeMemberProperty () {
				Name = name,
				Type = typeRef
			};
			prop.CustomAttributes.Add (
				new CodeAttributeDeclaration ("MonoMac.Foundation.Connect",
			    		new CodeAttributeArgument (new CodePrimitiveExpression (name))));
			
			var setValue = new CodePropertySetValueReferenceExpression ();
			var thisRef = new CodeThisReferenceExpression ();
			var fieldRef = new CodeFieldReferenceExpression (thisRef, fieldName);
			var setNativeRef = new CodeMethodReferenceExpression (thisRef, "SetNativeField");
			var getNativeRef = new CodeMethodReferenceExpression (thisRef, "GetNativeField");
			var namePrimitive = new CodePrimitiveExpression (name);
			var invokeGetNative = new CodeMethodInvokeExpression (getNativeRef, namePrimitive);
			
			prop.SetStatements.Add (new CodeAssignStatement (fieldRef, setValue));
			prop.SetStatements.Add (new CodeMethodInvokeExpression (setNativeRef, namePrimitive, setValue));
			
			prop.GetStatements.Add (new CodeAssignStatement (fieldRef, new CodeCastExpression (typeRef, invokeGetNative)));
			prop.GetStatements.Add (new CodeMethodReturnStatement (fieldRef));
			
			prop.Attributes = field.Attributes = (prop.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
			
			type.Members.Add (prop);
			type.Members.Add (field);
		}
		
		public static CodeTypeMember CreateEventMethod (string name, CodeTypeReference senderType)
		{
			var meth = new CodeMemberMethod () {
				Name = name.TrimEnd (':'),
				ReturnType = new CodeTypeReference (typeof (void)),
			};
			meth.Parameters.Add (
				new CodeParameterDeclarationExpression () {
					Name = "sender",
					Type = senderType }
			);
			
			meth.CustomAttributes.Add (
				new CodeAttributeDeclaration ("MonoMac.Foundation.Export",
					new CodeAttributeArgument (new CodePrimitiveExpression (name))));
			
			return meth;
		}
		
		static object ResolveIfReference (object o)
		{
			IBReference r = o as IBReference;
			if (r != null)
				return ResolveIfReference (r.Reference);
			else
				return o;
		}
	}
}
