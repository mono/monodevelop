using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
    /// <summary>
    /// Represents a scope where the variables are statically known.
    /// </summary>
    [Serializable]
    public class DeclarativeScope : Scope
    {
        // An array of values - one element for each variable declared in the scope.
        private object[] values;

        /// <summary>
        /// Creates a new declarative scope for use inside a function body.
        /// </summary>
        /// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
        /// <param name="functionName"> The name of the function.  Can be empty for an anonymous function. </param>
        /// <param name="argumentNames"> The names of each of the function arguments. </param>
        /// <returns> A new DeclarativeScope instance. </returns>
        public static DeclarativeScope CreateFunctionScope(Scope parentScope, string functionName, IEnumerable<string> argumentNames)
        {
            if (parentScope == null)
                throw new ArgumentNullException("parentScope", "Function scopes must have a parent scope.");
            if (functionName == null)
                throw new ArgumentNullException("functionName");
            if (argumentNames == null)
                throw new ArgumentNullException("argumentNames");
            var result = new DeclarativeScope(parentScope, 0);
            if (string.IsNullOrEmpty(functionName) == false)
                result.DeclareVariable(functionName);
            result.DeclareVariable("this");
            result.DeclareVariable("arguments");
            foreach (var argumentName in argumentNames)
                result.DeclareVariable(argumentName);
            return result;
        }

        /// <summary>
        /// Creates a new declarative scope for use inside a catch statement.
        /// </summary>
        /// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
        /// <param name="catchVariableName"> The name of the catch variable. </param>
        /// <returns> A new DeclarativeScope instance. </returns>
        public static DeclarativeScope CreateCatchScope(Scope parentScope, string catchVariableName)
        {
            if (parentScope == null)
                throw new ArgumentNullException("parentScope", "Catch scopes must have a parent scope.");
            if (catchVariableName == null)
                throw new ArgumentNullException("catchVariableName");
            var result = new DeclarativeScope(parentScope, 0);
            result.DeclareVariable(catchVariableName);
            result.CanDeclareVariables = false;    // Only the catch variable can be declared in this scope.
            return result;
        }

        /// <summary>
        /// Creates a new declarative scope for use inside a strict mode eval statement.
        /// </summary>
        /// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
        /// <returns> A new DeclarativeScope instance. </returns>
        public static DeclarativeScope CreateEvalScope(Scope parentScope)
        {
            if (parentScope == null)
                throw new ArgumentNullException("parentScope", "Eval scopes must have a parent scope.");
            return new DeclarativeScope(parentScope, 0);
        }

        /// <summary>
        /// Creates a new declarative scope for use at runtime.
        /// </summary>
        /// <param name="parentScope"> A reference to the parent scope.  Can not be <c>null</c>. </param>
        /// <param name="declaredVariableNames"> The names of variables that were declared in this scope. </param>
        /// <returns> A new DeclarativeScope instance. </returns>
        public static DeclarativeScope CreateRuntimeScope(Scope parentScope, string[] declaredVariableNames)
        {
            if (parentScope == null)
                throw new ArgumentNullException("parentScope", "Function scopes must have a parent scope.");
            if (declaredVariableNames == null)
                throw new ArgumentNullException("declaredVariableNames");
            var result = new DeclarativeScope(parentScope, declaredVariableNames.Length);
            foreach (string variableName in declaredVariableNames)
                result.DeclareVariable(variableName);
            result.values = new object[result.DeclaredVariableCount];
            return result;
        }

        /// <summary>
        /// Creates a new DeclarativeScope instance.
        /// </summary>
        /// <param name="parentScope"> A reference to the parent scope, or <c>null</c> if this is
        /// the global scope. </param>
        /// <param name="declaredVariableCount"> The number of variables declared in this scope. </param>
        private DeclarativeScope(Scope parentScope, int declaredVariableCount)
            : base(parentScope, declaredVariableCount)
        {
        }

        /// <summary>
        /// Gets an array of values, one for each variable.  Only available for declarative scopes
        /// created using CreateRuntimeScope().
        /// </summary>
        public object[] Values
        {
            get { return this.values; }
        }

        /// <summary>
        /// Declares a variable or function in this scope.  This will be initialized with the value
        /// of the given expression.
        /// </summary>
        /// <param name="name"> The name of the variable. </param>
        /// <param name="valueAtTopOfScope"> The value of the variable at the top of the scope. </param>
        /// <param name="writable"> <c>true</c> if the variable can be modified; <c>false</c>
        /// otherwise. </param>
        /// <param name="deletable"> <c>true</c> if the variable can be deleted; <c>false</c>
        /// otherwise. </param>
        /// <returns> A reference to the variable that was declared. </returns>
        public override DeclaredVariable DeclareVariable(string name, Expression valueAtTopOfScope = null, bool writable = true, bool deletable = false)
        {
            // Variables can be added to a declarative scope using eval().  When this happens the
            // values array needs to be resized.  That check happens here.
            if (this.values != null && this.DeclaredVariableCount >= this.Values.Length)
                Array.Resize(ref this.values, this.DeclaredVariableCount + 10);

            // Delegate to the Scope class.
            return base.DeclareVariable(name, valueAtTopOfScope, writable, deletable);
        }

    }

}