using System;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a scope which is backed by the properties of an object.
	/// </summary>
	[Serializable]
	public class ObjectScope : Scope
	{
		Library.ObjectInstance scopeObject;

		[NonSerialized]
		Expression scopeObjectExpression;

		bool providesImplicitThisValue;

		/// <summary>
		/// Creates a new global object scope.
		/// </summary>
		/// <returns> A new ObjectScope instance. </returns>
		public static ObjectScope CreateGlobalScope (Library.GlobalObject globalObject)
		{
			if (globalObject == null)
				throw new ArgumentNullException ("globalObject");
			return new ObjectScope (null) { ScopeObject = globalObject };
		}

		/// <summary>
		/// Creates a new object scope for use inside a with statement.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		/// <param name="scopeObject"> An expression that evaluates to the object to use. </param>
		/// <returns> A new ObjectScope instance. </returns>
		public static ObjectScope CreateWithScope (Scope parentScope, Expression scopeObject)
		{
			if (parentScope == null)
				throw new ArgumentException ("With scopes must have a parent scope.");
			return new ObjectScope (parentScope) {
				ScopeObjectExpression = scopeObject,
				ProvidesImplicitThisValue = true,
				CanDeclareVariables = false
			};
		}

		///// <summary>
		///// Creates a new object scope for use inside a with statement.
		///// </summary>
		///// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		///// <param name="scopeObject"> An expression that evaluates to the object to use. </param>
		///// <returns> A new ObjectScope instance. </returns>
		//public static ObjectScope CreateWithScope(Scope parentScope, Library.ObjectInstance scopeObject)
		//{
		//    if (parentScope == null)
		//        throw new ArgumentException("With scopes must have a parent scope.");
		//    return new ObjectScope(parentScope) { ScopeObject = scopeObject, ProvidesImplicitThisValue = true };
		//}

		/// <summary>
		/// Creates a new object scope for use at runtime.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
		/// <param name="scopeObject"> An expression that evaluates to the object to use. </param>
		/// <param name="providesImplicitThisValue"> Indicates whether an implicit "this" value is
		/// supplied to function calls in this scope. </param>
		/// <param name="canDeclareVariables"> Indicates whether variables can be declared within
		/// the scope. </param>
		/// <returns> A new ObjectScope instance. </returns>
		public static ObjectScope CreateRuntimeScope (Scope parentScope, Library.ObjectInstance scopeObject, bool providesImplicitThisValue, bool canDeclareVariables)
		{
			return new ObjectScope (parentScope) {
				ScopeObject = scopeObject,
				ProvidesImplicitThisValue = providesImplicitThisValue,
				CanDeclareVariables = canDeclareVariables
			};
		}

		/// <summary>
		/// Creates a new ObjectScope instance.
		/// </summary>
		ObjectScope (Scope parentScope)
			: base (parentScope)
		{
			ScopeObjectExpression = null;
			ProvidesImplicitThisValue = false;
		}

		/// <summary>
		/// Gets the object that stores the values of the variables in the scope.
		/// </summary>
		public Library.ObjectInstance ScopeObject {
			get { return scopeObject; }
			private set { scopeObject = value; }
		}

		/// <summary>
		/// Gets an expression that evaluates to the scope object.  <c>null</c> if the scope object
		/// is the global object.
		/// </summary>
		public Expression ScopeObjectExpression {
			get { return scopeObjectExpression; }
			private set { scopeObjectExpression = value; }
		}

		/// <summary>
		/// Gets a value that indicates whether an implicit "this" value is supplied to function
		/// calls in this scope.
		/// </summary>
		public bool ProvidesImplicitThisValue {
			get { return providesImplicitThisValue; }
			private set { providesImplicitThisValue = value; }
		}
	}
}