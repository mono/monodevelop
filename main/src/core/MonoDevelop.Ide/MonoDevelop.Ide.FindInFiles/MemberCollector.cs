// 
// MemberCollector.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang
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
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.Ide.FindInFiles
{
	public static class MemberCollector
	{
		static bool MatchParameters (IMember a, IMember b)
		{
			return MatchParameters (a as IParameterizedMember, b as IParameterizedMember);
		}

		static bool MatchParameters (IParameterizedMember a, IParameterizedMember b)
		{
			if (a == null && b == null) return true;
			if (a == null || b == null) return false;

			return Equals (a.Compilation, a.Parameters, b.Parameters);
		}

		#region Code from NRefactory ParameterListComparer

		public static bool Equals(ICompilation comp, IList<IParameter> x, IList<IParameter> y)
		{
			if (x == y)
				return true;
			if (x == null || y == null || x.Count != y.Count)
				return false;
			for (int i = 0; i < x.Count; i++) {
				var a = x[i];
				var b = y[i];
				if (a == null && b == null)
					continue;
				if (a == null || b == null)
					return false;

				// We want to consider the parameter lists "Method<T>(T a)" and "Method<S>(S b)" as equal.
				// However, the parameter types are not considered equal, as T is a different type parameter than S.
				// In order to compare the method signatures, we will normalize all method type parameters.
				IType aType = a.Type.AcceptVisitor(normalizationVisitor);
				IType bType = b.Type.AcceptVisitor(normalizationVisitor);
				bType = comp.Import (bType);
				if (!aType.Equals(bType))
					return false;
			}
			return true;
		}

		sealed class NormalizeTypeVisitor : TypeVisitor
		{
			public override IType VisitTypeParameter(ITypeParameter type)
			{
				if (type.OwnerType == SymbolKind.Method) {
					return ICSharpCode.NRefactory.TypeSystem.Implementation.DummyTypeParameter.GetMethodTypeParameter(type.Index);
				} else {
					return base.VisitTypeParameter(type);
				}
			}

			public override IType VisitTypeDefinition(ITypeDefinition type)
			{
				if (type.KnownTypeCode == KnownTypeCode.Object)
					return SpecialType.Dynamic;
				return base.VisitTypeDefinition(type);
			}
		}

		static readonly NormalizeTypeVisitor normalizationVisitor = new NormalizeTypeVisitor();

		#endregion

		/// <summary>
		/// find all base types(types that are not derived from other types) in the specified types
		/// </summary>
		/// <param name="types"></param>
		/// <returns></returns>
		public static IEnumerable<ITypeDefinition> GetBaseTypes (IEnumerable<ITypeDefinition> types)
		{
			if (types == null)
				yield break;
			types = types.ToList ();
			if (!types.Any ())
				yield break;

			var baseType = types.FirstOrDefault ();
			var otherTypes = new List<ITypeDefinition> ();

			foreach (var type in types.Skip (1)) {
				if (baseType.IsDerivedFrom (type)) {
					baseType = type;
				} else if (!type.IsDerivedFrom (baseType)) {
					// this type is not directly related to baseType
					otherTypes.Add (type);
				}
			}
			yield return baseType;
			foreach (var type in GetBaseTypes (otherTypes))
				yield return type;
		}

		static IEnumerable<IMember> GetMembers (ITypeDefinition type, IMember member, bool ignoreInherited,
												Func<IMember, bool> filter)
		{
			var options = ignoreInherited ? GetMemberOptions.IgnoreInheritedMembers : GetMemberOptions.None;
			var members = type.GetMembers (m => m.Name == member.Name, options);

/*			// Filter out shadowed members.
			// class A { public string Foo { get; set; } } class B : A { public string Foo { get; set; } }
			if (member.SymbolKind == SymbolKind.Property || !(member is IParameterizedMember)) {
				members = members.Where (m => m == member || m.DeclaringType.Kind == TypeKind.Interface);
			}*/
			if (filter != null)
				members = members.Where (filter);
			return members;
		}

		static IEnumerable<ITypeDefinition> Import (ICompilation compilation, IEnumerable<ITypeDefinition> types)
		{
			return types.Select (t => compilation.Import (t));
		}

		/// <summary>
		/// collect members with the same signature/name(if overloads are included) as the specified member
		/// in the inheritance tree
		/// </summary>
		public static IEnumerable<IMember> CollectMembers (Solution solution, IMember member, ReferenceFinder.RefactoryScope scope,
			bool includeOverloads = true, bool matchDeclaringType = false)
		{
			if (solution == null || member.SymbolKind == SymbolKind.Destructor || member.SymbolKind == SymbolKind.Operator)
				return new [] { member };

			if (member.SymbolKind == SymbolKind.Constructor) {
				if (includeOverloads)
					return member.DeclaringType.GetMembers (m => m.SymbolKind == SymbolKind.Constructor, GetMemberOptions.IgnoreInheritedMembers);
				return new [] { member };
			}

			Func<IMember, bool> memberFilter = null;
			if (member is IParameterizedMember && !includeOverloads)
				memberFilter = m => MatchParameters (m, member);

			var declaringType = member.DeclaringTypeDefinition;
			if (declaringType == null)
				return new [] { member };
			// only collect members in declaringType
			if (matchDeclaringType)
				return GetMembers (declaringType, member, true, memberFilter);

			if (declaringType.Kind != TypeKind.Class && declaringType.Kind != TypeKind.Interface)
				return GetMembers (declaringType, member, false, memberFilter);

			var searchTypes = new List<ITypeDefinition> ();
			if (includeOverloads) {
				var interfaces = from t in declaringType.GetAllBaseTypeDefinitions ()
								 where t.Kind == TypeKind.Interface && GetMembers (t, member, true, memberFilter).Any ()
								 select t;
				searchTypes.AddRange (GetBaseTypes (interfaces));
			}

			if (member.DeclaringType.Kind == TypeKind.Class) {
				var members = GetMembers (declaringType, member, false, memberFilter).ToList ();
				if (members.Any (m => m.IsOverridable))
					searchTypes.AddRange (GetBaseTypes (members.Select (m => m.DeclaringTypeDefinition)));
				else if (searchTypes.Count == 0)
					return members;
			}

			IList<ICompilation> compilations;
			if (scope == ReferenceFinder.RefactoryScope.Solution || scope == ReferenceFinder.RefactoryScope.Unknown) {
				var projects = SearchCollector.CollectProjects (solution, searchTypes);
				compilations = projects.Select (TypeSystemService.GetCompilation).ToList ();
			} else {
				compilations = new [] { member.Compilation };
			}

			var result = new List<IMember> ();
			var mainAssemblies = new HashSet<string> (compilations.Select (c => c.MainAssembly.AssemblyName));
			var searchedAssemblies = new HashSet<string> ();
			var searchedTypes = new HashSet<string> ();

			foreach (var compilation in compilations) {
				var baseTypeImports = Import(compilation, searchTypes).Where (t => t != null).ToList ();
				if (!baseTypeImports.Any ()) continue;

				foreach (var assembly in compilation.Assemblies) {
					// search main assemblies in their projects' own compilation, to avoid possible resolving problems
					if ((mainAssemblies.Contains(assembly.AssemblyName) && assembly != compilation.MainAssembly) ||
						!searchedAssemblies.Add (assembly.AssemblyName))
						continue;

					foreach (var type in assembly.GetAllTypeDefinitions ()) {
						// members in base types will also be added
						// because IsDerivedFrom return true for a type itself
						if (!searchedTypes.Add (type.ReflectionName) || !baseTypeImports.Any (type.IsDerivedFrom))
							continue;
						result.AddRange (GetMembers (type, member, true, memberFilter));
					}
				}
			}
			if (!result.Contains (member))
				result.Add (member);
			return result;
		}
	
	}
}

