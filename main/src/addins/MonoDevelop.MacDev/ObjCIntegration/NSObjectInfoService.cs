// 
// NSObjectInfoService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
// Copyright (c) 2011 Xamarin Inc.
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
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.TypeSystem;

using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectInfoService
	{
		//static readonly Regex frameworkRegex = new Regex ("#import\\s+<([A-Z][A-Za-z]*)/([A-Z][A-Za-z]*)\\.h>", RegexOptions.Compiled);
		static readonly Regex typeInfoRegex = new Regex ("@(interface|protocol)\\s+(\\w*)\\s*:\\s*(\\w*)", RegexOptions.Compiled);
		static readonly Regex ibRegex = new Regex ("(- \\(IBAction\\)|IBOutlet)([^;]*);", RegexOptions.Compiled);
		static readonly char[] colonChar = { ':' };
		static readonly char[] whitespaceChars = { ' ', '\t', '\n', '\r' };
		static readonly char[] splitActionParamsChars = { ' ', '\t', '\n', '\r', '*', '(', ')' };
		
		readonly string typeNamespace;
		readonly string nsobjectType, registerAttType, connectAttType, exportAttType, modelAttType,
			iboutletAttType, ibactionAttType;
		
		static Dictionary<TypeSystemService.ProjectContentWrapper,NSObjectProjectInfo> infos = new Dictionary<TypeSystemService.ProjectContentWrapper, NSObjectProjectInfo> ();
		
		static NSObjectInfoService ()
		{
			TypeSystemService.ProjectUnloaded += HandleDomUnloaded;
		}
		
		public NSObjectInfoService (string wrapperRoot)
		{
			this.WrapperRoot = wrapperRoot;
			typeNamespace = wrapperRoot + ".Foundation";
			connectAttType = "ConnectAttribute";
			exportAttType = "ExportAttribute";
			iboutletAttType = "OutletAttribute";
			ibactionAttType = "ActionAttribute";
			registerAttType = "RegisterAttribute";
			modelAttType = "ModelAttribute";
			nsobjectType = "NSObject";
		}
		
		public string WrapperRoot { get; private set; }
		
		public NSObjectProjectInfo GetProjectInfo (DotNetProject project, IAssembly lookinAssembly = null)
		{
			var dom = TypeSystemService.GetProjectContentWrapper (project);
			if (dom == null)
				return null;
			project.ReferenceAddedToProject += HandleDomReferencesUpdated;
			project.ReferenceRemovedFromProject += HandleDomReferencesUpdated;
			return GetProjectInfo (dom, lookinAssembly);
		}
		
		public NSObjectProjectInfo GetProjectInfo (TypeSystemService.ProjectContentWrapper dom, IAssembly lookinAssembly = null)
		{
			NSObjectProjectInfo info;
			
			lock (infos) {
				if (infos.TryGetValue (dom, out info))
					return info;
				
				var nso = dom.Compilation.LookupType (typeNamespace, nsobjectType);
				//only include DOMs that can resolve NSObject
				if (nso == null || nso.Kind == TypeKind.Unknown) {
					infos[dom] = null;
					return null;
				}
				
				info = new NSObjectProjectInfo (dom, this, lookinAssembly);
				infos[dom] = info;
			}
			return info;
		}

		static void HandleDomReferencesUpdated (object sender, ProjectReferenceEventArgs e)
		{
			var project = (DotNetProject)sender;
			var dom = TypeSystemService.GetProjectContentWrapper (project);
			if (dom == null)
				return;
			NSObjectProjectInfo info;
			lock (infos) {
				if (!infos.TryGetValue (dom, out info))
					return;
			}
			info.SetNeedsUpdating ();
		}

		static void HandleDomUnloaded (object sender, ProjectUnloadEventArgs e)
		{
			var project = (DotNetProject)e.Project;
			var dom = e.Wrapper;
			if (dom == null)
				return;
			lock (infos) {
				project.ReferenceAddedToProject -= HandleDomReferencesUpdated;
				project.ReferenceRemovedFromProject -= HandleDomReferencesUpdated;
				infos.Remove (dom);
			}
		}
		
		internal IEnumerable<NSObjectTypeInfo> GetRegisteredObjects (TypeSystemService.ProjectContentWrapper dom, IAssembly assembly)
		{
			var nso = dom.Compilation.LookupType (typeNamespace, nsobjectType);
			if (nso == null || nso.Kind == TypeKind.Unknown)
				throw new Exception ("Could not get NSObject from type database");
			
			//FIXME: only emit this for the wrapper NS
//			yield return new NSObjectTypeInfo ("NSObject", nso.GetDefinition ().FullName, null, null, false, false, false);
			int cnt = 0, infcnt=0, models=0;
			
			foreach (var contextType in assembly.GetAllTypeDefinitions ()) {
				if (contextType.IsDerivedFrom (nso)) {
					var info = ConvertType (dom, contextType);
					if (info != null)
						yield return info;
				}
			}
		}
		
		NSObjectTypeInfo ConvertType (TypeSystemService.ProjectContentWrapper dom, ITypeDefinition type)
		{
			string objcName = null;
			bool isModel = false;
			bool registeredInDesigner = true;
			foreach (var att in type.Attributes) {
				var attType = att.AttributeType;
				if (attType.Equals (dom.Compilation.LookupType (typeNamespace, registerAttType)))  {
					if (type.GetProjectContent () != null) {
						registeredInDesigner &=
							MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (att.Region.FileName);
					}
					
					//type registered with an explicit type name are up to the user to provide a valid name
					var posArgs = att.PositionalArguments;
					if (posArgs.Count == 1 || posArgs.Count == 2)
						objcName = posArgs[0].ConstantValue as string;
					//non-nested types in the root namespace have names accessible from obj-c
					else if (string.IsNullOrEmpty (type.Namespace) && type.Name.IndexOf ('.') < 0)
						objcName = type.Name;
				}
				if (attType.Equals (dom.Compilation.LookupType (typeNamespace, modelAttType))) {
					isModel = true;
				}
			}
			if (string.IsNullOrEmpty (objcName))
				return null;
			string baseType = type.DirectBaseTypes.First ().FullName;
			if (baseType == "System.Object")
				baseType = null;
			var info = new NSObjectTypeInfo (objcName, type.FullName, null, baseType, isModel,
					type.GetSourceProject () != null, registeredInDesigner);
			
			if (info.IsUserType) {
				UpdateTypeMembers (dom, info, type);
				info.DefinedIn = type.Parts.Select (p => (string) p.Region.FileName).ToArray ();
			}
			
			return info;
		}
		
		void UpdateTypeMembers (TypeSystemService.ProjectContentWrapper dom, NSObjectTypeInfo info, ITypeDefinition type)
		{
			info.Actions.Clear ();
			info.Outlets.Clear ();
			foreach (var prop in type.Properties) {
				foreach (var att in prop.Attributes) {
					var attType = att.AttributeType;
					bool isIBOutlet = attType.Equals (dom.Compilation.LookupType (typeNamespace, iboutletAttType));
					if (!isIBOutlet) {
						if (!attType.Equals (dom.Compilation.LookupType (typeNamespace, connectAttType)))
							continue;
					}
					string name = null;
					var posArgs = att.PositionalArguments;
					if (posArgs.Count == 1)
						name = posArgs[0].ConstantValue as string;
					if (string.IsNullOrEmpty (name))
						name = prop.Name;
					
					// HACK: Work around bug #1586 in the least obtrusive way possible. Strip out any outlet
					// with the name 'view' on subclasses of MonoTouch.UIKit.UIViewController to avoid 
					// conflicts with the view property mapped there
					if (name == "view")
						if (type.GetAllBaseTypeDefinitions ().Any (p => p.FullName == "MonoTouch.UIKit.UIViewController"))
							continue;
					
					var ol = new IBOutlet (name, prop.Name, null, prop.ReturnType.FullName);
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (prop.Region.FileName))
						ol.IsDesigner = true;
					info.Outlets.Add (ol);
					break;
				}
			}
			
			foreach (var meth in type.Methods) {
				foreach (var att in meth.Attributes) {
					var attType = att.AttributeType;
					bool isIBAction = attType.Equals (dom.Compilation.LookupType (typeNamespace, ibactionAttType));
					if (!isIBAction) {
						if (!attType.Equals (dom.Compilation.LookupType (typeNamespace, exportAttType)))
							continue;
					}
					bool isDesigner =  MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (
						meth.DeclaringTypeDefinition.Region.FileName);
					//only support Export from old designer files, user code must be IBAction
					if (!isDesigner && !isIBAction)
						continue;
					
					string[] name = null;
					var posArgs = att.PositionalArguments;
					if (posArgs.Count == 1 || posArgs.Count == 2) {
						var n = posArgs[0].ConstantValue as string;
						if (!string.IsNullOrEmpty (n))
							name = n.Split (colonChar);
					}
					var action = new IBAction (name != null? name [0] : meth.Name, meth.Name);
					int i = 1;
					foreach (var param in meth.Parameters) {
						string label = name != null && i < name.Length? name[i] : null;
						if (label != null && label.Length == 0)
							label = null;
						action.Parameters.Add (new IBActionParameter (label, param.Name, null, param.Type.FullName));
					}
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (meth.Region.FileName))
						action.IsDesigner = true;
					info.Actions.Add (action);
					break;
				}
			}
		}
		
		public static NSObjectTypeInfo ParseHeader (string headerFile)
		{
			string text = File.ReadAllText (headerFile);
			string userType = null, userBaseType = null;
			MatchCollection matches;
			NSObjectTypeInfo type;
			
			// First, grep for classes
			matches = typeInfoRegex.Matches (text);
			foreach (Match match in matches) {
				if (match.Groups[1].Value != "interface")
					continue;
				
				if (userType != null) {
					// UNSUPPORTED: more than 1 user-type defined in this header
					return null;
				}
				
				userType = match.Groups[2].Value;
				userBaseType = match.Groups[3].Value;
			}
			
			if (userType == null)
				return null;
			
			type = new NSObjectTypeInfo (userType, null, userBaseType, null, false, true, true);
			
			// Now grep for IBActions and IBOutlets
			matches = ibRegex.Matches (text);
			foreach (Match match in matches) {
				var kind = match.Groups[1].Value;
				var def = match.Groups[2].Value;
				if (kind == "IBOutlet") {
					var split = def.Split (whitespaceChars, StringSplitOptions.RemoveEmptyEntries);
					if (split.Length != 2)
						continue;
					string objcName = split[1].TrimStart ('*');
					string objcType = split[0].TrimEnd ('*');
					if (objcType == "id")
						objcType = "NSObject";
					if (string.IsNullOrEmpty (objcType)) {
						MessageService.ShowError (GettextCatalog.GetString ("Error while parsing header file."),
							string.Format (GettextCatalog.GetString ("The definition '{0}' can't be parsed."), def));
						objcType = "NSObject";
					}
					
					IBOutlet outlet = new IBOutlet (objcName, objcName, objcType, null);
					outlet.IsDesigner = true;
					
					type.Outlets.Add (outlet);
				} else {
					string[] split = def.Split (colonChar);
					string name = split[0].Trim ();
					var action = new IBAction (name, name);
					action.IsDesigner = true;
					
					string label = null;
					for (int i = 1; i < split.Length; i++) {
						var s = split[i].Split (splitActionParamsChars, StringSplitOptions.RemoveEmptyEntries);
						string objcType = s[0];
						if (objcType == "id")
							objcType = "NSObject";
						var par = new IBActionParameter (label, s[1], objcType, null);
						label = s.Length == 3? s[2] : null;
						action.Parameters.Add (par);
					}
					
					type.Actions.Add (action);
				}
			}
			
			return type;
		}
	}
	
	public class UserTypeChangeEventArgs : EventArgs
	{
		public UserTypeChangeEventArgs (IList<UserTypeChange> changes)
		{
			this.Changes = changes;
		}
		
		public IList<UserTypeChange> Changes { get; private set; }
	}
	
	public class UserTypeChange
	{
		public UserTypeChange (NSObjectTypeInfo type, UserTypeChangeKind kind)
		{
			this.Type = type;
			this.Kind = kind;
		}
		
		public NSObjectTypeInfo Type { get; private set; }
		public UserTypeChangeKind Kind { get; private set; }
	}
	
	public enum UserTypeChangeKind
	{
		Added,
		Removed,
		Modified
	}
	
}
