// 
// DomAssemblyReflector.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.Addins.Database;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Dom.Parser;
using System.IO;
using MonoDevelop.Projects.Dom;
using System.CodeDom;
using System.Reflection;
using MA = Mono.Addins.Database;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.AddinAuthoring
{
	public class DomAssemblyReflector: IAssemblyReflector
	{
		Solution solution;
		List<FilePath> loadedDoms = new List<FilePath> ();
		
		public DomAssemblyReflector (Solution solution)
		{
			this.solution = solution;
		}
		
		#region IAssemblyReflector implementation
		public void Initialize (IAssemblyLocator locator)
		{
		}

		public object GetCustomAttribute (object obj, Type type, bool inherit)
		{
			foreach (object att in GetCustomAttributes (obj, type, inherit))
				if (type.IsInstanceOfType (att))
					return att;
			return null;
		}
		
		IEnumerable<IAttribute> GetAttributes (object ob)
		{
			if (ob is IMember)
				return ((IMember)ob).Attributes;
			else if (ob is IParameter)
				return ((IParameter)ob).Attributes;
			else if (ob is ProjectDom)
				return ((ProjectDom)ob).Attributes;
			else
				throw new NotSupportedException ();
		}

		public object[] GetCustomAttributes (object obj, Type type, bool inherit)
		{
			ArrayList atts = new ArrayList ();
			foreach (IAttribute att in GetAttributes (obj)) {
				object catt = ConvertAttribute (att, type);
				if (catt != null)
					atts.Add (catt);
			}
			if (inherit && (obj is IType)) {
				IType td = (IType) obj;
				if (td.BaseType != null && td.BaseType.FullName != "System.Object") {
					IType bt = td.SourceProjectDom.GetType (td.BaseType);
					if (bt != null)
						atts.AddRange (GetCustomAttributes (bt, type, true));
				}
			}
			return atts.ToArray ();
		}
		
		
		object ConvertAttribute (IAttribute att, Type expectedType)
		{
			Type attype = typeof(IAssemblyReflector).Assembly.GetType (att.AttributeType.FullName);

			if (attype == null || !expectedType.IsAssignableFrom (attype))
				return null;
			
			object ob;
			
			var args = att.PositionalArguments;
			if (args.Count > 0) {
				object[] cargs = new object [args.Count];
				ArrayList typeParameters = null;

				// Constructor parameters of type System.Type can't be set because types from the assembly
				// can't be loaded. The parameter value will be set later using a type name property.
				for (int n=0; n<cargs.Length; n++) {
					var res = Evaluate (args [n]);
					cargs [n] = res.Value;
					if (res.Type == "System.Type") {
						if (typeParameters == null)
							typeParameters = new ArrayList ();
						cargs [n] = typeof(object);
						typeParameters.Add (n);
					}
				}
				ob = Activator.CreateInstance (attype, cargs);
				
				// If there are arguments of type System.Type, set them using the property
				if (typeParameters != null) {
					Type[] ptypes = new Type [cargs.Length];
					for (int n=0; n<cargs.Length; n++) {
						ptypes [n] = cargs [n].GetType ();
					}
					ConstructorInfo ci = attype.GetConstructor (ptypes);
					ParameterInfo[] ciParams = ci.GetParameters ();
					
					for (int n=0; n<typeParameters.Count; n++) {
						int ip = (int) typeParameters [n];
						string propName = ciParams[ip].Name;
						propName = char.ToUpper (propName [0]) + propName.Substring (1) + "Name";
						PropertyInfo pi = attype.GetProperty (propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

						if (pi == null)
							throw new InvalidOperationException ("Property '" + propName + "' not found in type '" + attype + "'.");

						pi.SetValue (ob, Evaluate (args [ip]).Value, null);
					}
				}
			} else {
				ob = Activator.CreateInstance (attype);
			}
			
			foreach (var namedArgument in att.NamedArguments) {
				string pname = namedArgument.Key;
				PropertyInfo prop = attype.GetProperty (pname);
				var res = Evaluate (namedArgument.Value);
				if (prop != null) {
					if (prop.PropertyType == typeof(System.Type)) {
						// We can't load the type. We have to use the typeName property instead.
						pname += "Name";
						prop = attype.GetProperty (pname, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						
						if (prop == null)
							throw new InvalidOperationException ("Property '" + pname + "' not found in type '" + attype + "'.");
					}
					prop.SetValue (ob, res.Value, null);
				}
			}
			return ob;
		}
		
		public List<MA.CustomAttribute> GetRawCustomAttributes (object obj, Type type, bool inherit)
		{
			ProjectDom dom;
			if (obj is ProjectDom)
				dom = (ProjectDom)obj;
			else if (obj is IType)
				dom = ((IType)obj).SourceProjectDom;
			else if (obj is IMember)
				dom = ((IMember)obj).DeclaringType.SourceProjectDom;
			else if (obj is IParameter)
				dom = ((IParameter)obj).DeclaringMember.DeclaringType.SourceProjectDom;
			else
				throw new NotSupportedException ();
			
			List<MA.CustomAttribute> atts = new List<MA.CustomAttribute> ();
			
			foreach (IAttribute att in GetAttributes (obj)) {
				MA.CustomAttribute catt = ConvertToRawAttribute (dom, att, type.FullName);
				if (catt != null)
					atts.Add (catt);
			}
			if (inherit && (obj is IType)) {
				IType td = (IType) obj;
				if (td.BaseType != null && td.BaseType.FullName != "System.Object") {
					IType bt = td.SourceProjectDom.GetType (td.BaseType);
					if (bt != null)
						atts.AddRange (GetRawCustomAttributes (bt, type, true));
				}
			}
			return atts;
		}
		
		
		MA.CustomAttribute ConvertToRawAttribute (ProjectDom dom, IAttribute att, string expectedType)
		{
			IType attType = dom.GetType (att.AttributeType);
			
			if (attType == null || !TypeIsAssignableFrom (expectedType, attType))
				return null;

			MA.CustomAttribute mat = new MA.CustomAttribute ();
			mat.TypeName = att.AttributeType.FullName;
			
			var arguments = att.PositionalArguments;
			if (arguments.Count > 0) {
				
				IMethod constructor = FindConstructor (dom, att);
				if (constructor == null)
					throw new InvalidOperationException ("Custom attribute constructor not found");

				for (int n=0; n<arguments.Count; n++) {
					IParameter par = constructor.Parameters[n];
					object val = Evaluate (arguments [n]).Value;
					if (val != null) {
						string name = par.Name;
						NodeAttributeAttribute bat = (NodeAttributeAttribute) GetCustomAttribute (par, typeof(NodeAttributeAttribute), false);
						if (bat != null)
							name = bat.Name;
						mat.Add (name, Convert.ToString (val, System.Globalization.CultureInfo.InvariantCulture));
					}
				}
			}
			
			foreach (var namedArgument in att.NamedArguments) {
				string pname = namedArgument.Key;
				object val = Evaluate (namedArgument.Value).Value;
				if (val == null)
					continue;

				foreach (IType td in GetInheritanceChain (attType)) {
					IMember prop = GetMember (td.Members, pname);
					if (prop == null)
						continue;

					NodeAttributeAttribute bat = (NodeAttributeAttribute) GetCustomAttribute (prop, typeof(NodeAttributeAttribute), false);
					if (bat != null) {
						string name = string.IsNullOrEmpty (bat.Name) ? prop.Name : bat.Name;
						mat.Add (name, Convert.ToString (val, System.Globalization.CultureInfo.InvariantCulture));
					}
				}
			}

			return mat;
		}

		static TMember GetMember<TMember> (IEnumerable<TMember> members, string name) where TMember : class, IMember
		{
			foreach (var member in members)
				if (member.Name == name)
					return member;

			return null;
		}
		
		IEnumerable<IType> GetInheritanceChain (IType td)
		{
			yield return td;
			while (td != null && td.BaseType != null && td.BaseType.FullName != "System.Object") {
				td = td.SourceProjectDom.GetType (td.BaseType);
				if (td != null)
					yield return td;
			}
		}

		IMethod FindConstructor (ProjectDom dom, IAttribute att)
		{
			// The constructor provided by CustomAttribute.Constructor is lacking some information, such as the parameter
			// name and custom attributes. Since we need the full info, we have to look it up in the declaring type.
			
			IType atd = dom.GetType (att.AttributeType);
			foreach (IMethod met in atd.Methods) {
				if (met.IsConstructor)
					continue;

				if (met.Parameters.Count == att.PositionalArguments.Count) {
					for (int n = met.Parameters.Count - 1; n >= 0; n--) {
						var res = Evaluate (att.PositionalArguments [n]);
						if (met.Parameters[n].ReturnType.FullName != res.Type)
							break;
						if (n == 0)
							return met;
					}
				}
			}
			return null;
		}		
		
		EvalResult Evaluate (CodeExpression exp)
		{
			if (exp is CodePrimitiveExpression) {
				CodePrimitiveExpression pe = (CodePrimitiveExpression) exp;
				return new EvalResult () { Type = pe.Value.GetType ().FullName, Value = pe.Value };
			}
			else if (exp is CodeTypeOfExpression) {
				CodeTypeOfExpression ce = (CodeTypeOfExpression) exp;
				return new EvalResult () { Type = "System.Type", Value = ce.Type.BaseType };
			}
			else
				throw new NotSupportedException ();
		}
		
		class EvalResult
		{
			public string Type;
			public object Value;
		}
		
		public object LoadAssembly (string file)
		{
			DotNetProject project = null;
			foreach (DotNetProject p in solution.GetAllSolutionItems<DotNetProject> ()) {
				foreach (var conf in p.Configurations) {
					if (p.GetOutputFileName (conf.Selector) == file) {
						project = p;
						break;
					}
				}
			}
			if (project != null)
				return ProjectDomService.GetProjectDom (project);
			else {
				if (!loadedDoms.Contains (file)) {
					loadedDoms.Add (file);
					ProjectDomService.LoadAssembly (Runtime.SystemAssemblyService.DefaultRuntime, file);
				}
				return ProjectDomService.GetAssemblyDom (Runtime.SystemAssemblyService.DefaultRuntime, file);
			}
		}
		
		public void UnloadAssemblyDoms ()
		{
			foreach (var f in loadedDoms)
				ProjectDomService.UnloadAssembly (Runtime.SystemAssemblyService.DefaultRuntime, f);
			loadedDoms.Clear ();
		}

		public object LoadAssemblyFromReference (object asmReference)
		{
			return asmReference;
		}

		public string[] GetResourceNames (object asm)
		{
			DotNetProject p = (DotNetProject) ((ProjectDom) asm).Project;
			List<string> res = new List<string> ();
			foreach (ProjectFile f in p.Files) {
				if (f.BuildAction == BuildAction.EmbeddedResource)
					res.Add (f.ResourceId);
			}
			return res.ToArray ();
		}

		public System.IO.Stream GetResourceStream (object asm, string resourceName)
		{
			DotNetProject p = (DotNetProject) ((ProjectDom) asm).Project;
			foreach (ProjectFile f in p.Files) {
				if (f.BuildAction == BuildAction.EmbeddedResource && f.ResourceId == resourceName)
					return File.OpenRead (f.FilePath);
			}
			throw new Exception ("Resource '" + resourceName + "' not found");
		}

		public IEnumerable GetAssemblyTypes (object asm)
		{
			ProjectDom dom = (ProjectDom) asm;
			return dom.Types;
		}

		public IEnumerable GetAssemblyReferences (object asm)
		{
			ProjectDom dom = (ProjectDom) asm;
			return dom.References;
		}

		public object GetType (object asm, string typeName)
		{
			ProjectDom dom = (ProjectDom) asm;
			return dom.GetType (typeName);
		}

		public string GetTypeName (object type)
		{
			IType t = (IType) type;
			return t.Name;
		}

		public string GetTypeFullName (object type)
		{
			IType t = (IType) type;
			return t.FullName;
		}

		public string GetTypeAssemblyQualifiedName (object type)
		{
			IType t = (IType) type;
			DotNetProject p = (DotNetProject) t.SourceProjectDom.Project;
			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) p.GetConfiguration (ConfigurationSelector.Default);
			return t.FullName + ", " + conf.CompiledOutputName.FileNameWithoutExtension;
		}

		public IEnumerable GetBaseTypeFullNameList (object type)
		{
			IType t = (IType) type;

			ArrayList list = new ArrayList ();
			Hashtable visited = new Hashtable ();
			GetBaseTypeFullNameList (visited, list, t);
			list.Remove (t.FullName);
			return list;
		}

		void GetBaseTypeFullNameList (Hashtable visited, ArrayList list, IType tr)
		{
			if (tr.FullName == "System.Object" || visited.Contains (tr.FullName))
				return;
			
			visited [tr.FullName] = tr;
			list.Add (tr.FullName);
			
			if (tr.BaseType != null) {
				IType bt = tr.SourceProjectDom.GetType (tr.BaseType);
				if (bt != null)
					GetBaseTypeFullNameList (visited, list, bt);
			}

			foreach (IReturnType interf in tr.ImplementedInterfaces) {
				IType bt = tr.SourceProjectDom.GetType (interf);
				if (bt != null)
					GetBaseTypeFullNameList (visited, list, bt);
			}
		}

		public bool TypeIsAssignableFrom (object baseType, object type)
		{
			IType tbase = (IType) baseType;
			IType ttype = (IType) type;
			return ttype.IsBaseType (tbase.ReturnType);
		}

		public bool TypeIsAssignableFrom (string baseTypeName, object type)
		{
			foreach (string bt in GetBaseTypeFullNameList (type))
				if (bt == baseTypeName)
					return true;
			return false;
		}

		public IEnumerable GetFields (object type)
		{
			IType t = (IType) type;
			return t.Fields;
		}

		public string GetFieldName (object field)
		{
			IField f = (IField) field;
			return f.Name;
		}

		public string GetFieldTypeFullName (object field)
		{
			IField f = (IField) field;
			return f.ReturnType.FullName;
		}
		#endregion
	}
}

