// MethodParameterDataProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using System.Collections;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Projects.Gui.Completion
{
	// This class can be used as base class for implementations of IParameterDataProvider
	
	public abstract class MethodParameterDataProvider: IParameterDataProvider
	{
		[Flags]
		public enum Scope 
		{
			Public,
			Private,
			Internal,
			Protected,
			All = Public | Private | Internal | Protected
		}
		IMethod[] listMethods;
		string methodName;
		
		public MethodParameterDataProvider (IType cls, string methodName, Scope scope)
		{
			this.methodName = methodName;
			
			ArrayList methods = new ArrayList ();
			foreach (IMethod met in cls.Methods) {
				if (met.Name == methodName && ShouldAdd (met, scope)) 
					methods.Add (met);
			}
			Init (methods);
		}
		public MethodParameterDataProvider (IType cls, string methodName) : this (cls, methodName, Scope.All)
		{
		}
		
		public MethodParameterDataProvider (IType cls, Scope scope)
		{
			// Look for constructors
			
			ArrayList methods = new ArrayList ();
			foreach (IMethod met in cls.Methods) {
				if (met.IsConstructor && ShouldAdd (met, scope))
					methods.Add (met);
			}
			Init (methods);
		}
		public MethodParameterDataProvider (IType cls) : this (cls, Scope.All)
		{
		}
		
		bool ShouldAdd (IMethod met, Scope scope)
		{
			return !((scope & Scope.Internal) != Scope.Internal && met.IsInternal ||
			         (scope & Scope.Private) != Scope.Private && met.IsPrivate ||
				     (scope & Scope.Protected) != Scope.Protected && met.IsProtected);
		}
		
		void Init (ArrayList methods)
		{
			// Sort the array of methods, so overloads with less parameters are shown first
			
			listMethods = new IMethod [methods.Count];
			methods.CopyTo (listMethods, 0);
			int[] parCount = new int [methods.Count];
			for (int n=0; n<methods.Count; n++)
				parCount[n] = listMethods [n].Parameters.Count;
			
			Array.Sort (parCount, listMethods, 0, parCount.Length);
		}
		
		public int OverloadCount {
			get { return listMethods.Length; }
		}
		
		public abstract int GetCurrentParameterIndex (ICodeCompletionContext ctx);
		
		public virtual string GetMethodMarkup (int overload, string[] parameters)
		{
			return methodName + " (" + string.Join (", ", parameters)  + ")";
		}
		
		public virtual string GetParameterMarkup (int overload, int paramIndex)
		{
			return GetParameter (overload, paramIndex).Name;
		}
		
		public virtual int GetParameterCount (int overload)
		{
			return GetMethod (overload).Parameters.Count;
		}
		
		protected IMethod GetMethod (int overload)
		{
			return (IMethod) listMethods [overload];
		}
		
		protected IParameter GetParameter (int overload, int paramIndex)
		{
			return GetMethod (overload).Parameters [paramIndex];
		}
	}
	
}
