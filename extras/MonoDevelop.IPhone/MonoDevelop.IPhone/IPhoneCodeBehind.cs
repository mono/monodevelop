// 
// CodeBehindGenerator.cs
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

namespace MonoDevelop.IPhone
{
	public class IPhoneCodeBehind : XibCodeBehind
	{
		static Dictionary<string,string> typeNameMap = new Dictionary<string, string> ();
		
		//FIXME: would prefer to look these up inside the MD type DB, if possible, instead of using reflection
		static void InitializeTypeNameMap ()
		{
			var asm = System.Reflection.Assembly.LoadFile ("/Developer/MonoTouch/usr/lib/mono/2.1/monotouch.dll");
			var nsobj = asm.GetType ("MonoTouch.Foundation.NSObject");
			var registerAtt = asm.GetType ("MonoTouch.Foundation.RegisterAttribute");
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
		
		static IPhoneCodeBehind ()
		{
			try {
				InitializeTypeNameMap ();
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		public IPhoneCodeBehind (IPhoneProject project) : base (project)
		{
		}
		
		public override CodeCompileUnit Generate (ProjectFile xibFile, CodeDomProvider provider, CodeGeneratorOptions options)
		{
			var doc = XDocument.Load (xibFile.FilePath);
			var ibDoc = IBDocument.Deserialize (doc);
			var project = (DotNetProject)xibFile.Project;
			var ccu = new CodeCompileUnit ();
			var ns = new CodeNamespace (project.GetDefaultNamespace (xibFile.FilePath));
			ccu.Namespaces.Add (ns);
			foreach (var ctd in GetTypes (ibDoc, provider, options))
				ns.Types.Add (ctd);
			return ccu;
		}
		
		IEnumerable<CodeTypeDeclaration> GetTypes (IBDocument doc, CodeDomProvider provider, CodeGeneratorOptions options)
		{
			object outVar;
			UnknownIBObject objects;
			if (!doc.Properties.TryGetValue ("IBDocument.Objects", out outVar) || (objects = outVar as UnknownIBObject) == null)
				return new CodeTypeDeclaration[0];
			
			//process the connection records
			NSMutableArray connectionRecords;
			if (!objects.Properties.TryGetValue ("connectionRecords", out outVar) || (connectionRecords = outVar as NSMutableArray) == null)
				return new CodeTypeDeclaration[0];
			
			//group connection records by type ref ID
			var typeRecords = new Dictionary<int,List<IBConnectionRecord>> ();
			foreach (var record in connectionRecords.Values.OfType<IBConnectionRecord> ()) {
				//get the type this member belongs in
				var ev = record.Connection as IBActionConnection;
				var outlet = record.Connection as IBOutletConnection;
				if (outlet == null && ev == null) {
					//not a recognised connection type. probably a desktop xib
					continue;
				}
				int? typeIndex = ((IBObject)(ev != null
					? ev.Destination.Reference
					: outlet.Source.Reference)).Id;
				if (typeIndex == null)
					throw new InvalidOperationException ("Connection " + record.ConnectionId + " references null object ID");
				List<IBConnectionRecord> records;
				if (!typeRecords.TryGetValue (typeIndex.Value, out records))
					typeRecords[typeIndex.Value] = records = new List<IBConnectionRecord> ();
				records.Add (record);
			}
			
			//grab the custom class names, keyed by object ID
			var classNames = new Dictionary<int, string> ();
			var flattenedProperties = (NSMutableDictionary) objects.Properties ["flattenedProperties"];
			foreach (var pair in flattenedProperties.Values) {
				string keyStr = (string)pair.Key;
				if (!keyStr.EndsWith (".CustomClassName"))
					continue;
				int key = int.Parse (keyStr.Substring (0, keyStr.IndexOf ('.')));
				string name = (string)pair.Value;
				
				//HACK: why does IB not generate partial classes for UIApplication or UIResponder? I guess we should suppress them too
				if (name == "UIApplication" || name == "UIResponder")
					continue;
				
				classNames[key] = (string)pair.Value;
			}
			
			// it seems to be hard to figure out which objects we should generate classes for,
			// so take the list of classes that xcode would generate
			var ibApprovedPartialClassNames = new HashSet<string> ();
			UnknownIBObject classDescriber;
			if (doc.Properties.TryGetValue ("IBDocument.Classes", out outVar) && (classDescriber = outVar as UnknownIBObject) != null) {
				NSMutableArray arr;
				if (classDescriber.Properties.TryGetValue ("referencedPartialClassDescriptions", out outVar) && (arr = outVar as NSMutableArray) != null) {
					foreach (var cls in arr.Values.OfType<IBPartialClassDescription> ())
						if (!String.IsNullOrEmpty (cls.ClassName))
						    ibApprovedPartialClassNames.Add (cls.ClassName);
				}
			}
			
			// construct the type objects, keyed by ref ID
			var objectRecords = (IBMutableOrderedSet) objects.Properties ["objectRecords"];
			var customTypeNames = new Dictionary<int,string> ();
			var types = new Dictionary<int,CodeTypeDeclaration> ();
			foreach (IBObjectRecord record in objectRecords.OrderedObjects.OfType<IBObjectRecord> ()) {
				string name;
				int? objId = ((IBObject)ResolveIfReference (record.Object)).Id;
				if (objId != null && classNames.TryGetValue (record.ObjectId, out name)) {
					
					customTypeNames[objId.Value] = name;
					
					if (!ibApprovedPartialClassNames.Contains (name))
						continue;
					
					//HACK to avoid duplicate class definitions, which is not compilable
					ibApprovedPartialClassNames.Remove (name);
					
					var type = new CodeTypeDeclaration (name) {
						IsPartial = true
					};
					type.CustomAttributes.Add (
						new CodeAttributeDeclaration ("MonoTouch.Foundation.Register",
							new CodeAttributeArgument (new CodePrimitiveExpression (name))));
					
					//FIXME: implement proper base class resolution. I'm not sure where the info is - it might need some
					// inference rules
					
					var obj = ResolveIfReference (record.Object);
					if (obj != null) {
						string baseType = "MonoTouch.Foundation.NSObject";
						if (obj is IBProxyObject) {
							baseType = "MonoTouch.UIKit.UIViewController";
						} else if (obj is UnknownIBObject) {
							var uobj = (UnknownIBObject)obj;
							
							//if the item comes from another nib, don't generate the partial class in this xib's codebehind
							if (uobj.Properties.ContainsKey ("IBUINibName") && !String.IsNullOrEmpty (uobj.Properties["IBUINibName"] as string))
								continue;
							
							baseType = GetTypeName (null, uobj) ?? "MonoTouch.Foundation.NSObject";
						}
						type.Comments.Add (new CodeCommentStatement (String.Format ("Base type probably should be {0} or subclass", baseType))); 
					}
					
					types.Add (objId.Value, type);
				}
			}
			
			foreach (KeyValuePair<int,List<IBConnectionRecord>> typeRecord in typeRecords) {
				CodeTypeDeclaration type;
				if (!types.TryGetValue (typeRecord.Key, out type))
					continue;
				
				//separate out the actions and outlets
				var actions = new List<IBActionConnection> ();
				var outlets = new List<IBOutletConnection> ();
				foreach (var record in typeRecord.Value) {
					if (record.Connection is IBActionConnection)
						actions.Add ((IBActionConnection)record.Connection);
					else if (record.Connection is IBOutletConnection)
						outlets.Add ((IBOutletConnection)record.Connection);
				}
				
				//process the actions, grouping ones with the same name
				foreach (var actionGroup in actions.GroupBy (a => a.Label)) {
					//find a common sender type for all the items in the grouping
					CodeTypeReference senderType = null;
					foreach (IBActionConnection ev in actionGroup) {
						var sender = ResolveIfReference (ev.Source) as IBObject;
						var newType = new CodeTypeReference (GetTypeName (customTypeNames, sender) ?? "MonoTouch.Foundation.NSObject");
						if (senderType == null) {
							senderType = newType;
							continue;
						} else if (senderType == newType) {
							continue;
						} else {
							//FIXME: resolve common type
							newType = new CodeTypeReference ("MonoTouch.Foundation.NSObject");
							break;
						}	
					}
					
					if (type.Members.Count == 0)
						AddWarningDisablePragmas (type, provider);
					
					//create the action method and add it
					StringWriter actionStubWriter = null;
					GenerateAction (type, actionGroup.Key, senderType, provider, options, ref actionStubWriter);
					if (actionStubWriter != null) {
						type.Comments.Add (new CodeCommentStatement (actionStubWriter.ToString ()));
						actionStubWriter.Dispose ();
					}
				}
				
				foreach (var outlet in outlets) {
					CodeTypeReference outletType;
					//destination is widget, so get type
					var widget = ResolveIfReference (outlet.Destination.Reference) as IBObject;
					outletType = new CodeTypeReference (GetTypeName (customTypeNames, widget) ?? "System.Object");
					
					if (type.Members.Count == 0)
						AddWarningDisablePragmas (type, provider);
					AddOutletProperty (type, outlet.Label, outletType);
				}
			}
			
			return types.Values;
		}
		
		static void AddWarningDisablePragmas (CodeTypeDeclaration type, CodeDomProvider provider)
		{
			if (provider is Microsoft.CSharp.CSharpCodeProvider) {
				type.Members.Add (new CodeSnippetTypeMember ("#pragma warning disable 0169")); // unused member
			}
		}

		static void GenerateAction (CodeTypeDeclaration type, string name, CodeTypeReference senderType, CodeDomProvider provider,
		                            CodeGeneratorOptions generatorOptions, ref StringWriter actionStubWriter)
		{	
			if (provider is Microsoft.CSharp.CSharpCodeProvider) {
				type.Members.Add (new CodeSnippetTypeMember ("[MonoTouch.Foundation.Export(\"" + name + "\")]"));
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
				var a = new CodeAttributeDeclaration ("MonoTouch.Foundation.Export");
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
		
		static string GetTypeName (Dictionary<int,string> customTypeNames, IBObject obj)
		{
			string name = null;
			
			// First resolve custom classes. Seems Apple also uses these for some framework classes,
			// maybe for classes without direct desktop equivalents?
			if (obj != null && obj.Id.HasValue && customTypeNames != null)
				customTypeNames.TryGetValue (obj.Id.Value, out name);
			
			//else, try to handle the interface builder built-in types
			if (name == null && obj is UnknownIBObject) {
				string ibType = ((UnknownIBObject)obj).Class;
				if (ibType.StartsWith ("NS")) {
					name = ibType;
				} else if (ibType.StartsWith ("IB") && ibType.Length > 2 && ibType != "IBUICustomObject") {
					name = ibType.Substring (2);
				}
			}
			
			//now try to resolve the obj-c name to a fully qualified class
			if (name != null) {
				string resolvedName;
				if (typeNameMap.TryGetValue (name, out resolvedName))
					return resolvedName;
			}
			
			return name;
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
				new CodeAttributeDeclaration ("MonoTouch.Foundation.Connect",
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
				new CodeAttributeDeclaration ("MonoTouch.Foundation.Export",
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
