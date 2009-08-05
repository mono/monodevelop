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
using MonoDevelop.IPhone.InterfaceBuilder;
using System.CodeDom.Compiler;
using System.IO;

namespace MonoDevelop.IPhone
{
	
	static class CodeBehindGenerator
	{
		
		public static IEnumerable<CodeTypeDeclaration> GetTypes (XDocument xibDoc, CodeDomProvider provider, CodeGeneratorOptions generatorOptions)
		{
			var ibDoc = IBDocument.Deserialize (xibDoc);
			
			object outVar;
			UnknownIBObject objects;
			if (!ibDoc.Properties.TryGetValue ("IBDocument.Objects", out outVar) || (objects = outVar as UnknownIBObject) == null)
				return new CodeTypeDeclaration[0];
			
			//process the connection records
			NSMutableArray connectionRecords;
			if (!objects.Properties.TryGetValue ("connectionRecords", out outVar) || (connectionRecords = outVar as NSMutableArray) == null)
				return new CodeTypeDeclaration[0];
			
			//group connection records by type ref ID
			var typeRecords = new Dictionary<int,List<IBConnectionRecord>> ();
			foreach (var record in connectionRecords.Values.OfType<IBConnectionRecord> ()) {
				//get the type this member belongs in
				var ev = record.Connection as IBCocoaTouchEventConnection;
				int? typeIndex = ((IBObject)(ev != null
					? ev.Destination.Reference
					: record.Connection.Source.Reference)).Id;
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
			
			//and construct the type objects, keyed by ref ID
			var objectRecords = (IBMutableOrderedSet) objects.Properties ["objectRecords"];
			var types = new Dictionary<int,CodeTypeDeclaration> ();
			foreach (IBObjectRecord record in objectRecords.OrderedObjects.OfType<IBObjectRecord> ()) {
				string name;
				int? objId = ((IBObject)ResolveIfReference (record.Object)).Id;
				if (objId != null && classNames.TryGetValue (record.ObjectId, out name)) {
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
							var cls = ((UnknownIBObject)obj).Class;
							if (cls != "IBUICustomObject")
								baseType = GetTypeName (cls).BaseType;
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
				var actions = new List<IBCocoaTouchEventConnection> ();
				var outlets = new List<IBCocoaTouchOutletConnection> ();
				foreach (var record in typeRecord.Value) {
					var ev = record.Connection as IBCocoaTouchEventConnection;
					if (ev != null)
						actions.Add (ev);
					else
						outlets.Add (record.Connection);
				}
				
				//process the actions, grouping ones with the same name
				foreach (var actionGroup in actions.GroupBy (a => a.Label)) {
					//find a common sender type for all the items in the grouping
					CodeTypeReference senderType = null;
					foreach (IBCocoaTouchEventConnection ev in actionGroup) {
						var sender = ResolveIfReference (ev.Source) as UnknownIBObject;
						var newType = sender != null? GetTypeName (sender.Class) : new CodeTypeReference ("MonoTouch.Foundation.NSObject");
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
					
					//create the action method and add it
					bool isPartial;
					var meth = CreateEventMethod (provider, actionGroup.Key, senderType, out isPartial);
					if (isPartial) {
						type.Members.Add (meth);
					}
					else {
						var actionStubWriter = new StringWriter ();
						try {
							provider.GenerateCodeFromMember (meth, actionStubWriter, generatorOptions);
							actionStubWriter.WriteLine ();
							type.Comments.Add (new CodeCommentStatement (actionStubWriter.ToString ()));
						} catch {
							actionStubWriter = null;
							break;
						} finally {
							actionStubWriter.Dispose ();
						}
					}
				}
				
				foreach (var outlet in outlets) {
					CodeTypeReference outletType;
					//destination is widget, so get type
					var widget = outlet.Destination.Reference as UnknownIBObject;
					if (widget != null)
						outletType = GetTypeName (widget.Class);
					else
						outletType = new CodeTypeReference ("System.Object");
					
					type.Members.Add (CreateOutletProperty (outlet.Label, outletType));
				}
			}
			
			return types.Values;
		}
		
		public static IEnumerable<CodeTypeDeclaration> GetTypesOld (XDocument xibDoc, CodeDomProvider provider, CodeGeneratorOptions generatorOptions)
		{
			var ibDoc = IBDocument.Deserialize (xibDoc);
				
			object outVar;
			UnknownIBObject classDescriber;
			if (ibDoc.Properties.TryGetValue ("IBDocument.Classes", out outVar) && (classDescriber = outVar as UnknownIBObject) != null) {
				NSMutableArray arr;
				if (classDescriber.Properties.TryGetValue ("referencedPartialClassDescriptions", out outVar) && (arr = outVar as NSMutableArray) != null) {
					foreach (var clsDesc in arr.Values.OfType<IBPartialClassDescription> ()) {
						if (clsDesc.SourceIdentifier == null)
							continue;
						string majorKey = clsDesc.SourceIdentifier.MajorKey;
						if (majorKey != "IBUserSource" && majorKey != "IBProjectSource")
							continue;
						
						var type = new CodeTypeDeclaration (clsDesc.ClassName) {
							IsPartial = true
						};
						
						type.CustomAttributes.Add (
							new CodeAttributeDeclaration ("MonoTouch.Foundation.Register",
								new CodeAttributeArgument (new CodePrimitiveExpression (clsDesc.ClassName))));
						
						if (!String.IsNullOrEmpty (clsDesc.SuperclassName))
							type.BaseTypes.Add ("MonoTouch.UIKit." + clsDesc.SuperclassName);
						else
							type.BaseTypes.Add ("MonoTouch.UIKit.UIResponder");
						
						if (clsDesc.Outlets != null)
							foreach (KeyValuePair<object,object> outlet in clsDesc.Outlets.Values)
								type.Members.Add (CreateOutletProperty ((string)outlet.Key, new CodeTypeReference ((string)outlet.Value)));
						
						if (clsDesc.Actions != null) {
							StringWriter actionStubWriter = null;
							foreach (KeyValuePair<object,object> action in clsDesc.Actions.Values) {
								bool isPartial;
								//FIXME: can we strongly type this?
								var meth = CreateEventMethod (provider, (string)action.Key,
								                              new CodeTypeReference ("MonoTouch.Foundation.NSObject"),
								                              out isPartial);
								if (isPartial) {
									type.Members.Add (meth);
								} else {
									if (actionStubWriter == null) {
										actionStubWriter = new StringWriter ();
										actionStubWriter.WriteLine ("Action method stubs:");
										actionStubWriter.WriteLine ();
									}
									try {
										provider.GenerateCodeFromMember (meth, actionStubWriter, generatorOptions);
										actionStubWriter.WriteLine ();
									} catch {
										actionStubWriter = null;
										break;
									}
								}
							}
							if (actionStubWriter != null) {
								type.Comments.Add (new CodeCommentStatement (actionStubWriter.ToString ()));
								actionStubWriter.Dispose ();
							}
						}
						
						yield return type;
					}
				}
			}
		}
		
		static CodeTypeReference GetTypeName (string ibType)
		{
			string name;
			if (ibType.StartsWith ("NS"))
				name = "MonoTouch.Foundation." + ibType;
			else if (ibType.StartsWith ("IB") && ibType.Length > 2)
				name = "MonoTouch.UIKit." + ibType.Substring (2);
			else
				name = "MonoTouch.Foundation.NSObject";
			return new CodeTypeReference (name);
		}
		
		public static CodeMemberProperty CreateOutletProperty (string name, CodeTypeReference typeRef)
		{
			var prop = new CodeMemberProperty () {
				Name = name,
				Type = typeRef
			};
			prop.CustomAttributes.Add (
				new CodeAttributeDeclaration ("MonoTouch.Foundation.Connect",
			    		new CodeAttributeArgument (new CodePrimitiveExpression (name))));
			prop.SetStatements.Add (
				new CodeMethodInvokeExpression (
					new CodeMethodReferenceExpression (
						new CodeThisReferenceExpression (), "SetNativeField"),
						new CodePrimitiveExpression (name),
						new CodePropertySetValueReferenceExpression ()));
			prop.GetStatements.Add (
				new CodeMethodReturnStatement (
					new CodeCastExpression (typeRef,
						new CodeMethodInvokeExpression (
							new CodeMethodReferenceExpression (
								new CodeThisReferenceExpression (), "GetNativeField"),
								new CodePrimitiveExpression (name)))));
			prop.Attributes = (prop.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Family;
			return prop;
		}
		
		public static CodeTypeMember CreateEventMethod (CodeDomProvider provider, string name, CodeTypeReference senderType, out bool isPartial)
		{
			if (provider is Microsoft.CSharp.CSharpCodeProvider) {
				isPartial = true;
				return new CodeSnippetTypeMember (
					String.Format ("[MonoTouch.Foundation.Export(\"{0}\")]partial void {1} ({2} sender);\n",
					               name, provider.CreateValidIdentifier (name.Substring (0, name.Length - 1)), senderType.BaseType));
			}
			
			isPartial = false;
			var meth = new CodeMemberMethod () {
				Name = name.Trim (':'),
				ReturnType = new CodeTypeReference ("partial void"),
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
