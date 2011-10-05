// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public static class ParameterListComparer
	{
		// We want to consider the parameter lists "Method<T>(T a)" and "Method<S>(S b)" as equal.
		// However, the parameter types are not considered equal, as T is a different type parameter than S.
		// In order to compare the method signatures, we will normalize all method type parameters.
		sealed class NormalizeMethodTypeParameters : TypeVisitor
		{
			public static readonly NormalizeMethodTypeParameters Instance = new NormalizeMethodTypeParameters();
			
			ITypeParameter[] normalTypeParameters = { new DefaultTypeParameter(EntityType.Method, 0, string.Empty) };
			
			public override IType VisitTypeParameter(ITypeParameter type)
			{
				if (type.OwnerType == EntityType.Method) {
					ITypeParameter[] tps = this.normalTypeParameters;
					while (type.Index >= tps.Length) {
						// We don't have a normal type parameter for this index, so we need to extend our array.
						// Because the array can be used concurrently from multiple threads, we have to use
						// Interlocked.CompareExchange.
						ITypeParameter[] newTps = new ITypeParameter[type.Index + 1];
						tps.CopyTo(newTps, 0);
						for (int i = tps.Length; i < newTps.Length; i++) {
							newTps[i] = new DefaultTypeParameter(EntityType.Method, i, string.Empty);
						}
						ITypeParameter[] oldTps = Interlocked.CompareExchange(ref normalTypeParameters, newTps, tps);
						if (oldTps == tps) {
							// exchange successful
							tps = newTps;
						} else {
							// exchange not successful
							tps = oldTps;
						}
					}
					return tps[type.Index];
				} else {
					return base.VisitTypeParameter(type);
				}
			}
		}
		
		public static bool Compare(ITypeResolveContext context, IParameterizedMember x, IParameterizedMember y)
		{
			var px = x.Parameters;
			var py = y.Parameters;
			if (px.Count != py.Count)
				return false;
			for (int i = 0; i < px.Count; i++) {
				var a = px[i];
				var b = py[i];
				if (a == null && b == null)
					continue;
				if (a == null || b == null)
					return false;
				IType aType = a.Type.Resolve(context);
				IType bType = b.Type.Resolve(context);
				
				aType = aType.AcceptVisitor(NormalizeMethodTypeParameters.Instance);
				bType = bType.AcceptVisitor(NormalizeMethodTypeParameters.Instance);
				
				if (!aType.Equals(bType))
					return false;
			}
			return true;
		}
		
		public static int GetHashCode(ITypeResolveContext context, IParameterizedMember obj)
		{
			int hashCode = obj.Parameters.Count;
			unchecked {
				foreach (IParameter p in obj.Parameters) {
					hashCode *= 27;
					IType type = p.Type.Resolve(context);
					type = type.AcceptVisitor(NormalizeMethodTypeParameters.Instance);
					hashCode += type.GetHashCode();
				}
			}
			return hashCode;
		}
	}
}
