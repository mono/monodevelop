using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

    /// <summary>
    /// Represents the debugger statement.
    /// </summary>
    public class DebuggerStatement : Statement
    {
        /// <summary>
        /// Creates a new DebuggerStatement instance.
        /// </summary>
        /// <param name="labels"> The labels that are associated with this statement. </param>
        public DebuggerStatement(IList<string> labels)
            : base(labels)
        {
        }

        /// <summary>
        /// Converts the statement to a string.
        /// </summary>
        /// <param name="indentLevel"> The number of tabs to include before the statement. </param>
        /// <returns> A string representing this statement. </returns>
        public override string ToString(int indentLevel)
        {
            return new string('\t', indentLevel) + "debugger;";
        }
    }

}