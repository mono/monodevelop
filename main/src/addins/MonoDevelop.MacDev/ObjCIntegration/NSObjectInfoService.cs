// 
// NSObjectInfoService.cs
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
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.TypeSystem;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectInfoService
	{
		static readonly Regex ibRegex = new Regex ("(- \\(IBAction\\)|IBOutlet)([^;]*);", RegexOptions.Compiled);
		static readonly char[] colonChar = { ':' };
		static readonly char[] whitespaceChars = { ' ', '\t', '\n', '\r' };
		static readonly char[] splitActionParamsChars = { ' ', '\t', '\n', '\r', '*', '(', ')' };
		
		readonly ITypeReference nsobjectType, registerAttType, connectAttType, exportAttType, modelAttType,
			iboutletAttType, ibactionAttType;
		
		static Dictionary<DotNetProject,NSObjectProjectInfo> infos = new Dictionary<DotNetProject, NSObjectProjectInfo> ();
		
		static NSObjectInfoService ()
		{
			TypeSystemService.ProjectUnloaded += HandleDomUnloaded;
		}
		
		public NSObjectInfoService (string wrapperRoot)
		{
			this.WrapperRoot = wrapperRoot;
			string foundation = wrapperRoot + ".Foundation";
			connectAttType = new GetClassTypeReference (foundation, "ConnectAttribute");
			exportAttType = new GetClassTypeReference (foundation, "ExportAttribute");
			iboutletAttType = new GetClassTypeReference (foundation, "OutletAttribute");
			ibactionAttType = new GetClassTypeReference (foundation, "ActionAttribute");
			registerAttType = new GetClassTypeReference (foundation, "RegisterAttribute");
			modelAttType = new GetClassTypeReference (foundation, "ModelAttribute");
			nsobjectType = new GetClassTypeReference (foundation, "NSObject");
		}
		
		public string WrapperRoot { get; private set; }
		
		public NSObjectProjectInfo GetProjectInfo (DotNetProject project)
		{
			var dom = TypeSystemService.GetContext (project);
			if (dom == null)
				return null;
			
			TypeSystemService.ForceUpdate (dom);
			project.ReferenceAddedToProject += HandleDomReferencesUpdated;
			project.ReferenceRemovedFromProject += HandleDomReferencesUpdated;
			return GetProjectInfo (project, dom);
		}
		
		NSObjectProjectInfo GetProjectInfo (DotNetProject project, ITypeResolveContext dom)
		{
			NSObjectProjectInfo info;
			
			lock (infos) {
				if (infos.TryGetValue (project, out info))
					return info;
				
				//only include DOMs that can resolve NSObject
				var nso = nsobjectType.Resolve (dom);
				if (nso == null) {
					infos[project] = null;
					return null;
				}
				
				info = new NSObjectProjectInfo (project, dom, this);
				infos[project] = info;
			}
			return info;
		}

		static void HandleDomReferencesUpdated (object sender, ProjectReferenceEventArgs e)
		{
			var project = (DotNetProject)sender;
			NSObjectProjectInfo info;
			lock (infos) {
				if (!infos.TryGetValue (project, out info))
					return;
			}
			info.SetNeedsUpdating ();
		}

		static void HandleDomUnloaded (object sender, ProjectEventArgs e)
		{
			var project = (DotNetProject)e.Project;
			lock (infos) {
				project.ReferenceAddedToProject -= HandleDomReferencesUpdated;
				project.ReferenceRemovedFromProject -= HandleDomReferencesUpdated;
				infos.Remove (project);
			}
		}
		
		internal IEnumerable<NSObjectTypeInfo> GetRegisteredObjects (ITypeResolveContext dom)
		{
			var nso = nsobjectType.Resolve (dom);
			
			if (nso == null)
				throw new Exception ("Could not get NSObject from type database");
			
			//FIXME: only emit this for the wrapper NS
			yield return new NSObjectTypeInfo ("NSObject", nso.GetDefinition (), null, null, false);
			
			foreach (var type in nso.GetAllBaseTypeDefinitions (dom)) {
				var info = ConvertType (dom, type);
				if (info != null)
					yield return info;
			}
		}
		
		NSObjectTypeInfo ConvertType (ITypeResolveContext dom, ITypeDefinition type)
		{
			string objcName = null;
			bool isModel = false;
			bool registeredInDesigner = true;
			foreach (var part in type.GetParts ()) {
				foreach (var att in part.Attributes) {
					var attType = att.AttributeType.Resolve (dom);
					if (attType.Equals (registerAttType.Resolve (dom)))  {
						if (type.GetProjectContent () != null) {
							registeredInDesigner &=
								MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (part.Region.FileName);
						}
						//type registered with an explicit type name are up to the user to provide a valid name
						var posArgs = att.GetPositionalArguments (dom);
						if (posArgs.Count == 1)
							objcName = (string)posArgs[0].GetValue (dom);
						//non-nested types in the root namespace have names accessible from obj-c
						else if (string.IsNullOrEmpty (type.Namespace) && type.Name.IndexOf ('.') < 0)
							objcName = type.Name;
					}
					if (attType.Equals (modelAttType.Resolve (dom))) {
						isModel = true;
					}
				}
			}
			if (string.IsNullOrEmpty (objcName))
				return null;
			var info = new NSObjectTypeInfo (objcName, type, null, type.BaseTypes.First (), isModel);
			info.IsUserType = type.GetProjectContent () != null;
			info.IsRegisteredInDesigner = registeredInDesigner;
			
			if (info.IsUserType) {
				UpdateTypeMembers (dom, info, type);
				info.DefinedIn = type.GetParts ().Select (p => (string) p.Region.FileName).ToArray ();
			}
			
			return info;
		}
		
		void UpdateTypeMembers (ITypeResolveContext dom, NSObjectTypeInfo info, ITypeDefinition type)
		{
			info.Actions.Clear ();
			info.Outlets.Clear ();
			
			foreach (var prop in type.Properties) {
				foreach (var att in prop.Attributes) {
					var attType = att.AttributeType.Resolve (dom);
					bool isIBOutlet = iboutletAttType.Resolve (dom).Equals (attType);
					if (!isIBOutlet) {
						if (connectAttType.Resolve (dom).Equals (attType))
							continue;
					}
					string name = null;
					var posArgs = att.GetPositionalArguments (dom);
					if (posArgs.Count == 1)
						name = (string)posArgs[0].GetValue (dom);
					if (string.IsNullOrEmpty (name))
						name = prop.Name;
					var ol = new IBOutlet (name, prop.Name, null, prop.ReturnType.Resolve (dom).FullName);
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (prop.DeclaringTypeDefinition.Region.FileName))
						ol.IsDesigner = true;
					info.Outlets.Add (ol);
					break;
				}
			}
			
			foreach (var meth in type.Methods) {
				foreach (var att in meth.Attributes) {
					var attType = att.AttributeType.Resolve (dom);
					bool isIBAction = ibactionAttType.Resolve (dom).Equals (attType);
					if (!isIBAction) {
						if (exportAttType.Resolve (dom).Equals (attType))
							continue;
					}
					bool isDesigner =  MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (
						meth.DeclaringTypeDefinition.Region.FileName);
					//only support Export from old designer files, user code must be IBAction
					if (!isDesigner && !isIBAction)
						continue;
					
					string[] name = null;
					var posArgs = att.GetPositionalArguments (dom);
					if (posArgs.Count == 1) {
						var n = (string)posArgs[0].GetValue (dom);
						if (!string.IsNullOrEmpty (n))
							name = n.Split (colonChar);
					}
					var action = new IBAction (name != null? name [0] : meth.Name, meth.Name);
					int i = 1;
					foreach (var param in meth.Parameters) {
						string label = name != null && i < name.Length? name[i] : null;
						if (label != null && label.Length == 0)
							label = null;
						action.Parameters.Add (new IBActionParameter (label, param.Name, null, param.Type.Resolve (dom).FullName));
					}
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (meth.DeclaringTypeDefinition.Region.FileName))
						action.IsDesigner = true;
					info.Actions.Add (action);
					break;
				}
			}
		}
		
		public static NSObjectTypeInfo ParseHeader (string headerFile)
		{
			var text = System.IO.File.ReadAllText (headerFile);
			var matches = ibRegex.Matches (text);
			var type = new NSObjectTypeInfo (System.IO.Path.GetFileNameWithoutExtension (headerFile), null, null, null, false);
			foreach (Match match in matches) {
				var kind = match.Groups[1].Value;
				var def = match.Groups[2].Value;
				if (kind == "IBOutlet") {
					var split = def.Split (whitespaceChars, StringSplitOptions.RemoveEmptyEntries);
					if (split.Length != 2)
						continue;
					string objcType = split[1].TrimStart ('*');
					if (objcType == "id")
						objcType = "NSObject";
					type.Outlets.Add (new IBOutlet ((objcType), null, split[0].TrimEnd ('*'), null));
				} else {
					string[] split = def.Split (colonChar);
					var action = new IBAction (split[0].Trim (), null);
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