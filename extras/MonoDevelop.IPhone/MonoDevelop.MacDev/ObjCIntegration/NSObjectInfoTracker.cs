// 
// NSObjectInfoTracker.cs
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
using System.IO;
using System.Linq;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectInfoTracker
	{
		Dictionary<string,NSObjectTypeInfo> objcTypes = new Dictionary<string,NSObjectTypeInfo> ();
		Dictionary<string,NSObjectTypeInfo> cliTypes = new Dictionary<string,NSObjectTypeInfo> ();
		
		IReturnType nsobjectType;
		IReturnType registerAttType;
		IReturnType connectAttType;
		IReturnType exportAttType;
		IReturnType modelAttType;
		DotNetProject project;
		
		public NSObjectInfoTracker (DotNetProject project, string wrapperRootNamespace)
		{
			this.project = project;
			
			string foundation = wrapperRootNamespace + ".Foundation";
			connectAttType = new DomReturnType (foundation, "ConnectAttribute");
			exportAttType = new DomReturnType (foundation, "ExportAttribute");
			registerAttType = new DomReturnType (foundation, "RegisterAttribute");
			modelAttType = new DomReturnType (foundation, "ModelAttribute");
			nsobjectType = new DomReturnType (foundation, "NSObject");
			
			var dom = ProjectDomService.GetProjectDom (project);
			foreach (var type in GetRegisteredObjects (dom)) {
				if (objcTypes.ContainsKey (type.ObjCName))
					Console.WriteLine ("Duplicate obj-c type '{0}'", type.ObjCName);
				else
					objcTypes.Add (type.ObjCName, type);
				if (cliTypes.ContainsKey (type.CliName))
					Console.WriteLine ("Duplicate CLI type '{0}'", type.CliName);
				else
					cliTypes.Add (type.CliName, type);
			}
			
			foreach (var type in objcTypes.Values) {
				ResolveTypes (dom, type);
			}
		}
		
		void ResolveTypes (ProjectDom dom, NSObjectTypeInfo type)
		{
			NSObjectTypeInfo resolved;
			
			if (type.BaseObjCType == null && type.BaseCliType != null) {
				if (cliTypes.TryGetValue (type.BaseCliType, out resolved)) {
					if (resolved.IsModel)
						type.BaseIsModel = true;
					type.BaseObjCType = resolved.ObjCName;
					if (resolved.IsUserType)
						type.UserTypeReferences.Add (resolved.ObjCName);
				} else {
					//managed classes many have implicitly registered base classes with a name not
					//expressible in obj-c. In this case, the best we can do is walk down the 
					//hierarchy until we find a valid base class
					foreach (var bt in dom.GetInheritanceTree (dom.GetType (type.BaseCliType))) { 
						if (cliTypes.TryGetValue (bt.FullName, out resolved)) {
							if (resolved.IsModel)
								type.BaseIsModel = true;
							type.BaseObjCType = resolved.ObjCName;
							if (resolved.IsUserType)
								type.UserTypeReferences.Add (resolved.ObjCName);
							break;
						}
					}
					if (type.BaseObjCType == null)
						Console.WriteLine ("Could not resolve CLI type '{0}'", type.BaseCliType);
				}
			}
			
			if (type.BaseCliType == null && type.BaseObjCType != null) {
				if (objcTypes.TryGetValue (type.BaseObjCType, out resolved))
					type.BaseCliType = resolved.CliName;
			}
			
			foreach (var outlet in type.Outlets) {
				if (outlet.ObjCType == null) {
					if (cliTypes.TryGetValue (outlet.CliType, out resolved)) {
						outlet.ObjCType = resolved.ObjCName;
						if (resolved.IsUserType)
							type.UserTypeReferences.Add (resolved.ObjCName);
					}
				}
				if (outlet.CliType == null) {
					if (objcTypes.TryGetValue (outlet.ObjCType, out resolved))
						outlet.CliType = resolved.CliName;
				}
			}
			
			foreach (var action in type.Actions) {
				foreach (var param in action.Parameters) {
					if (param.ObjCType == null) {
						if (cliTypes.TryGetValue (param.CliType, out resolved)) {
							param.ObjCType = resolved.ObjCName;
							if (resolved.IsUserType)
								type.UserTypeReferences.Add (resolved.ObjCName);
						}
					}
					if (param.CliType == null) {
						if (objcTypes.TryGetValue (param.ObjCType, out resolved))
							param.CliType = resolved.CliName;
					}
				}
			}
		}
		
		public IEnumerable<NSObjectTypeInfo> GetUserTypes ()
		{
			return objcTypes.Values.Where (t => t.IsUserType);
		}
		
		public void GenerateObjcType (NSObjectTypeInfo type, string directory)
		{
			if (type.IsModel)
				throw new ArgumentException ("Cannot generate definition for model");
			
			string hFilePath = Path.Combine (directory, type.ObjCName + ".h");
			string mFilePath = Path.Combine (directory, type.ObjCName + ".m");
			
			using (var sw = File.CreateText (hFilePath)) {
				sw.WriteLine (modificationWarning);
				sw.WriteLine ();
				
				sw.WriteLine ("#import <UIKit/UIKit.h>");
				foreach (var reference in type.UserTypeReferences) {
					sw.WriteLine ("#import \"{0}.h\"", reference);
				}
				sw.WriteLine ();
				
				sw.WriteLine ("@interface {0} : {1} {{", type.ObjCName, type.BaseIsModel? "NSObject" : type.BaseObjCType);
				foreach (var outlet in type.Outlets) {
					sw.WriteLine ("\t{0} *_{1};", outlet.ObjCType, outlet.ObjCName);
				}
				sw.WriteLine ("}");
				sw.WriteLine ();
				
				foreach (var outlet in type.Outlets) {
					sw.WriteLine ("@property (nonatomic, retain) IBOutlet {0} *{1};", outlet.ObjCType, outlet.ObjCName);
					sw.WriteLine ();
				}
				
				foreach (var action in type.Actions) {
					if (action.Parameters.Any (p => p.ObjCType == null))
						continue;
					WriteActionSignature (action, sw);
					sw.WriteLine (";");
					sw.WriteLine ();
				}
				
				sw.WriteLine ("@end");
			}
			
			using (var sw = File.CreateText (mFilePath)) {
				sw.WriteLine (modificationWarning);
				sw.WriteLine ();
				
				sw.WriteLine ("#import \"{0}.h\"", type.ObjCName);
				sw.WriteLine ();
				
				sw.WriteLine ("@implementation {0}", type.ObjCName);
				sw.WriteLine ();
				
				bool hasOutlet = false;
				foreach (var outlet in type.Outlets) {
					sw.WriteLine ("@synthesize {0} = _{0};", outlet.ObjCName);
					hasOutlet = true;
				}
				if (hasOutlet)
					sw.WriteLine ();
				
				foreach (var action in type.Actions) {
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
		
		static string modificationWarning =
			"// WARNING\n" +
			"// This file has been generated automatically by MonoDevelop to\n" +
			"// mirror C# types. Changes in this file made by drag-connecting\n" +
			"// from the UI designer will be synchronized back to C#, but\n" +
			"// more complex manual changes may not transfer correctly.\n";
		
		void WriteActionSignature (IBAction action, TextWriter writer)
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
		
		IEnumerable<NSObjectTypeInfo> GetRegisteredObjects (ProjectDom dom)
		{
			var nso = dom.GetType (nsobjectType);
			
			if (nso == null)
				throw new Exception ("Could not get NSObject from type database");
			
			yield return new NSObjectTypeInfo ("NSObject", nsobjectType.FullName, null, null, false);
			
			foreach (var type in dom.GetSubclasses (nso, true)) {
				string objcName = null;
				bool isModel = false;
				foreach (var att in type.Attributes) {
					if (att.AttributeType.FullName == registerAttType.FullName) {
						//type registered with an explicit type name are up to the user to proide a valid name
						if (att.PositionalArguments.Count == 1)
							objcName = (string)((System.CodeDom.CodePrimitiveExpression)att.PositionalArguments[0]).Value;
						//non-nested types in the root namespace have names accessible from obj-c
						else if (string.IsNullOrEmpty (type.Namespace) && type.Name.IndexOf ('.') < 0)
							objcName = type.Name;
					}
					if (att.AttributeType.FullName == modelAttType.FullName) {
						isModel = true;
					}
				}
				if (string.IsNullOrEmpty (objcName))
					continue;
				
				var info = new NSObjectTypeInfo (objcName, type.FullName, null, type.BaseType.FullName, isModel);
				info.IsUserType = type.SourceProject != null;
				
				if (info.IsUserType)
					UpdateType (dom, info, type);
				
				yield return info;
			}
		}
		
		void UpdateType (ProjectDom dom, NSObjectTypeInfo info, IType type)
		{
			info.Actions.Clear ();
			info.Outlets.Clear ();
			
			foreach (var prop in type.Properties) {
				foreach (var att in prop.Attributes) {
					if (att.AttributeType.FullName != connectAttType.FullName)
						continue;
					string name = null;
					if (att.PositionalArguments.Count == 1)
						name = (string)((System.CodeDom.CodePrimitiveExpression)att.PositionalArguments[0]).Value;
					if (string.IsNullOrEmpty (name))
						name = prop.Name;
					info.Outlets.Add (new IBOutlet (name, prop.Name, null, prop.ReturnType.FullName));
					break;
				}
			}
			
			foreach (var meth in type.Methods) {
				foreach (var att in meth.Attributes) {
					if (att.AttributeType.FullName != exportAttType.FullName)
						continue;
					string[] name = null;
					if (att.PositionalArguments.Count == 1) {
						var n = (string)((System.CodeDom.CodePrimitiveExpression)att.PositionalArguments[0]).Value;
						if (!string.IsNullOrEmpty (n))
							name = n.Split (new [] { ':' });
					}
					var action = new IBAction (name != null? name [0] : meth.Name, meth.Name);
					info.Actions.Add (action);
					int i = 1;
					foreach (var param in meth.Parameters) {
						string label = name != null && i < name.Length? name[i] : null;
						if (label != null && label.Length == 0)
							label = null;
						action.Parameters.Add (new IBActionParameter (label, param.Name, null, param.ReturnType.FullName));
					}
					break;
				}
			}
		}
	}
}
