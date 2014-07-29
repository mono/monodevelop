using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Represents an enclosing context where variables are uniquely defined.
	/// </summary>
	[Serializable]
	public abstract class Scope
	{
		// A dictionary containing the variables declared in this scope.
		private Dictionary<string, DeclaredVariable> variables;

		/// <summary>
		/// Represents a variable declared in a scope.
		/// </summary>
		[Serializable]
		public class DeclaredVariable
		{
			// The scope the variable was declared in.
			public Scope Scope;

			// The index of the variable (in the order it was added).
			public int Index;

			// The name of the variable.
			public string Name;

			// The initial value of the variable (used for function declarations only).
			[NonSerialized]
			public Expression ValueAtTopOfScope;

			// true if the variable has been set with the initial value.
			public bool Initialized;

			// true if the variable can be modified.
			public bool Writable;

			// true if the variable can be deleted.
			public bool Deletable;

			// The statically-determined storage type for the variable.
			[NonSerialized]
			public PrimitiveType Type = PrimitiveType.Any;
		}

		/// <summary>
		/// Creates a new Scope instance.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope, or <c>null</c> if this is
		/// the global scope. </param>
		protected Scope (Scope parentScope)
			: this (parentScope, 0)
		{
		}

		/// <summary>
		/// Creates a new Scope instance.
		/// </summary>
		/// <param name="parentScope"> A reference to the parent scope, or <c>null</c> if this is
		/// the global scope. </param>
		/// <param name="declaredVariableCount"> The number of variables declared in this scope. </param>
		protected Scope (Scope parentScope, int declaredVariableCount)
		{
			ParentScope = parentScope;
			variables = new Dictionary<string, DeclaredVariable> (declaredVariableCount);
			CanDeclareVariables = true;
			ExistsAtRuntime = true;
		}

		/// <summary>
		/// Gets a reference to the parent scope.  Can be <c>null</c> if this is the global scope.
		/// </summary>
		public Scope ParentScope {
			get;
			private set;
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the scope exists at runtime.  Defaults to
		/// <c>true</c>; will only be false if GenerateScopeCreation() has been called and the
		/// scope was optimized away.
		/// </summary>
		public bool ExistsAtRuntime {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value indicating whether variables can be declared within the scope.
		/// </summary>
		public bool CanDeclareVariables {
			get;
			protected set;
		}

		/// <summary>
		/// Gets the number of variables declared in this scope.
		/// </summary>
		public int DeclaredVariableCount {
			get { return variables.Count; }
		}

		/// <summary>
		/// Gets an enumerable list of the names of all the declared variables (including function
		/// declarations), listed in the order they were declared.
		/// </summary>
		public IEnumerable<string> DeclaredVariableNames {
			get {
				var declaredVariables = new List<DeclaredVariable> (variables.Values);
				declaredVariables.Sort ((a, b) => a.Index - b.Index);
				var names = new string[declaredVariables.Count];
				for (int i = 0; i < declaredVariables.Count; i++)
					names [i] = declaredVariables [i].Name;
				return names;
			}
		}

		/// <summary>
		/// Gets an enumerable list of the declared variables, in no particular order.
		/// </summary>
		public IEnumerable<DeclaredVariable> DeclaredVariables {
			get { return variables.Values; }
		}

		/// <summary>
		/// Gets the index of the given variable.
		/// </summary>
		/// <param name="variableName"> The name of the variable. </param>
		/// <returns> The index of the given variable, or <c>-1</c> if the variable doesn't exist
		/// in the scope. </returns>
		public DeclaredVariable GetDeclaredVariable (string variableName)
		{
			if (variableName == null)
				throw new ArgumentNullException ("variableName");
			DeclaredVariable variable;
			if (!variables.TryGetValue (variableName, out variable))
				return null;
			return variable;
		}

		/// <summary>
		/// Returns <c>true</c> if the given variable has been declared in this scope.
		/// </summary>
		/// <param name="name"> The name of the variable. </param>
		/// <returns> <c>true</c> if the given variable has been declared in this scope;
		/// <c>false</c> otherwise. </returns>
		public bool HasDeclaredVariable (string name)
		{
			return variables.ContainsKey (name);
		}

		/// <summary>
		/// Declares a variable or function in this scope.  This will be initialized with the value
		/// of the given expression.
		/// </summary>
		/// <param name="name"> The name of the variable. </param>
		/// <param name="valueAtTopOfScope"> The value of the variable at the top of the scope.
		/// Can be <c>null</c> to indicate the variable does not need initializing. </param>
		/// <param name="writable"> <c>true</c> if the variable can be modified; <c>false</c>
		/// otherwise.  Defaults to <c>true</c>. </param>
		/// <param name="deletable"> <c>true</c> if the variable can be deleted; <c>false</c>
		/// otherwise.  Defaults to <c>true</c>. </param>
		/// <returns> A reference to the variable that was declared. </returns>
		public virtual DeclaredVariable DeclareVariable (string name, Expression valueAtTopOfScope = null, bool writable = true, bool deletable = false)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			// If variables cannot be declared in the scope, try the parent scope instead.
			if (!CanDeclareVariables) {
				if (ParentScope == null)
					throw new InvalidOperationException ("Invalid scope chain.");
				return ParentScope.DeclareVariable (name, valueAtTopOfScope, writable, deletable);
			}

			DeclaredVariable variable;
			variables.TryGetValue (name, out variable);
			if (variable == null) {
				// This is a local variable that has not been declared before.
				variable = new DeclaredVariable () {
					Scope = this,
					Index = variables.Count,
					Name = name,
					Writable = writable,
					Deletable = deletable
				};
				variables.Add (name, variable);
			}

			// Set the initial value, if one was provided.
			if (valueAtTopOfScope != null) {
				// Function expressions override literals.
				if (!(valueAtTopOfScope is LiteralExpression && variable.ValueAtTopOfScope is FunctionExpression))
					variable.ValueAtTopOfScope = valueAtTopOfScope;
			}

			return variable;
		}

		/// <summary>
		/// Removes a declared variable from the scope.
		/// </summary>
		/// <param name="name"> The name of the variable. </param>
		public void RemovedDeclaredVariable (string name)
		{
			variables.Remove (name);
		}
	}

}