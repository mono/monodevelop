
using System;
using System.Collections;
using MonoDevelop.Projects.Parser;

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
		
		public MethodParameterDataProvider (IClass cls, string methodName, Scope scope)
		{
			this.methodName = methodName;
			
			ArrayList methods = new ArrayList ();
			foreach (IMethod met in cls.Methods) {
				if (met.Name == methodName && ShouldAdd (met, scope)) 
					methods.Add (met);
			}
			Init (methods);
		}
		public MethodParameterDataProvider (IClass cls, string methodName) : this (cls, methodName, Scope.All)
		{
		}
		
		public MethodParameterDataProvider (IClass cls, Scope scope)
		{
			// Look for constructors
			
			ArrayList methods = new ArrayList ();
			foreach (IMethod met in cls.Methods) {
				if (met.IsConstructor && ShouldAdd (met, scope))
					methods.Add (met);
			}
			Init (methods);
		}
		public MethodParameterDataProvider (IClass cls) : this (cls, Scope.All)
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
