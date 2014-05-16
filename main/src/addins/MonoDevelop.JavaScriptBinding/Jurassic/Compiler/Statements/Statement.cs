using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

    /// <summary>
    /// Represents a javascript statement.
    /// </summary>
    public abstract class Statement : JSAstNode
    {
        private List<string> labels;

        /// <summary>
        /// Creates a new Statement instance.
        /// </summary>
        /// <param name="labels"> The labels that are associated with this statement. </param>
        public Statement(IList<string> labels)
        {
            if (labels != null && labels.Count > 0)
                this.labels = new List<string>(labels);
        }

        /// <summary>
        /// Returns a value that indicates whether the statement has one or more labels attached to
        /// it.
        /// </summary>
        public bool HasLabels
        {
            get { return this.labels != null; }
        }

        /// <summary>
        /// Gets or sets the labels associated with this statement.
        /// </summary>
        public IList<string> Labels
        {
            get { return this.labels; }
        }

        /// <summary>
        /// Gets or sets the portion of source code associated with this statement.  For
        /// single-line statements this encompasses the whole statement but for multi-line (block)
        /// statements it only encompasses part of the statement.
        /// </summary>
        public SourceCodeSpan SourceSpan
        {
            get;
            set;
        }

        /// <summary>
        /// Converts the statement to a string.
        /// </summary>
        /// <param name="indentLevel"> The number of tabs to include before the statement. </param>
        /// <returns> A string representing this statement. </returns>
        public override string ToString()
        {
            return this.ToString(0);
        }

        /// <summary>
        /// Converts the statement to a string.
        /// </summary>
        /// <param name="indentLevel"> The number of tabs to include before the statement. </param>
        /// <returns> A string representing this statement. </returns>
        public abstract string ToString(int indentLevel);
    }

}