//
// BaseAssemblyResolver.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// This code is a modified version of the assembly resolver implemented in Cecil.
// Keep in synch as much as possible.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using Mono.Cecil;

namespace Stetic {

	internal class AssemblyResolver : BaseAssemblyResolver {

		Hashtable _assemblies;

		public IDictionary AssemblyCache {
			get { return _assemblies; }
		}

		public AssemblyResolver ()
		{
			_assemblies = new Hashtable ();
		}

		public override AssemblyDefinition Resolve (AssemblyNameReference name)
		{
			AssemblyDefinition asm = (AssemblyDefinition) _assemblies [name.Name];
			if (asm == null) {
				asm = base.Resolve (name);
				asm.Resolver = this;
				_assemblies [name.Name] = asm;
			}

			return asm;
		}

		public TypeDefinition Resolve (TypeReference type)
		{
			if (type is TypeDefinition)
				return (TypeDefinition) type;

			AssemblyNameReference reference = type.Scope as AssemblyNameReference;
			if (reference != null) {
				AssemblyDefinition assembly = Resolve (reference);
				return assembly.MainModule.Types [type.FullName];
			}

			ModuleDefinition module = type.Scope as ModuleDefinition;
			if (module != null)
				return module.Types [type.FullName];

			throw new NotImplementedException ();
		}

		public FieldDefinition Resolve (FieldReference field)
		{
			TypeDefinition type = Resolve (field.DeclaringType);
			return GetField (type.Fields, field);
		}

		static FieldDefinition GetField (ICollection collection, FieldReference reference)
		{
			foreach (FieldDefinition field in collection) {
				if (field.Name != reference.Name)
					continue;

				if (!AreSame (field.FieldType, reference.FieldType))
					continue;

				return field;
			}

			return null;
		}

		public MethodDefinition Resolve (MethodReference method)
		{
			TypeDefinition type = Resolve (method.DeclaringType);
			if (method.Name == MethodDefinition.Cctor || method.Name == MethodDefinition.Ctor)
				return GetMethod (type.Constructors, method);
			else
				return GetMethod (type, method);
		}

		MethodDefinition GetMethod (TypeDefinition type, MethodReference reference)
		{
			while (type != null) {
				MethodDefinition method = GetMethod (type.Methods, reference);
				if (method == null)
					type = Resolve (type.BaseType);
				else
					return method;
			}

			return null;
		}

		static MethodDefinition GetMethod (ICollection collection, MethodReference reference)
		{
			foreach (MethodDefinition meth in collection) {
				if (meth.Name != reference.Name)
					continue;

				if (!AreSame (meth.ReturnType.ReturnType, reference.ReturnType.ReturnType))
					continue;

				if (!AreSame (meth.Parameters, reference.Parameters))
					continue;

				return meth;
			}

			return null;
		}

		static bool AreSame (ParameterDefinitionCollection a, ParameterDefinitionCollection b)
		{
			if (a.Count != b.Count)
				return false;

			if (a.Count == 0)
				return true;

			for (int i = 0; i < a.Count; i++)
				if (!AreSame (a [i].ParameterType, b [i].ParameterType))
					return false;

			return true;
		}

		static bool AreSame (TypeReference a, TypeReference b)
		{
			while (a is TypeSpecification || b is TypeSpecification) {
				if (a.GetType () != b.GetType ())
					return false;

				a = ((TypeSpecification) a).ElementType;
				b = ((TypeSpecification) b).ElementType;
			}

			if (a is GenericParameter || b is GenericParameter) {
				if (a.GetType() != b.GetType())
					return false;

				GenericParameter pa = (GenericParameter) a;
				GenericParameter pb = (GenericParameter) b;

				return pa.Position == pb.Position;
			}

			return a.FullName == b.FullName;
		}

		public void CacheAssembly (AssemblyDefinition assembly)
		{
			_assemblies [assembly.Name.FullName] = assembly;
			assembly.Resolver = this;
		}

		public string Resolve (AssemblyNameReference name, StringCollection basePaths)
		{
			string [] exts = new string [] { ".dll", ".exe" };

			if (basePaths != null) {
				foreach (string dir in basePaths) {
					foreach (string ext in exts) {
						string file = Path.Combine (dir, name.Name + ext);
						if (File.Exists (file))
							return file;
					}
				}
			}

			if (name.Name == "mscorlib")
				return GetCorlib (name);

			string asm = GetAssemblyInGac (name);
			if (asm != null)
				return asm;

			throw new FileNotFoundException ("Could not resolve: " + name);
		}

		string GetCorlib (AssemblyNameReference reference)
		{
			if (typeof (object).Assembly.GetName ().Version == reference.Version)
				return typeof (object).Assembly.Location;

			string path = Directory.GetParent (
				Directory.GetParent (
					typeof (object).Module.FullyQualifiedName).FullName
				).FullName;

			if (OnMono ()) {
				if (reference.Version.Major == 1)
					path = Path.Combine (path, "1.0");
				else if (reference.Version.Major == 2)
					path = Path.Combine (path, "2.0");
				else
					throw new NotSupportedException ("Version not supported: " + reference.Version);
			} else {
				if (reference.Version.ToString () == "1.0.3300.0")
					path = Path.Combine (path, "v1.0.3705");
				else if (reference.Version.ToString () == "1.0.5000.0")
					path = Path.Combine (path, "v1.1.4322");
				else if (reference.Version.ToString () == "2.0.0.0")
					path = Path.Combine (path, "v2.0.50727");
				else
					throw new NotSupportedException ("Version not supported: " + reference.Version);
			}

			return Path.Combine (path, "mscorlib.dll");
		}

		static string GetAssemblyInGac (AssemblyNameReference reference)
		{
			if (reference.PublicKeyToken == null || reference.PublicKeyToken.Length == 0)
				return null;

			string currentGac = GetCurrentGacPath ();
			if (OnMono ()) {
				string s = GetAssemblyFile (reference, currentGac);
				if (File.Exists (s))
					return s;
			} else {
				string [] gacs = new string [] {"GAC_MSIL", "GAC_32", "GAC"};
				for (int i = 0; i < gacs.Length; i++) {
					string gac = Path.Combine (Directory.GetParent (currentGac).FullName, gacs [i]);
					string asm = GetAssemblyFile (reference, gac);
					if (Directory.Exists (gac) && File.Exists (asm))
						return asm;
				}
			}

			return null;
		}

		static string GetAssemblyFile (AssemblyNameReference reference, string gac)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (reference.Version);
			sb.Append ("__");
			for (int i = 0; i < reference.PublicKeyToken.Length; i++)
				sb.Append (reference.PublicKeyToken [i].ToString ("x2"));

			return Path.Combine (
				Path.Combine (
					Path.Combine (gac, reference.Name), sb.ToString ()),
					string.Concat (reference.Name, ".dll"));
		}

		static string GetCurrentGacPath ()
		{
			return Directory.GetParent (
				Directory.GetParent (
					Path.GetDirectoryName (
						typeof (Uri).Module.FullyQualifiedName)
					).FullName
				).FullName;
		}
	}
}
