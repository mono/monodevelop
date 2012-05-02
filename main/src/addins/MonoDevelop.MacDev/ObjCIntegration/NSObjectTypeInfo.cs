// 
// NSObjectInfo.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectTypeInfo
	{
		public NSObjectTypeInfo (string objcName, string cliName, string baseObjCName, string baseCliName, bool isModel, bool isUserType, bool isRegisteredInDesigner)
		{
			IsRegisteredInDesigner = isRegisteredInDesigner;
			BaseObjCType = baseObjCName;
			BaseCliType = baseCliName;
			IsUserType = isUserType;
			ObjCName = objcName;
			CliName = cliName;
			IsModel = isModel;
			
			UserTypeReferences = new HashSet<string> ();
			Outlets = new List<IBOutlet> ();
			Actions = new List<IBAction> ();
		}
		
		public string ObjCName { get; private set; }
		public string CliName { get; internal set; }
		public bool IsModel { get; internal set; }
		
		public string BaseObjCType { get; internal set; }
		public string BaseCliType { get; internal set; } 
		public bool BaseIsModel { get; internal set; }
		
		public bool IsUserType { get; internal set; }
		public bool IsRegisteredInDesigner { get; internal set; }
		
		public List<IBOutlet> Outlets { get; private set; }
		public List<IBAction> Actions { get; private set; }
		
		public string[] DefinedIn { get; internal set; }
		
		public string GetDesignerFile ()
		{
			if (DefinedIn != null)
				foreach (var d in DefinedIn)
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (d))
						return d;
			return null;
		}
		
		public HashSet<string> UserTypeReferences { get; private set; }
		
		static void AddNamespaceForCliType (HashSet<string> namespaces, string ignore, string typeName)
		{
			string ns;
			int dot;
			
			if (typeName == null)
				return;
			
			if ((dot = typeName.LastIndexOf ('.')) == -1)
				return;
			
			ns = typeName.Substring (0, dot);
			if (ns != ignore && !namespaces.Contains (ns))
				namespaces.Add (ns);
		}
		
		public HashSet<string> GetNamespaces ()
		{
			HashSet<string> namespaces = new HashSet<string> ();
			string ignore = null;
			int dot;
			
			if ((dot = CliName.LastIndexOf ('.')) != -1)
				ignore = CliName.Substring (0, dot);
			
			AddNamespaceForCliType (namespaces, ignore, BaseCliType);
			
			foreach (var outlet in Outlets)
				AddNamespaceForCliType (namespaces, ignore, outlet.CliType);
			
			foreach (var action in Actions) {
				foreach (var param in action.Parameters)
					AddNamespaceForCliType (namespaces, ignore, param.CliType);
			}
			
			return namespaces;
		}
		
		static string GetSuggestedRegisterName (string fullName)
		{
			int dot = fullName.LastIndexOf ('.');
			if (dot == -1)
				return fullName;
			
			return fullName.Substring (dot + 1);
		}
		
		public void GenerateObjcType (string directory, string[] frameworks)
		{
			if (IsModel) {
				// We don't generate header files for protocols.
				return;
			}
			
			string hFilePath = Path.Combine (directory, ObjCName + ".h");
			string mFilePath = Path.Combine (directory, ObjCName + ".m");
			
			using (var sw = File.CreateText (hFilePath)) {
				sw.WriteLine (modificationWarning);
				sw.WriteLine ();
				
				foreach (var framework in frameworks)
					sw.WriteLine ("#import <{0}/{0}.h>", framework);
				
				sw.WriteLine ();
				
				foreach (var reference in UserTypeReferences)
					sw.WriteLine ("#import \"{0}.h\"", reference);
				
				sw.WriteLine ();
				
				if (BaseObjCType == null && BaseCliType != null && !BaseIsModel) {
					throw new ObjectiveCGenerationException (string.Format (
						"Could not generate class '{0}' as its base type '{1}' could not be resolved to Objective-C.\n\n" +
						"Hint: Try adding [Register (\"{2}\")] to the class definition for {1}.",
						CliName, BaseCliType, GetSuggestedRegisterName (BaseCliType)), this);
				}
				
				var baseType = BaseIsModel ? "NSObject" : BaseObjCType;
				sw.WriteLine ("@interface {0} : {1} {{", ObjCName, baseType);
				foreach (var outlet in Outlets) {
					sw.WriteLine ("\t{0} *_{1};", AsId (outlet.ObjCType), outlet.ObjCName);
				}
				sw.WriteLine ("}");
				sw.WriteLine ();
				
				foreach (var outlet in Outlets) {
					var type = AsId (outlet.ObjCType);
					if (string.IsNullOrEmpty (type)) {
						throw new ObjectiveCGenerationException (string.Format (
							"Could not generate outlet '{0}' in class '{1}' as its type '{2}' could not be resolved to Objective-C.\n\n" +
							"Hint: Try adding [Register (\"{3}\")] to the class definition for {2}.",
							outlet.CliName, this.CliName, outlet.CliType, GetSuggestedRegisterName (outlet.CliType)), this);
					}
					sw.WriteLine ("@property (nonatomic, retain) IBOutlet {0} *{1};", type, outlet.ObjCName);
					sw.WriteLine ();
				}
				
				foreach (var action in Actions) {
					WriteActionSignature (action, sw);
					sw.WriteLine (";");
					sw.WriteLine ();
				}
				
				sw.WriteLine ("@end");
			}
			
			using (var sw = File.CreateText (mFilePath)) {
				sw.WriteLine (modificationWarning);
				sw.WriteLine ();
				
				sw.WriteLine ("#import \"{0}.h\"", ObjCName);
				sw.WriteLine ();
				
				sw.WriteLine ("@implementation {0}", ObjCName);
				sw.WriteLine ();
				
				bool hasOutlet = false;
				foreach (var outlet in Outlets) {
					sw.WriteLine ("@synthesize {0} = _{0};", outlet.ObjCName);
					hasOutlet = true;
				}
				if (hasOutlet)
					sw.WriteLine ();
				
				foreach (var action in Actions) {
					if (action.Parameters.Any (p => p.ObjCType == null))
						continue;
					WriteActionSignature (action, sw);
					sw.WriteLine (" {");
					sw.WriteLine ("}");
					sw.WriteLine ();
				}
				
				sw.WriteLine ("@end");
			}

			var lastSourceUpdateTime = DefinedIn.Max (f => File.GetLastWriteTime (f));
			File.SetLastWriteTime (hFilePath, lastSourceUpdateTime);
			File.SetLastWriteTime (mFilePath, lastSourceUpdateTime);
		}
		
		static string AsId (string objcType)
		{
			if (objcType == "NSObject")
				return "id";
			return objcType;
		}
		
		static string modificationWarning =
			"// WARNING\n" +
			"// This file has been generated automatically by MonoDevelop to\n" +
			"// mirror C# types. Changes in this file made by drag-connecting\n" +
			"// from the UI designer will be synchronized back to C#, but\n" +
			"// more complex manual changes may not transfer correctly.\n";
		
		void WriteActionSignature (IBAction action, System.IO.TextWriter writer)
		{
			writer.Write ("- (IBAction){0}", action.ObjCName);
			bool isFirst = true;
			
			foreach (var param in action.Parameters) {
				string paramType = param.ObjCType;
				if (paramType == null) {
					throw new ObjectiveCGenerationException (string.Format (
						"Could not generate Obj-C code for action '{0}' in class '{1}' as the type '{2}'" +
						 "of its parameter '{3}' could not be resolved to Obj-C",
						action.CliName, this.CliName, param.CliType, param.Name), this);
					
				}
				if (isFirst && paramType == "NSObject")
					paramType = "id";
				else
					paramType = paramType + " *";
				
				if (isFirst) {
					isFirst = false;
					writer.Write (":({0}){1}", paramType, param.Name);
				} else {
					writer.Write (" {0}:({1}){2}", param.Label, paramType, param.Name);
				}
			}	
		}
		
		/// <summary>
		/// Merges CLI info from previous version of the type into the parsed objc-type.
		/// </summary>
		public void MergeCliInfo (NSObjectTypeInfo previousType)
		{
			CliName = previousType.CliName;
			DefinedIn = previousType.DefinedIn;
			IsModel = previousType.IsModel;
			BaseIsModel = previousType.BaseIsModel;
			IsUserType = previousType.IsUserType;
			IsRegisteredInDesigner = previousType.IsRegisteredInDesigner;
			
			var existingOutlets = new Dictionary<string,IBOutlet> ();
			foreach (var o in previousType.Outlets)
				existingOutlets[o.ObjCName] = o;
			
			var existingActions = new Dictionary<string,IBAction> ();
			foreach (var a in previousType.Actions)
				existingActions[a.ObjCName] = a;
			
			foreach (var a in Actions) {
				IBAction existing;
				if (existingActions.TryGetValue (a.ObjCName, out existing)) {
					a.IsDesigner = existing.IsDesigner;
					a.CliName = existing.CliName;
				}
			}
			
			foreach (var o in Outlets) {
				IBOutlet existing;
				if (existingOutlets.TryGetValue (o.ObjCName, out existing)) {
					o.IsDesigner = existing.IsDesigner;
					o.CliName = existing.CliName;
				}
			}
		}
		
		public void GenerateCodeTypeDeclaration (CodeDomProvider provider, CodeGeneratorOptions generatorOptions,
			string wrapperName, out CodeTypeDeclaration ctd, out string ns)
		{
			var registerAtt = new CodeTypeReference (wrapperName + ".Foundation.RegisterAttribute");
			
			ctd = new System.CodeDom.CodeTypeDeclaration () {
				IsPartial = true,
			};
			
			if (Outlets.Any (o => o.IsDesigner) || Actions.Any (a => a.IsDesigner))
				AddWarningDisablePragmas (ctd, provider);
			
			var dotIdx = CliName.LastIndexOf ('.');
			if (dotIdx > 0) {
				ns = CliName.Substring (0, dotIdx);
				ctd.Name = CliName.Substring (dotIdx + 1);
			} else {
				ctd.Name = CliName;
				ns = null;
			}
			if (IsRegisteredInDesigner)
				AddAttribute (ctd.CustomAttributes, registerAtt, ObjCName);
			
			GenerateActionsOutlets (provider, ctd, wrapperName);
		}
		
		void GenerateActionsOutlets (CodeDomProvider provider, CodeTypeDeclaration type, string wrapperName)
		{
			var outletAtt = new CodeTypeReference (wrapperName + ".Foundation.OutletAttribute");
			var actionAtt = new CodeTypeReference (wrapperName + ".Foundation.ActionAttribute");
			
			foreach (var a in Actions)
				if (a.IsDesigner)
					GenerateAction (actionAtt, type, a, provider);
			
			foreach (var o in Outlets)
				if (o.IsDesigner)
					AddOutletProperty (outletAtt, type, o.CliName, new CodeTypeReference (o.CliType));
		}
		
		static void AddOutletProperty (CodeTypeReference outletAtt, CodeTypeDeclaration type, string name,
			CodeTypeReference typeRef)
		{
			var fieldName = "__impl_" + name;
			var field = new CodeMemberField (typeRef, fieldName);
			
			var prop = new CodeMemberProperty () {
				Name = name,
				Type = typeRef
			};
			AddAttribute (prop.CustomAttributes, outletAtt, name);
			
			var setValue = new CodePropertySetValueReferenceExpression ();
			var thisRef = new CodeThisReferenceExpression ();
			var fieldRef = new CodeFieldReferenceExpression (thisRef, fieldName);
			
			prop.SetStatements.Add (new CodeAssignStatement (fieldRef, setValue));
			prop.GetStatements.Add (new CodeMethodReturnStatement (fieldRef));
			
			prop.Attributes = field.Attributes = (prop.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
			
			type.Members.Add (prop);
			type.Members.Add (field);
		}
		
		static void AddAttribute (CodeAttributeDeclarationCollection atts, CodeTypeReference type, string val)
		{
			atts.Add (new CodeAttributeDeclaration (type, new CodeAttributeArgument (new CodePrimitiveExpression (val))));
		}
		
		static void AddWarningDisablePragmas (CodeTypeDeclaration type, CodeDomProvider provider)
		{
			if (provider is Microsoft.CSharp.CSharpCodeProvider) {
				type.Members.Add (new CodeSnippetTypeMember ("#pragma warning disable 0169")); // unused member
			}
		}
		
		static void GenerateAction (CodeTypeReference actionAtt, CodeTypeDeclaration type, IBAction action, 
			CodeDomProvider provider)
		{
			var m = CreateEventMethod (actionAtt, action);
			type.Members.Add (m);
			
			if (provider.FileExtension == "pas") {
				m.UserData ["OxygenePartial"] = "YES";
				m.UserData ["OxygeneEmpty"] = "YES";
			}
		}
		
		static void GenerateReleaseDesignerOutletsMethod (CodeTypeDeclaration type)
		{
			var thisRef = new CodeThisReferenceExpression ();
			var nullRef = new CodePrimitiveExpression (null);
			
			var meth = new CodeMemberMethod () {
				Name = "ReleaseDesignerOutlets",
			};
			
			foreach (var outlet in type.Members.OfType<CodeMemberProperty> ()) {
				var propRef = new CodePropertyReferenceExpression (thisRef, outlet.Name);
				meth.Statements.Add (
					new CodeConditionStatement (
						new CodeBinaryOperatorExpression (propRef, CodeBinaryOperatorType.IdentityInequality, nullRef),
						new CodeExpressionStatement (new CodeMethodInvokeExpression (propRef, "Dispose")),
						new CodeAssignStatement (propRef, nullRef)
					)
				);
			}
			
			type.Members.Add (meth);
		}
		
		public static CodeTypeMember CreateEventMethod (CodeTypeReference exportAtt, IBAction action)
		{
			var meth = new CodeMemberMethod () {
				Name = action.CliName,
				ReturnType = new CodeTypeReference (typeof (void)),
			};
			foreach (var p in action.Parameters) {
				meth.Parameters.Add (new CodeParameterDeclarationExpression () {
					Name = p.Name,
					Type = new CodeTypeReference (p.ObjCType)
				});
			}
			AddAttribute (meth.CustomAttributes, exportAtt, action.GetObjcFullName ());
			
			return meth;
		}
		
		public override string ToString ()
		{
			return string.Format ("[NSObjectTypeInfo: ObjCName={0}, CliName={1}, IsModel={2}, BaseObjCType={3}, BaseCliType={4}, BaseIsModel={5}, IsUserType={6}, IsRegisteredInDesigner={7}, Outlets={8}, Actions={9}, DefinedIn={10}, UserTypeReferences={11}]", ObjCName, CliName, IsModel, BaseObjCType, BaseCliType, BaseIsModel, IsUserType, IsRegisteredInDesigner, Outlets, Actions, DefinedIn, UserTypeReferences);
		}
	}
	
	class ObjectiveCGenerationException : Exception
	{
		NSObjectTypeInfo typeInfo;
		
		public ObjectiveCGenerationException (string message, NSObjectTypeInfo typeInfo) : base (message)
		{
			this.typeInfo = typeInfo;
		}
		
		public NSObjectTypeInfo TypeInfo { get { return typeInfo; } }
	}
}
