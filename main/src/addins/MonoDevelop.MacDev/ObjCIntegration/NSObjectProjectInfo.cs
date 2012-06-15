// 
// NSObjectProjectInfo.cs
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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using MonoDevelop.Projects;

using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public class NSObjectProjectInfo
	{
		Dictionary<string,NSObjectTypeInfo> objcTypes = new Dictionary<string,NSObjectTypeInfo> ();
		Dictionary<string,NSObjectTypeInfo> cliTypes = new Dictionary<string,NSObjectTypeInfo> ();
		Dictionary<string,NSObjectTypeInfo> refCliTypes = new Dictionary<string,NSObjectTypeInfo> ();
		Dictionary<string,NSObjectTypeInfo> refObjcTypes = new Dictionary<string,NSObjectTypeInfo> ();
		
		NSObjectInfoService infoService;
		TypeSystemService.ProjectContentWrapper dom;
		IAssembly lookinAssembly;
		bool needsUpdating;
		
		public NSObjectProjectInfo (TypeSystemService.ProjectContentWrapper dom, NSObjectInfoService infoService, IAssembly lookinAssembly)
		{
			this.infoService = infoService;
			this.dom = dom;
			this.lookinAssembly = lookinAssembly;
			needsUpdating = true;
		}
		
		internal void SetNeedsUpdating ()
		{
			needsUpdating = true;
		}
		
		internal void Update (bool force)
		{
			if (force)
				SetNeedsUpdating ();
			Update ();
		}
		
		internal void Update ()
		{
			if (!needsUpdating)
				return;

			foreach (DotNetProject r in dom.ReferencedProjects) {
				var info = infoService.GetProjectInfo (r);
				if (info != null)
					info.Update ();
			}

			TypeSystemService.ForceUpdate (dom);
			objcTypes.Clear ();
			cliTypes.Clear ();
			refObjcTypes.Clear ();
			refCliTypes.Clear ();
			
			foreach (var type in infoService.GetRegisteredObjects (dom, dom.Compilation.MainAssembly)) {
				if (objcTypes.ContainsKey (type.ObjCName)) {
					var other = objcTypes[type.ObjCName];
					if (other.CliName == type.CliName)
						continue;

					throw new ArgumentException (string.Format ("Multiple types ({0} and {1}) registered with the same Objective-C name: {2}", type.CliName, other.CliName, type.ObjCName));
				}
				
				objcTypes.Add (type.ObjCName, type);
				cliTypes.Add (type.CliName, type);
			}
			
			foreach (var refAssembly in dom.Compilation.ReferencedAssemblies) {
				foreach (var type in infoService.GetRegisteredObjects (dom, refAssembly)) {
					if (refObjcTypes.ContainsKey (type.ObjCName)) {
						var other = refObjcTypes[type.ObjCName];
						if (other.CliName == type.CliName)
							continue;

						throw new ArgumentException (string.Format ("Multiple types ({0} and {1}) registered with the same Objective-C name: {2}", type.CliName, other.CliName, type.ObjCName));
					}
					
					refObjcTypes.Add (type.ObjCName, type);
					refCliTypes.Add (type.CliName, type);
				}
			}
			
			foreach (var type in cliTypes.Values)
				ResolveCliToObjc (type);
			
			needsUpdating = false;
		}
		
		public IEnumerable<NSObjectTypeInfo> GetTypes ()
		{
			return objcTypes.Values;
		}
		
		public NSObjectTypeInfo GetType (string objcName)
		{
			NSObjectTypeInfo ret;
			if (objcTypes.TryGetValue (objcName, out ret))
				return ret;
			return null;
		}
		
		public bool ContainsType (string objcName)
		{
			return objcTypes.ContainsKey (objcName);
		}
		
		internal void InsertUpdatedType (NSObjectTypeInfo type)
		{
			objcTypes[type.ObjCName] = type;
			cliTypes[type.CliName] = type;
		}
		
		bool TryResolveCliToObjc (string cliType, out NSObjectTypeInfo resolved)
		{
			if (cliTypes.TryGetValue (cliType, out resolved))
				return true;
			if (refCliTypes.TryGetValue (cliType, out resolved))
				return true;
			resolved = null;
			return false;
		}
		
		bool TryResolveObjcToCli (string objcType, out NSObjectTypeInfo resolved)
		{
			if (objcTypes.TryGetValue (objcType, out resolved))
				return true;
			if (refObjcTypes.TryGetValue (objcType, out resolved))
				return true;
#if false
			var msg = new StringBuilder ("Can't resolve "+ objcType + Environment.NewLine);
			foreach (var r in dom.References) {
				msg.Append ("Referenced dom:");
				msg.Append (r);
				var rDom = infoService.GetProjectInfo (r);
				if (rDom == null) {
					msg.AppendLine ("projectinfo == null");
					continue;
				}
				msg.Append ("known types:");
				msg.AppendLine (string.Join (",", rDom.objcTypes.Keys.ToArray()));
			}
			LoggingService.LogWarning (msg.ToString ());
#endif
			resolved = null;
			return false;
		}

		static IEnumerable<ITypeDefinition> GetAllBaseClassDefinitions (IType type)
		{
			while (type != null) {
				IType baseType = null;

				foreach (var t in type.DirectBaseTypes) {
					if (t.Kind != TypeKind.Class)
						continue;
					var def = t.GetDefinition ();
					if (def != null)
						yield return def;
					baseType = t;
					break;
				}

				type = baseType;
			}
		}
		
		/// <summary>
		/// Resolves the Objective-C types by mapping the known .NET type information.
		/// </summary>
		/// <param name='type'>
		/// An NSObjectTypeInfo with the .NET type information filled in.
		/// </param>
		public void ResolveCliToObjc (NSObjectTypeInfo type)
		{
			NSObjectTypeInfo resolved;
			
			if (type.BaseObjCType == null && type.BaseCliType != null) {
				if (TryResolveCliToObjc (type.BaseCliType, out resolved)) {
					if (resolved.IsModel)
						type.BaseIsModel = true;
					type.BaseObjCType = resolved.ObjCName;
					//FIXME: handle type references better
					if (resolved.IsUserType)
						type.UserTypeReferences.Add (resolved.ObjCName);
				} else {
					//managed classes many have implicitly registered base classes with a name not
					//expressible in obj-c. In this case, the best we can do is walk down the 
					//hierarchy until we find a valid base class
					var reference = ReflectionHelper.ParseReflectionName (type.BaseCliType);
					var baseCliType = reference.Resolve (dom.Compilation);
					foreach (var bt in GetAllBaseClassDefinitions (baseCliType)) {
						if (TryResolveCliToObjc (bt.ReflectionName, out resolved)) {
							if (resolved.IsModel)
								type.BaseIsModel = true;
							type.BaseObjCType = resolved.ObjCName;
							if (resolved.IsUserType)
								type.UserTypeReferences.Add (resolved.ObjCName);
							break;
						}
					}
				}
			}
			
			foreach (var outlet in type.Outlets) {
				if (outlet.ObjCType != null)
					continue;
				
				if (TryResolveCliToObjc (outlet.CliType, out resolved)) {
					outlet.ObjCType = resolved.ObjCName;
					if (resolved.IsUserType)
						type.UserTypeReferences.Add (resolved.ObjCName);
				}
			}
			
			foreach (var action in type.Actions) {
				foreach (var param in action.Parameters) {
					if (param.ObjCType != null)
						continue;
					
					if (TryResolveCliToObjc (param.CliType, out resolved)) {
						param.ObjCType = resolved.ObjCName;
						if (resolved.IsUserType)
							type.UserTypeReferences.Add (resolved.ObjCName);
					}
				}
			}
		}
		
		/// <summary>
		/// Resolves the type by mapping the known Objective-C type information to .NET types.
		/// </summary>
		/// <returns>
		/// The number of unresolved types still remaining.
		/// </returns>
		/// <param name='type'>
		/// The NSObjectTypeInfo that contains the known Objective-C type information.
		/// Typically this will be the result of NSObjectInfoService.ParseHeader().
		/// </param>
		/// <param name='provider'>
		/// A CodeDom provider which is used to make sure type names don't conflict with language keywords.
		/// </param>
		/// <param name='defaultNamespace'>
		/// The default namespace used when forcing type resolution.
		/// </param>
		public void ResolveObjcToCli (IProgressMonitor monitor, NSObjectTypeInfo type, CodeDomProvider provider, string defaultNamespace)
		{
			NSObjectTypeInfo resolved;
			
			// Resolve our base type
			if (type.BaseCliType == null) {
				if (TryResolveObjcToCli (type.BaseObjCType, out resolved)) {
					type.BaseCliType = resolved.CliName;
				} else {
					var reference = new GetClassTypeReference (defaultNamespace, provider.CreateValidIdentifier (type.BaseObjCType));
					type.BaseCliType = reference.Resolve (dom.Compilation).ReflectionName;

					var message = string.Format ("Failed to resolve Objective-C type '{0}' to a type in the current solution.", type.BaseObjCType);
					message += string.Format (" Adding a [Register (\"{0}\")] attribute to the class which corresponds to this will allow it to be synced to Objective-C.", type.BaseObjCType);
					monitor.ReportError (null, new UserException ("Error while syncing", message));
				}
			}
			
			// Resolve [Outlet] types
			foreach (var outlet in type.Outlets) {
				if (outlet.CliType != null)
					continue;
				
				if (TryResolveObjcToCli (outlet.ObjCType, out resolved)) {
					outlet.CliType = resolved.CliName;
				} else {
					outlet.CliType = defaultNamespace + "." + provider.CreateValidIdentifier (outlet.ObjCType);

					var message = string.Format ("Failed to resolve Objective-C outlet '{0}' of type '{1}' to a type in the current solution.", outlet.ObjCName, outlet.ObjCType);
					message += string.Format (" Adding a [Register (\"{0}\")] attribute to the class which corresponds to this will allow it to be synced to Objective-C.", outlet.ObjCType);
					monitor.ReportError (null, new UserException ("Error while syncing", message));
				}
			}
			
			// Resolve [Action] param types
			foreach (var action in type.Actions) {
				foreach (var param in action.Parameters) {
					if (param.CliType != null)
						continue;
					
					if (TryResolveObjcToCli (param.ObjCType, out resolved)) {
						param.CliType = resolved.CliName;
					} else {
						param.CliType = defaultNamespace + "." + provider.CreateValidIdentifier (param.ObjCType);
	
						var message = string.Format ("Failed to resolve paramater '{0}' of type '{2}' on Objective-C action '{1}' to a type in the current solution.", param.Name, action.ObjCName, param.ObjCType);
						message += string.Format (" Adding a [Register (\"{0}\")] attribute to the class which corresponds to this will allow it to be synced to Objective-C.", param.ObjCType);
						monitor.ReportError (null, new UserException ("Error while syncing", message));
					}
				}
			}
		}
	}
}
