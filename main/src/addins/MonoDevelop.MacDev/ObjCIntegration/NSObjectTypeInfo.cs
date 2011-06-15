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

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectTypeInfo
	{
		public NSObjectTypeInfo (string objcName, string cliName, string baseObjCName, string baseCliName, bool isModel)
		{
			this.ObjCName = objcName;
			this.CliName = cliName;
			this.BaseObjCType = baseObjCName;
			this.BaseCliType = baseCliName;
			this.IsModel = isModel;
			
			Outlets = new List<IBOutlet> ();
			Actions = new List<IBAction> ();
			UserTypeReferences = new HashSet<string> ();
		}
		
		public string ObjCName { get; private set; }
		public string CliName { get; private set; }
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
		
		public void GenerateObjcType (string directory)
		{
			if (IsModel)
				throw new ArgumentException ("Cannot generate definition for model");
			
			string hFilePath = System.IO.Path.Combine (directory, ObjCName + ".h");
			string mFilePath = System.IO.Path.Combine (directory, ObjCName + ".m");
			
			using (var sw = System.IO.File.CreateText (hFilePath)) {
				sw.WriteLine (modificationWarning);
				sw.WriteLine ();
				
				//FIXME: fix these imports for MonoMac
				sw.WriteLine ("#import <UIKit/UIKit.h>");
				foreach (var reference in UserTypeReferences) {
					sw.WriteLine ("#import \"{0}.h\"", reference);
				}
				sw.WriteLine ();
				
				var baseType = (BaseIsModel || BaseObjCType == null) ? "NSObject" : BaseObjCType;
				sw.WriteLine ("@interface {0} : {1} {{", ObjCName, baseType);
				foreach (var outlet in Outlets) {
					sw.WriteLine ("\t{0} *_{1};", AsId (outlet.ObjCType), outlet.ObjCName);
				}
				sw.WriteLine ("}");
				sw.WriteLine ();
				
				foreach (var outlet in Outlets) {
					sw.WriteLine ("@property (nonatomic, retain) IBOutlet {0} *{1};", AsId (outlet.ObjCType), outlet.ObjCName);
					sw.WriteLine ();
				}
				
				foreach (var action in Actions) {
					if (action.Parameters.Any (p => p.ObjCType == null))
						continue;
					WriteActionSignature (action, sw);
					sw.WriteLine (";");
					sw.WriteLine ();
				}
				
				sw.WriteLine ("@end");
			}
			
			using (var sw = System.IO.File.CreateText (mFilePath)) {
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
					a.CliName = existing.CliName;
					if (!existing.IsDesigner)
						continue;
				} else {
					a.CliName = a.ObjCName;
				}
				a.IsDesigner = true;
			}
			
			foreach (var o in Outlets) {
				IBOutlet existing;
				if (existingOutlets.TryGetValue (o.ObjCName, out existing)) {
					o.CliName = existing.CliName;
					if (!existing.IsDesigner)
						continue;
				} else {
					o.CliName = o.ObjCName;
				}
				o.IsDesigner = true;
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
	}
}