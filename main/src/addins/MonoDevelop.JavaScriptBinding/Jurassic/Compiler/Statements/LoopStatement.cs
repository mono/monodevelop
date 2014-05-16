using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

    /// <summary>
    /// Represents a javascript loop statement (for, for-in, while and do-while).
    /// </summary>
    public abstract class LoopStatement : Statement
    {
        /// <summary>
        /// Creates a new LoopStatement instance.
        /// </summary>
        /// <param name="labels"> The labels that are associated with this statement. </param>
        public LoopStatement(IList<string> labels)
            : base(labels)
        {
        }

        /// <summary>
        /// Gets or sets the statement that initializes the loop variable.
        /// </summary>
        public Statement InitStatement
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the var statement that initializes the loop variable.
        /// </summary>
        public VarStatement InitVarStatement
        {
            get { return this.InitStatement as VarStatement; }
        }

        /// <summary>
        /// Gets the expression that initializes the loop variable.
        /// </summary>
        public Expression InitExpression
        {
            get { return (this.InitStatement is ExpressionStatement) == true ? ((ExpressionStatement)this.InitStatement).Expression : null; }
        }

        /// <summary>
        /// Gets or sets the statement that checks whether the loop should terminate.
        /// </summary>
        public ExpressionStatement ConditionStatement
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the expression that checks whether the loop should terminate.
        /// </summary>
        public Expression Condition
        {
            get { return this.ConditionStatement.Expression; }
        }

        /// <summary>
        /// Gets or sets the statement that increments (or decrements) the loop variable.
        /// </summary>
        public ExpressionStatement IncrementStatement
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the expression that increments (or decrements) the loop variable.
        /// </summary>
        public Expression Increment
        {
            get { return this.IncrementStatement.Expression; }
        }

        /// <summary>
        /// Gets or sets the loop body.
        /// </summary>
        public Statement Body
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value that indicates whether the condition should be checked at the end of the
        /// loop.
        /// </summary>
        protected virtual bool CheckConditionAtEnd
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an enumerable list of child nodes in the abstract syntax tree.
        /// </summary>
        public override IEnumerable<JSAstNode> ChildNodes
        {
            get
            {
                if (this.InitStatement != null)
                    yield return this.InitStatement;
                if (this.CheckConditionAtEnd == false && this.ConditionStatement != null)
                    yield return this.ConditionStatement;
                yield return this.Body;
                if (this.IncrementStatement != null)
                    yield return this.IncrementStatement;
                if (this.CheckConditionAtEnd == true && this.ConditionStatement != null)
                    yield return this.ConditionStatement;
            }
        }
    }

}