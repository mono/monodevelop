#region license
//
//	(C) 2005 - 2007 db4objects Inc. http://www.db4o.com
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;

using Cecil.Decompiler.Ast;
using Cecil.Decompiler.Cil;
using Cecil.Decompiler.Steps;

namespace Cecil.Decompiler {

	public class StatementDecompiler : BaseInstructionVisitor, IDecompilationStep {

		List<Statement> [] statements;
		HashSet<InstructionBlock> processed;
		AnnotationStore annotations;
		Dictionary<VariableReference, Expression> assignments;

		ExpressionDecompiler expression_decompiler;
		MethodBody body;
		VariableDefinitionCollection variables;
		ControlFlowGraph cfg;
		BlockOptimization optimization;

		InstructionBlock current_block;
		string current_label;

		public StatementDecompiler (BlockOptimization optimization)
		{
			this.optimization = optimization;
		}

		public BlockStatement Process (DecompilationContext context, BlockStatement body)
		{
			this.cfg = context.ControlFlowGraph;
			this.annotations = AnnotationStore.CreateStore (cfg, optimization);
			this.body = context.Body;
			this.variables = context.Variables;

			this.expression_decompiler = new ExpressionDecompiler (context.Method, annotations);
			this.statements = new List<Statement> [cfg.Blocks.Length];
			this.processed = new HashSet<InstructionBlock> ();
			this.assignments = new Dictionary<VariableReference, Expression> ();

			Run ();

			PopulateBodyBlock (body);

			return body;
		}

		void Run ()
		{
			ProcessBlocks ();
			RemoveRegisterVariables ();
		}

		IEnumerable<Statement> GetStatements ()
		{
			foreach (var block_statements in statements) {
				if (block_statements == null)
					continue;

				foreach (var statement in block_statements)
					yield return statement;
			}
		}

		void PopulateBodyBlock (BlockStatement block)
		{
			AddRangeToBlock (block, GetStatements ());
		}

		void AddRangeToBlock (BlockStatement block, IEnumerable<Statement> range)
		{
			foreach (var statement in range)
				block.Statements.Add (statement);
		}

		void ProcessBlocks ()
		{
			foreach (var block in cfg.Blocks)
				ProcessBlock (block);
		}

		void ProcessBlock (InstructionBlock block)
		{
			if (WasProcessed (block))
				return;

			var previous_block = current_block;
			current_block = block;

			MarkProcessed (block);

			ProcessInstructions (block);
			ProcessBlockExceptionHandlers (block);

			current_block = previous_block;
		}

		void ProcessBlockExceptionHandlers (InstructionBlock block)
		{
			// optimizable
			if (!body.HasExceptionHandlers)
				return;

			foreach (var data in cfg.GetExceptionData ())
				if (block.Index + 1 == data.TryRange.End.Index)
					ProcessExceptionData (data);
		}

		void ProcessInstructions (InstructionBlock block)
		{
			foreach (var instruction in block)
				ProcessInstruction (instruction);
		}

		void ProcessInstruction (Instruction instruction)
		{
			if (SkipExceptionObjectAction (instruction))
				return;

			CheckLabel (instruction);

			expression_decompiler.Visit (instruction);

			if (TryProcessExpression (instruction))
				return;

			if (GetStackAfter (instruction) == 0)
				CreateStatement (instruction);
		}

		bool TryProcessExpression (Instruction instruction)
		{
			Annotation annotation;
			if (!annotations.TryGetAnnotation (instruction, out annotation))
				return false;

			switch (annotation) {
			case Annotation.ConditionExpression:
				PushConditionExpression (instruction);
				return true;
			case Annotation.NullCoalescing:
				PushNullCoalescingExpression (instruction);
				return true;
			default:
				return false;
			}
		}

		void PushNullCoalescingExpression (Instruction instruction)
		{
			var expression = new NullCoalesceExpression ();
			expression.Condition = Pop ();

			ProcessExpressionBlock (current_block.Successors [1], true);
			expression.Expression = Pop ();

			Push (expression);
		}

		void ProcessExpressionBlock (InstructionBlock block)
		{
			ProcessExpressionBlock (block, false);
		}

		void ProcessExpressionBlock (InstructionBlock block, bool skip_first)
		{
			MarkProcessed (block);

			var current = current_block;
			current_block = block;

			foreach (var instruction in block) {
				if (skip_first && instruction == block.First)
					continue;

				expression_decompiler.Visit (instruction);
				TryProcessExpression (instruction);
			}

			current_block = current;
		}

		void RemoveRegisterVariables ()
		{
			foreach (var register in expression_decompiler.GetRegisters ())
				variables.Remove (register.Resolve ());
		}

		void CheckLabel (Instruction instruction)
		{
			if (!annotations.IsAnnotated (instruction, Annotation.Labeled))
				return;

			current_label = annotations.GetData<string> (instruction);
		}

		bool SkipExceptionObjectAction (Instruction instruction)
		{
			return cfg.HasExceptionObject (instruction.Offset);
		}

		void AssertStackIsEmpty ()
		{
			if (expression_decompiler.Count == 0)
				return;

			throw new InvalidOperationException ("stack should be empty after a statement");
		}

		void CreateStatement (Instruction instruction)
		{
			Visit (instruction);
		}

		public override void OnBrfalse (Instruction instruction)
		{
			AddCondition (instruction);
		}

		public override void OnBrtrue (Instruction instruction)
		{
			AddCondition (instruction);
		}

		public override void OnBle (Instruction instruction)
		{
			AddCondition (instruction);
		}

		public override void OnBlt (Instruction instruction)
		{
			AddCondition (instruction);
		}

		public override void OnBgt (Instruction instruction)
		{
			AddCondition (instruction);
		}

		public override void OnBeq (Instruction instruction)
		{
			AddCondition (instruction);
		}

		public override void OnBne_Un (Instruction instruction)
		{
			AddCondition (instruction);
		}

		public override void OnBge (Instruction instruction)
		{
			AddCondition (instruction);
		}

		void AddCondition (Instruction instruction)
		{
			Annotation annotation;
			if (!annotations.TryGetAnnotation (instruction, out annotation))
				return;

			switch (annotation) {
			case Annotation.Goto:
				AddBasicCondition (instruction, (Instruction) instruction.Operand);
				break;
			case Annotation.Condition:
				AddCompleteCondition (instruction);
				break;
			case Annotation.ConditionExpression:
				PushConditionExpression (instruction);
				break;
			case Annotation.PreTestedLoop:
				AddPreTestedLoop (instruction);
				break;
			case Annotation.PostTestedLoop:
				AddPostTestedLoop (instruction);
				break;
			default:
				throw new NotSupportedException ();
			}
		}

		void AddPreTestedLoop (Instruction instruction)
		{
			var loop = new WhileStatement (Pop (), new BlockStatement ());
			AddLoop (instruction, loop, loop.Body);
		}

		void AddPostTestedLoop (Instruction instruction)
		{
			var loop = new DoWhileStatement (Pop (), new BlockStatement ());
			AddLoop (instruction, loop, loop.Body);
		}

		void AddLoop (Instruction instruction, Statement loop, BlockStatement body)
		{
			var data = annotations.GetData<LoopData> (instruction);

			MoveStatementsToBlock (data.Body, body);

			Add (loop);
		}

		void PushConditionExpression (Instruction instruction)
		{
			var condition = new ConditionExpression ();
			condition.Condition = Pop ();

			ProcessExpressionBlock (current_block.Successors [1]);
			condition.Else = Pop ();

			ProcessExpressionBlock (current_block.Successors [0]);
			condition.Then = Pop ();

			Push (condition);
		}

		void AddBasicCondition (Instruction instruction, Instruction target)
		{
			var then = new BlockStatement ();

			var condition = new IfStatement (Pop (), then, null);

			then.Statements.Add (new GotoStatement (annotations.GetData<string> (instruction)));

			Add (condition);
		}

		void AddCompleteCondition (Instruction instruction)
		{
			Negate ();

			var condition = new IfStatement (Pop (), new BlockStatement (), null);

			ProcessConditionBranches (annotations.GetData<ConditionData> (instruction), condition);

			Add (condition);
		}

		void ProcessConditionBranches (ConditionData data, IfStatement conditional)
		{
			MoveStatementsToBlock (data.Then, conditional.Then);

			if (data.Else != null && !IsElseUnecessary (conditional)) {
				conditional.Else = new BlockStatement ();

				MoveStatementsToBlock (data.Else, conditional.Else);

				if (IsEmptyThenCondition (conditional))
					SwapConditionBranches (conditional);
			}
		}

		static bool IsElseUnecessary (IfStatement condition)
		{
			var then = condition.Then;
			if (then.Statements.Count == 0)
				return false;

			var last = then.Statements [then.Statements.Count - 1];

			return last is BreakStatement
				|| last is ContinueStatement
				|| last is ReturnStatement;
		}

		static bool IsEmptyThenCondition (IfStatement conditional)
		{
			return conditional.Then.Statements.Count == 0
				&& conditional.Else != null
				&& conditional.Else.Statements.Count > 0;
		}

		void SwapConditionBranches (IfStatement conditional)
		{
			conditional.Then = conditional.Else;
			conditional.Else = null;
			Push (conditional.Condition);
			Negate ();
			conditional.Condition = Pop ();
		}

		void Negate ()
		{
			expression_decompiler.Negate ();
		}

		void RemoveVariable (VariableReference reference)
		{
			RemoveVariable ((VariableDefinition) reference);
		}

		void RemoveVariable (VariableDefinition variable)
		{
			var index = variables.IndexOf (variable);
			if (index == -1)
				return;

			variables.RemoveAt (index);
		}

		void Push (Expression expression)
		{
			expression_decompiler.Push (expression);
		}

		void MoveStatementsToBlock (InstructionBlock start, InstructionBlock limit, BlockStatement block)
		{
			for (int i = start.Index; i < limit.Index; i++) {
				ProcessBlock (cfg.Blocks [i]);

				var block_statements = this.statements [i];
				if (block_statements == null)
					continue;

				AddRangeToBlock (block, block_statements);

				block_statements.Clear ();
			}
		}

		void MoveStatementsToBlock (BlockRange range, BlockStatement block)
		{
			MoveStatementsToBlock (range.Start, range.End, block);
		}

		/*
		public override void OnSwitch (Instruction instruction)
		{
			AddOptimizedSwitch (instruction);

			var @switch = new SwitchStatement (Pop ());

			var targets = (Instruction []) instruction.Operand;

			for (int i = 0; i < targets.Length; i++) {
				var target = targets [i];

				var body = new BlockStatement ();

				var condition = new ConditionCase (new LiteralExpression (i), body);

				body.Statements.Add (
					new GotoStatement (GetLabelName (target)));

				@switch.Cases.Add (condition);
			}

			Add (@switch);
		}

		void AddOptimizedSwitch (Instruction instruction)
		{
			RemoveSwitchTemporaryVariable ();

			var @switch = new SwitchStatement (Pop ());

			ProcessSwitchCases (instruction, @switch);

			InstructionBlock jump_to_default;
			ProcessExtendedSwitchCases (@switch, out jump_to_default);

			ProcessDefaultSwitchCase (@switch, jump_to_default);

			CleanSwitchCases (@switch);

			Add (@switch);
		}

		void ProcessSwitchCases (Instruction instruction, SwitchStatement @switch)
		{
			var successors = current_block.Successors;
			var targets = (Instruction []) instruction.Operand;

			for (int i = 0; i < targets.Length; i++) {
				var target = targets [i];

				InstructionBlock limit;

				if (i == targets.Length - 1)
					limit = successors.Last ().Successors.First ();
				else
					limit = successors [i + 1];

				var @case = CreateSwitchConditionCase (
					new LiteralExpression (i),
					successors [i],
					limit);

				@switch.Cases.Add (@case);
			}
		}

		void ProcessExtendedSwitchCases (SwitchStatement @switch, out InstructionBlock jump_to_default)
		{
			var cursor_block = current_block.Successors.Last ();
			jump_to_default = null;

			while (true) {
				MarkProcessed (cursor_block);

				if (cursor_block.Successors.Length != 2) {
					jump_to_default = cursor_block;
					return;
				}

				expression_decompiler.Visit (cursor_block.First.Next);

				var next_block = cfg.Blocks [cursor_block.Index + 1];

				var @case = CreateSwitchConditionCase (
					Pop (),
					cursor_block.Successors.First (),
					next_block.Successors.First ());

				@switch.Cases.Add (@case);

				cursor_block = next_block;
			}
		}

		ConditionCase CreateSwitchConditionCase (Expression condition, InstructionBlock start, InstructionBlock limit)
		{
			var @case = new ConditionCase (condition, new BlockStatement ());
			MoveStatementsToBlock (start, limit, @case.Body);
			return @case;
		}

		void ProcessDefaultSwitchCase (SwitchStatement @switch, InstructionBlock jump_to_default)
		{
			var successors = current_block.Successors;
			var end_block = GetFirstCommonChild (successors.First (), successors.Last ());

			var start_block = jump_to_default.Successors.First ();

			if (start_block == end_block)
				return;

			var @default = new DefaultCase (new BlockStatement ());

			MoveStatementsToBlock (start_block, end_block, @default.Body);

			@switch.Cases.Add (@default);
		}

		void CleanSwitchCases (SwitchStatement @switch)
		{
			for (int i = 0; i < @switch.Cases.Count; i++) {
				var body = @switch.Cases [i].Body;

				if (i < @switch.Cases.Count - 1)
					body.Statements.RemoveAt (body.Statements.Count - 1);

				body.Statements.Add (new BreakStatement ());
			}
		}

		void RemoveSwitchTemporaryVariable ()
		{
			if (RemoveTemporaryVariable ())
				return;

			var current = Pop ();

			var binary = current as BinaryExpression;
			if (binary == null || !binary.Right.Isa (CodeNodeType.LiteralExpression)) {
				Push (current);
				return;
			}

			Push (binary.Left);

			if (!RemoveTemporaryVariable ()) {
				Push (current);
				return;
			}

			binary.Left = Pop ();
			Push (binary);
		}*/

		public override void OnNop (Instruction instruction)
		{
		}

		public override void OnPop (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnRet (Instruction instruction)
		{
			Add (new ReturnStatement (GetStackBefore (instruction) == 1 ? Pop () : null));
		}

		public override void OnThrow (Instruction instruction)
		{
			Add (new ThrowStatement (Pop ()));
		}

		public override void OnRethrow (Instruction instruction)
		{
			Add (new ThrowStatement ());
		}

		public override void OnCall (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnCallvirt (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnNewobj (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnInitobj (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnBr (Instruction instruction)
		{
			Annotation annotation;
			if (!annotations.TryGetAnnotation (instruction, out annotation))
				return;

			switch (annotation) {
			case Annotation.Goto:
				Add (new GotoStatement (annotations.GetData<string> (instruction)));
				break;
			case Annotation.Break:
				Add (new BreakStatement ());
				break;
			case Annotation.Continue:
				Add (new ContinueStatement ());
				break;
			case Annotation.Skip:
				break;
			default:
				throw new NotSupportedException ();
			}
		}

		public override void OnLeave (Instruction instruction)
		{
		}

		public override void OnEndfinally (Instruction instruction)
		{
		}

		public override void OnEndfilter (Instruction instruction)
		{
		}

		public override void OnStloc_0 (Instruction instruction)
		{
			AddStoreLocal (instruction);
		}

		public override void OnStloc_1 (Instruction instruction)
		{
			AddStoreLocal (instruction);
		}

		public override void OnStloc_2 (Instruction instruction)
		{
			AddStoreLocal (instruction);
		}

		public override void OnStloc_3 (Instruction instruction)
		{
			AddStoreLocal (instruction);
		}

		public override void OnStloc (Instruction instruction)
		{
			AddStoreLocal (instruction);
		}

		void AddStoreLocal (Instruction instruction)
		{
			if (annotations.IsAnnotated (instruction, Annotation.Skip))
				return;

			AddExpressionStatement ();
		}

		public override void OnStarg (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_Any (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_I (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_I1 (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_I2 (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_I4 (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_I8 (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_R4 (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_R8 (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		public override void OnStelem_Ref (Instruction instruction)
		{
			AddExpressionStatement ();
		}

		void AddExpressionStatement ()
		{
			Add (new ExpressionStatement (Pop ()));
		}

		public override void OnStfld (Instruction instruction)
		{
			AddStoreField (instruction);
		}

		public override void OnStsfld (Instruction instruction)
		{
			AddStoreField (instruction);
		}

		void AddStoreField (Instruction instruction)
		{
			Add (new ExpressionStatement (Pop ()));
		}

		void ProcessExceptionData (ExceptionHandlerData data)
		{
			var block_statements = GetOrCreateStatementListAt (data.TryRange.Start.Index);

			var @try = new TryStatement ();
			@try.Try = new BlockStatement ();

			MoveStatementsToBlock (data.TryRange.Start, data.TryRange.End, @try.Try);

			ProcessCatchHandlers (data, @try);

			ProcessFinallyHandler (data, @try);

			block_statements.Add (@try);
		}

		void ProcessFinallyHandler (ExceptionHandlerData data, TryStatement @try)
		{
			if (data.FinallyRange == null)
				return;

			var range = data.FinallyRange;

			@try.Finally = new BlockStatement ();

			MoveStatementsToBlock (range.Start, range.End, @try.Finally);
		}

		void ProcessCatchHandlers (ExceptionHandlerData data, TryStatement @try)
		{
			foreach (var catch_data in data.Catches)
				@try.CatchClauses.Add (CreateCatchHandler (catch_data));
		}

		CatchClause CreateCatchHandler (CatchHandlerData catch_data)
		{
			var range = catch_data.Range;

			var variable = GetCatchVariable (range.Start.First);

			RemoveVariable (variable);

			var clause = new CatchClause (
				new BlockStatement (),
				catch_data.Type,
				new VariableDeclarationExpression (variable));

			MoveStatementsToBlock (range.Start, range.End, clause.Body);

			return clause;
		}

		VariableDefinition GetCatchVariable (Instruction instruction)
		{
			switch (instruction.OpCode.Code) {
			case Code.Pop:
				return null;
			case Code.Stloc:
			case Code.Stloc_S:
				return (VariableDefinition) instruction.Operand;
			case Code.Stloc_0:
				return body.Variables [0];
			case Code.Stloc_1:
				return body.Variables [1];
			case Code.Stloc_2:
				return body.Variables [2];
			case Code.Stloc_3:
				return body.Variables [3];
			default:
				throw new NotSupportedException (instruction.OpCode.Name);
			}
		}

		int GetStackBefore (Instruction instruction)
		{
			return GetInstructionData (instruction).StackBefore;
		}

		int GetStackAfter (Instruction instruction)
		{
			return GetInstructionData (instruction).StackAfter;
		}

		InstructionData GetInstructionData (Instruction instruction)
		{
			return cfg.GetData (instruction);
		}

		void Add (Statement statement)
		{
			if (statement == null)
				throw new ArgumentNullException ("statement");

			var block_statements = GetOrCreateStatementListAt (current_block.Index);

			if (current_label != null) {
				var label = new LabeledStatement (current_label);
				current_label = null;
				block_statements.Add (label);
			}

			block_statements.Add (statement);
		}

		List<Statement> GetOrCreateStatementListAt (int index)
		{
			var block_statements = statements [index];
			if (block_statements != null)
				return block_statements;

			block_statements = new List<Statement> (4);
			statements [index] = block_statements;
			return block_statements;
		}

		Expression Pop ()
		{
			return expression_decompiler.Pop ();
		}

		void MarkProcessed (InstructionBlock [] blocks)
		{
			foreach (var block in blocks)
				MarkProcessed (block);
		}

		void MarkProcessed (InstructionBlock block)
		{
			processed.Add (block);
		}

		bool WasProcessed (InstructionBlock block)
		{
			return processed.Contains (block);
		}
	}
}
