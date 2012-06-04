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
using MonoDevelop.Ide.TypeSystem;

using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectInfoService
	{
		//static readonly Regex frameworkRegex = new Regex ("#import\\s+<([A-Z][A-Za-z]*)/([A-Z][A-Za-z]*)\\.h>", RegexOptions.Compiled);
		static readonly Regex typeInfoRegex = new Regex ("@(interface|protocol)\\s+(\\w*)\\s*:\\s*(\\w*)", RegexOptions.Compiled);
		static readonly Regex ibRegex = new Regex ("(-\\s*\\(IBAction\\)|IBOutlet)\\s*([^;]*);", RegexOptions.Compiled);
		static readonly char[] colonChar = { ':' };
		static readonly char[] whitespaceChars = { ' ', '\t', '\n', '\r' };
		static readonly char[] splitActionParamsChars = { ' ', '\t', '\n', '\r', '*', '(', ')' };
		
		readonly ITypeReference nsobjectType, registerAttType, connectAttType, exportAttType, modelAttType,
			iboutletAttType, ibactionAttType;
		
		static Dictionary<TypeSystemService.ProjectContentWrapper,NSObjectProjectInfo> infos = new Dictionary<TypeSystemService.ProjectContentWrapper, NSObjectProjectInfo> ();

		ITypeDefinition Resolve (TypeSystemService.ProjectContentWrapper dom, ITypeReference reference)
		{
			return reference.Resolve (dom.Compilation).GetDefinition ();
		}
		

		static NSObjectInfoService ()
		{
			TypeSystemService.ProjectUnloaded += HandleDomUnloaded;
		}
		
		public NSObjectInfoService (string wrapperRoot)
		{
			this.WrapperRoot = wrapperRoot;
			var typeNamespace = wrapperRoot + ".Foundation";
			connectAttType = new GetClassTypeReference (typeNamespace, "ConnectAttribute");
			exportAttType = new GetClassTypeReference (typeNamespace, "ExportAttribute");
			iboutletAttType = new GetClassTypeReference (typeNamespace, "OutletAttribute");
			ibactionAttType = new GetClassTypeReference (typeNamespace, "ActionAttribute");
			registerAttType = new GetClassTypeReference (typeNamespace, "RegisterAttribute");
			modelAttType = new GetClassTypeReference (typeNamespace, "ModelAttribute");
			nsobjectType = new GetClassTypeReference (typeNamespace, "NSObject");
		}
		
		public string WrapperRoot { get; private set; }
		
		public NSObjectProjectInfo GetProjectInfo (DotNetProject project, IAssembly lookinAssembly = null)
		{
			var dom = TypeSystemService.GetProjectContentWrapper (project);
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
				
				var nso = Resolve (dom, nsobjectType);
				//only include DOMs that can resolve NSObject
				if (nso == null || nso.Kind == TypeKind.Unknown)
					return null;
				
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
			var project = e.Project as DotNetProject;
			if (project == null)
				return;
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
			var nso = Resolve (dom, nsobjectType);
			if (nso == null || nso.Kind == TypeKind.Unknown)
				throw new Exception ("Could not get NSObject from type database");
			
			//FIXME: only emit this for the wrapper NS
//			yield return new NSObjectTypeInfo ("NSObject", nso.GetDefinition ().FullName, null, null, false, false, false);
			int cnt = 0, infcnt=0, models=0;
			nso = assembly.Compilation.Import (nso);
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
				if (attType.Equals (Resolve (dom, registerAttType))) {
					if (type.GetProjectContent () != null) {
						registeredInDesigner &=
							MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (att.Region.FileName);
					}
					
					//type registered with an explicit type name are up to the user to provide a valid name
					var posArgs = att.PositionalArguments;
					if (posArgs.Count == 1 || posArgs.Count == 2)
						objcName = posArgs [0].ConstantValue as string;
					//non-nested types in the root namespace have names accessible from obj-c
					else if (string.IsNullOrEmpty (type.Namespace) && type.Name.IndexOf ('.') < 0)
						objcName = type.Name;
				}
				
				if (attType.Equals (Resolve (dom, modelAttType)))
					isModel = true;
			}
			
			if (string.IsNullOrEmpty (objcName))
				return null;
			
			string baseType = type.DirectBaseTypes.First ().ReflectionName;
			if (baseType == "System.Object")
				baseType = null;
			
			bool isUserType = !type.ParentAssembly.Equals (Resolve (dom, nsobjectType).ParentAssembly);
			
			var info = new NSObjectTypeInfo (objcName, type.ReflectionName, null, baseType, isModel, isUserType, registeredInDesigner);

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
					bool isIBOutlet = attType.Equals (Resolve (dom, iboutletAttType));
					if (!isIBOutlet) {
						if (!attType.Equals (Resolve (dom, connectAttType)))
							continue;
					}
					string name = null;
					var posArgs = att.PositionalArguments;
					if (posArgs.Count == 1)
						name = posArgs [0].ConstantValue as string;
					if (string.IsNullOrEmpty (name))
						name = prop.Name;
					
					// HACK: Work around bug #1586 in the least obtrusive way possible. Strip out any outlet
					// with the name 'view' on subclasses of MonoTouch.UIKit.UIViewController to avoid 
					// conflicts with the view property mapped there
					if (name == "view") {
						if (type.GetAllBaseTypeDefinitions ().Any (p => p.ReflectionName == "MonoTouch.UIKit.UIViewController"))
							continue;
					}
					
					var ol = new IBOutlet (name, prop.Name, null, prop.ReturnType.ReflectionName);
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (prop.Region.FileName))
						ol.IsDesigner = true;
					info.Outlets.Add (ol);
					break;
				}
			}
			foreach (var meth in type.Methods) {
				foreach (var att in meth.Attributes) {
					var attType = att.AttributeType;
					bool isIBAction = attType.Equals (Resolve (dom, ibactionAttType));
					if (!isIBAction) {
						if (!attType.Equals (Resolve (dom, exportAttType)))
							continue;
					}

					bool isDesigner = meth.Parts.Any (part => MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (part.Region.FileName));
					//only support Export from old designer files, user code must be IBAction
					if (!isDesigner && !isIBAction)
						continue;
					
					string[] name = null;
					var posArgs = att.PositionalArguments;
					if (posArgs.Count == 1 || posArgs.Count == 2) {
						var n = posArgs [0].ConstantValue as string;
						if (!string.IsNullOrEmpty (n))
							name = n.Split (colonChar);
					}
					var action = new IBAction (name != null ? name [0] : meth.Name, meth.Name);
					int i = 1;
					foreach (var param in meth.Parameters) {
						string label = name != null && i < name.Length ? name [i] : null;
						if (label != null && label.Length == 0)
							label = null;
						action.Parameters.Add (new IBActionParameter (label, param.Name, null, param.Type.ReflectionName));
					}
					if (meth.Parts.Any (part => MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (part.Region.FileName)))
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
					string objcType = split[0].TrimEnd ('*');
					string objcName = null;

					for (int i = 1; i < split.Length; i++) {
						objcName = split[i].TrimStart ('*');
						if (string.IsNullOrEmpty (objcName))
							continue;

						if (i + 1 < split.Length) {
							// This is a bad sign... what tokens are after the name??
							objcName = null;
							break;
						}
					}

					if (string.IsNullOrEmpty (objcType) || string.IsNullOrEmpty (objcName)) {
						MessageService.ShowError (GettextCatalog.GetString ("Error while parsing header file."),
							string.Format (GettextCatalog.GetString ("The definition '{0}' can't be parsed."), def));

						// We can't recover if objcName is empty...
						if (string.IsNullOrEmpty (objcName))
							continue;

						// We can try using NSObject...
						objcType = "NSObject";
					}

					if (objcType == "id")
						objcType = "NSObject";

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
