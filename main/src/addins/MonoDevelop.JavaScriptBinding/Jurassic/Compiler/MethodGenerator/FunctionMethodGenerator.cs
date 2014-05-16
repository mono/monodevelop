using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
    /// <summary>
    /// Represents the information needed to compile a function.
    /// </summary>
    public class FunctionMethodGenerator : MethodGenerator
    {
        /// <summary>
        /// Creates a new FunctionMethodGenerator instance.
        /// </summary>
        /// <param name="engine"> The script engine. </param>
        /// <param name="scope"> The function scope. </param>
        /// <param name="functionName"> The name of the function. </param>
        /// <param name="includeNameInScope"> Indicates whether the name should be included in the function scope. </param>
        /// <param name="argumentNames"> The names of the arguments. </param>
        /// <param name="bodyText"> The source code of the function. </param>
        /// <param name="body"> The root of the abstract syntax tree for the body of the function. </param>
        /// <param name="scriptPath"> The URL or file system path that the script was sourced from. </param>
        /// <param name="options"> Options that influence the compiler. </param>
        public FunctionMethodGenerator(ScriptEngine engine, string functionName, bool includeNameInScope, IList<string> argumentNames, string bodyText, Statement body, string scriptPath, CompilerOptions options)
            : base(engine, new DummyScriptSource(scriptPath), options)
        {
            this.Name = functionName;
            this.IncludeNameInScope = includeNameInScope;
            this.ArgumentNames = argumentNames;
            this.BodyRoot = body;
            this.BodyText = bodyText;
            Validate();
        }

        /// <summary>
        /// Dummy implementation of ScriptSource.
        /// </summary>
        private class DummyScriptSource : ScriptSource
        {
            private string path;

            public DummyScriptSource(string path)
            {
                this.path = path;
            }

            public override string Path
            {
                get { return this.path; }
            }

            public override System.IO.TextReader GetReader()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Creates a new FunctionContext instance.
        /// </summary>
        /// <param name="engine"> The script engine. </param>
        /// <param name="scope"> The function scope. </param>
        /// <param name="functionName"> The name of the function. </param>
        /// <param name="argumentNames"> The names of the arguments. </param>
        /// <param name="body"> The source code for the body of the function. </param>
        /// <param name="options"> Options that influence the compiler. </param>
        public FunctionMethodGenerator(ScriptEngine engine, DeclarativeScope scope, string functionName, IList<string> argumentNames, string body, CompilerOptions options)
            : base(engine, new StringScriptSource(body), options)
        {
            this.Name = functionName;
            this.ArgumentNames = argumentNames;
            this.BodyText = body;
        }

        /// <summary>
        /// Gets the name of the function.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the display name for the function.  This is statically inferred from the
        /// context if the function is the target of an assignment or if the function is within an
        /// object literal.  Only set if the function name is empty.
        /// </summary>
        public string DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the function name should be included in the function scope (i.e. if
        /// this is <c>false</c>, then you cannot reference the function by name from within the
        /// body of the function).
        /// </summary>
        public bool IncludeNameInScope
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a list of argument names.
        /// </summary>
        public IList<string> ArgumentNames
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the root of the abstract syntax tree for the body of the function.
        /// </summary>
        public Statement BodyRoot
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the source code for the body of the function.
        /// </summary>
        public string BodyText
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a name for the generated method.
        /// </summary>
        /// <returns> A name for the generated method. </returns>
        protected override string GetMethodName()
        {
            if (this.DisplayName != null)
                return this.DisplayName;
            else if (string.IsNullOrEmpty(this.Name))
                return "anonymous";
            else
                return this.Name;
        }

        /// <summary>
        /// Gets a name for the function, as it appears in the stack trace.
        /// </summary>
        /// <returns> A name for the function, as it appears in the stack trace, or <c>null</c> if
        /// this generator is generating code in the global scope. </returns>
        protected override string GetStackName()
        {
            return GetMethodName();
        }

        /// <summary>
        /// Checks whether the function is valid (in strict mode the function cannot be named
        /// 'arguments' or 'eval' and the argument names cannot be duplicated).
        /// </summary>
        private void Validate()
        {
            if (this.StrictMode == true)
            {
                // If the function body is strict mode, then the function name cannot be 'eval' or 'arguments'.
                if (this.Name == "arguments" || this.Name == "eval")
                    throw new JavaScriptException(this.Engine, "SyntaxError", string.Format("Functions cannot be named '{0}' in strict mode.", this.Name));

                // If the function body is strict mode, then the argument names cannot be 'eval' or 'arguments'.
                foreach (var argumentName in this.ArgumentNames)
                    if (argumentName == "arguments" || argumentName == "eval")
                        throw new JavaScriptException(this.Engine, "SyntaxError", string.Format("Arguments cannot be named '{0}' in strict mode.", argumentName));

                // If the function body is strict mode, then the argument names cannot be duplicates.
                var duplicateCheck = new HashSet<string>();
                foreach (var argumentName in this.ArgumentNames)
                {
                    if (duplicateCheck.Contains(argumentName) == true)
                        throw new JavaScriptException(this.Engine, "SyntaxError", string.Format("Duplicate argument name '{0}' is not allowed in strict mode.", argumentName));
                    duplicateCheck.Add(argumentName);
                }
            }
        }

        /// <summary>
        /// Converts this object to a string.
        /// </summary>
        /// <returns> A string representing this object. </returns>
        public override string ToString()
        {
            if (this.BodyRoot != null)
                return string.Format("function {0}({1}) {2}", this.Name, StringHelpers.Join(", ", this.ArgumentNames), this.BodyRoot);
            return string.Format("function {0}({1}) {{\n{2}\n}}", this.Name, StringHelpers.Join(", ", this.ArgumentNames), this.BodyText);
        }
    }

}