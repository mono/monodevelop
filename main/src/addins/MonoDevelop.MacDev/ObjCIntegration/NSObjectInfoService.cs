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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectInfoService
	{
		static readonly Regex ibRegex = new Regex ("(- \\(IBAction\\)|IBOutlet)([^;]*);", RegexOptions.Compiled);
		static readonly char[] colonChar = { ':' };
		static readonly char[] whitespaceChars = { ' ', '\t', '\n', '\r' };
		static readonly char[] splitActionParamsChars = { ' ', '\t', '\n', '\r', '*', '(', ')' };
		
		readonly IReturnType nsobjectType, registerAttType, connectAttType, exportAttType, modelAttType,
			iboutletAttType, ibactionAttType;
		
		static Dictionary<ProjectDom,NSObjectProjectInfo> infos = new Dictionary<ProjectDom, NSObjectProjectInfo> ();
		
		public NSObjectInfoService (string wrapperRoot)
		{
			this.WrapperRoot = wrapperRoot;
			string foundation = wrapperRoot + ".Foundation";
			connectAttType = new DomReturnType (foundation, "ConnectAttribute");
			exportAttType = new DomReturnType (foundation, "ExportAttribute");
			iboutletAttType = new DomReturnType (foundation, "OutletAttribute");
			ibactionAttType = new DomReturnType (foundation, "ActionAttribute");
			registerAttType = new DomReturnType (foundation, "RegisterAttribute");
			modelAttType = new DomReturnType (foundation, "ModelAttribute");
			nsobjectType = new DomReturnType (foundation, "NSObject");
		}
		
		public string WrapperRoot { get; private set; }
		
		public NSObjectProjectInfo GetProjectInfo (DotNetProject project)
		{
			var dom = ProjectDomService.GetProjectDom (project);
			if (dom == null)
				return null;
			
			dom.ForceUpdate (true);
			
			return GetProjectInfo (dom);
		}
		
		public NSObjectProjectInfo GetProjectInfo (ProjectDom dom)
		{
			NSObjectProjectInfo info;
			
			lock (infos) {
				if (infos.TryGetValue (dom, out info))
					return info;
				
				//only include DOMs that can resolve NSObject
				var nso = dom.GetType (nsobjectType);
				if (nso == null) {
					infos[dom] = null;
					return null;
				}
				
				info = new NSObjectProjectInfo (dom, this);
				infos[dom] = info;
				dom.Unloaded += HandleDomUnloaded;
				dom.ReferencesUpdated += HandleDomReferencesUpdated;
			}
			return info;
		}

		static void HandleDomReferencesUpdated (object sender, EventArgs e)
		{
			var dom = (ProjectDom)sender;
			NSObjectProjectInfo info;
			lock (infos) {
				if (!infos.TryGetValue (dom, out info))
					return;
			}
			info.SetNeedsUpdating ();
		}

		static void HandleDomUnloaded (object sender, EventArgs e)
		{
			var dom = (ProjectDom)sender;
			lock (infos) {
				dom.Unloaded -= HandleDomUnloaded;
				dom.ReferencesUpdated -= HandleDomReferencesUpdated;
				infos.Remove (dom);
			}
		}
		
		internal IEnumerable<NSObjectTypeInfo> GetRegisteredObjects (ProjectDom dom)
		{
			var nso = dom.GetType (nsobjectType);
			
			if (nso == null)
				throw new Exception ("Could not get NSObject from type database");
			
			//FIXME: only emit this for the wrapper NS
			yield return new NSObjectTypeInfo ("NSObject", nsobjectType.FullName, null, null, false);
			
			foreach (var type in dom.GetSubclasses (nso, false)) {
				var info = ConvertType (dom, type);
				if (info != null)
					yield return info;
			}
		}
		
		NSObjectTypeInfo ConvertType (ProjectDom dom, IType type)
		{
			string objcName = null;
			bool isModel = false;
			bool registeredInDesigner = true;
			foreach (var part in type.Parts) {
				foreach (var att in part.Attributes) {
					if (att.AttributeType.FullName == registerAttType.FullName) {
						if (type.SourceProject != null) {
							registeredInDesigner &=
								MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (part.CompilationUnit.FileName);
						}
						//type registered with an explicit type name are up to the user to provide a valid name
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
			}
			if (string.IsNullOrEmpty (objcName))
				return null;
			var info = new NSObjectTypeInfo (objcName, type.FullName, null, type.BaseType.FullName, isModel);
			info.IsUserType = type.SourceProject != null;
			info.IsRegisteredInDesigner = registeredInDesigner;
			
			if (info.IsUserType) {
				UpdateTypeMembers (dom, info, type);
				info.DefinedIn = type.Parts.Select (p => (string) p.CompilationUnit.FileName).ToArray ();
			}
			
			return info;
		}
		
		void UpdateTypeMembers (ProjectDom dom, NSObjectTypeInfo info, IType type)
		{
			info.Actions.Clear ();
			info.Outlets.Clear ();
			
			foreach (var prop in type.Properties) {
				foreach (var att in prop.Attributes) {
					bool isIBOutlet = att.AttributeType.FullName == iboutletAttType.FullName;
					if (!isIBOutlet) {
						if (att.AttributeType.FullName != connectAttType.FullName)
							continue;
					}
					string name = null;
					if (att.PositionalArguments.Count == 1)
						name = (string)((System.CodeDom.CodePrimitiveExpression)att.PositionalArguments[0]).Value;
					if (string.IsNullOrEmpty (name))
						name = prop.Name;
					var ol = new IBOutlet (name, prop.Name, null, prop.ReturnType.FullName);
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (prop.DeclaringType.CompilationUnit.FileName))
						ol.IsDesigner = true;
					info.Outlets.Add (ol);
					break;
				}
			}
			
			foreach (var meth in type.Methods) {
				foreach (var att in meth.Attributes) {
					bool isIBAction = att.AttributeType.FullName == ibactionAttType.FullName;
					if (!isIBAction) {
						if (att.AttributeType.FullName != exportAttType.FullName)
							continue;
					}
					bool isDesigner =  MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (
						meth.DeclaringType.CompilationUnit.FileName);
					//only support Export from old designer files, user code must be IBAction
					if (!isDesigner && !isIBAction)
						continue;
					
					string[] name = null;
					if (att.PositionalArguments.Count == 1) {
						var n = (string)((System.CodeDom.CodePrimitiveExpression)att.PositionalArguments[0]).Value;
						if (!string.IsNullOrEmpty (n))
							name = n.Split (colonChar);
					}
					var action = new IBAction (name != null? name [0] : meth.Name, meth.Name);
					int i = 1;
					foreach (var param in meth.Parameters) {
						string label = name != null && i < name.Length? name[i] : null;
						if (label != null && label.Length == 0)
							label = null;
						action.Parameters.Add (new IBActionParameter (label, param.Name, null, param.ReturnType.FullName));
					}
					if (MonoDevelop.DesignerSupport.CodeBehind.IsDesignerFile (meth.DeclaringType.CompilationUnit.FileName))
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