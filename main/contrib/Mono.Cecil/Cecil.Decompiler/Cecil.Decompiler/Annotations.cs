#region license
//
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

using Cecil.Decompiler;
using Cecil.Decompiler.Cil;

namespace Cecil.Decompiler {

	[Flags]
	enum Annotation {
		None = 0,
		Goto = 1,
		Labeled = 2,
		Condition = 4,
		ConditionExpression = 8,
		NullCoalescing = 16,
		PreTestedLoop = 32,
		PostTestedLoop = 64,
		Break = 128,
		Continue = 256,
		Switch = 512,
		Skip = 1024,
		Register = 2048,
	}

	static class AnnotationExtensions {

		public static bool Is (this Annotation self, Annotation annotation)
		{
			return (self & annotation) == annotation;
		}

		public static Annotation Add (this Annotation self, Annotation annotation)
		{
			return self | annotation;
		}
	}

	class ConditionData {

		public readonly BlockRange Then;
		public BlockRange Else;

		public ConditionData (InstructionBlock then_start, InstructionBlock then_end)
		{
			Then = new BlockRange (then_start, then_end);
		}

		public void AddElseData (InstructionBlock else_start, InstructionBlock else_end)
		{
			Else = new BlockRange (else_start, else_end);
		}
	}

	class LoopData {

		public readonly BlockRange Body;

		public LoopData (InstructionBlock body_start, InstructionBlock body_end)
		{
			Body = new BlockRange (body_start, body_end);
		}
	}

	class AnnotationStore {

		Dictionary<int, Annotation> annotations = new Dictionary<int, Annotation> ();
		Dictionary<int, object> data = new Dictionary<int, object> ();

		public void Annotate (Instruction instruction, Annotation annotation)
		{
			Annotation current;
			if (!TryGetAnnotation (instruction, out current)) {
				annotations.Add (instruction.Offset, annotation);
				return;
			}

			current = current.Add (annotation);
			annotations [instruction.Offset] = current;
		}

		public void Annotate (Instruction instruction, Annotation annotation, object data)
		{
			Annotate (instruction, annotation);
			this.data.Add (instruction.Offset, data);
		}

		public bool HasAnnotation (Instruction instruction)
		{
			return annotations.ContainsKey (instruction.Offset);
		}

		public bool TryGetAnnotation (Instruction instruction, out Annotation annotation)
		{
			return annotations.TryGetValue (instruction.Offset, out annotation);
		}

		public void RemoveAnnotation (Instruction instruction, Annotation annotation)
		{
			if (IsAnnotated (instruction, annotation)) {
				annotations.Remove (instruction.Offset);
			}
		}

		public bool IsAnnotated (Instruction instruction, Annotation annotation)
		{
			Annotation current;
			if (!TryGetAnnotation (instruction, out current))
				return false;

			return current.Is (annotation);
		}

		public TData GetData<TData> (Instruction instruction)
		{
			object data;
			this.data.TryGetValue (instruction.Offset, out data);
			return (TData) data;
		}

		public bool TryGetData<TData> (Instruction instruction, out TData data)
		{
			object current;
			if (!this.data.TryGetValue (instruction.Offset, out current)) {
				data = default (TData);
				return false;
			}

			data = (TData) current;
			return true;
		}

		public static AnnotationStore CreateStore (ControlFlowGraph cfg, BlockOptimization optimization)
		{
			AnnotationStoreBuilder builder;

			switch (optimization) {
			case BlockOptimization.Basic:
				builder = new BasicAnnotationBuilder (cfg);
				break;
			case BlockOptimization.Detailed:
				builder = new DetailedAnnotationBuilder (cfg);
				break;
			default:
				throw new ArgumentException ();
			}

			return builder.BuildStore ();
		}
	}

	abstract class AnnotationStoreBuilder {

		protected ControlFlowGraph cfg;
		protected AnnotationStore store;

		HashSet<InstructionBlock> processed = new HashSet<InstructionBlock> ();

		protected AnnotationStoreBuilder (ControlFlowGraph cfg)
		{
			this.cfg = cfg;
			this.store = new AnnotationStore ();
		}

		public void Annotate (Instruction instruction, Annotation annotation)
		{
			store.Annotate (instruction, annotation);
		}

		public void Annotate (Instruction instruction, Annotation annotation, object data)
		{
			store.Annotate (instruction, annotation, data);
		}

		public AnnotationStore BuildStore ()
		{
			Process (cfg.Blocks [0]);
			ProcessAll ();
			return store;
		}

		protected void Process (InstructionBlock block)
		{
			if (processed.Contains (block))
				return;

			processed.Add (block);

			ProcessBlock (block);
		}

		void ProcessAll ()
		{
			foreach (var block in cfg.Blocks)
				Process (block);
		}

		protected virtual void ProcessBlock (InstructionBlock block)
		{
			var length = block.Successors.Length;

			if (length == 0)
				return;

			switch (length) {
			case 1:
				ProcessOneWayBlock (block);
				break;
			case 2:
				ProcessTwoWayBlock (block);
				break;
			default:
				ProcessMultipleWayBlock (block);
				break;
			}
		}

		protected static IEnumerable<InstructionBlock> GetChildTree (InstructionBlock block)
		{
			yield return block;

			var successors = block.Successors;

			if (successors.Length == 0)
				yield break;

			for (int i = 0; i < successors.Length; i++) {
				if (successors [i].Index > block.Index)
					foreach (var nested in GetChildTree (successors [i]))
						yield return nested;
			}
		}

		protected static InstructionBlock GetFirstCommonChild (InstructionBlock a, InstructionBlock b)
		{
			var list = new List<InstructionBlock> (GetChildTree (a));
			foreach (var child in GetChildTree (b))
				if (list.Contains (child))
					return child;

			return null;
		}

		protected abstract void ProcessOneWayBlock (InstructionBlock block);
		protected abstract void ProcessTwoWayBlock (InstructionBlock block);
		protected abstract void ProcessMultipleWayBlock (InstructionBlock block);
	}

	class BasicAnnotationBuilder : AnnotationStoreBuilder {

		public BasicAnnotationBuilder (ControlFlowGraph cfg)
			: base (cfg)
		{
		}

		protected override void ProcessBlock (InstructionBlock block)
		{
			ProcessRegisters (block);

			base.ProcessBlock (block);
		}

		void ProcessRegisters (InstructionBlock block)
		{
			foreach (var instruction in block) {
				ProcessAccessRegister (instruction);
			}
		}

		void ProcessAccessRegister (Instruction instruction)
		{
			int store_register, load_register;

			if (!IsStoreRegister (instruction, out store_register))
				return;

			var next = instruction.Next;

			if (IsRegisterBranchCheck (next)) {
				Annotate (next, Annotation.Skip);
				next = next.Next;
			}

			var load = next;

			if (!IsLoadRegister (load, out load_register))
				return;

			if (store_register != load_register)
				return;

			if (!PostLoadFlowControlIsBranch (load))
				return;

			Annotate (instruction, Annotation.Skip);
			Annotate (next, Annotation.Register);
		}

		static bool IsRegisterBranchCheck (Instruction instruction)
		{
			return IsGotoNextInstruction (instruction);
		}

		static bool PostLoadFlowControlIsBranch (Instruction load)
		{
			switch (load.Next.OpCode.FlowControl) {
			case FlowControl.Branch:
			case FlowControl.Break:
			case FlowControl.Cond_Branch:
			case FlowControl.Return:
			case FlowControl.Throw:
				return true;
			}

			return false;
		}

		static bool IsStoreRegister (Instruction instruction, out int register)
		{
			switch (instruction.OpCode.Code) {
			case Code.Stloc:
			case Code.Stloc_S:
				register = ((VariableReference) instruction.Operand).Index;
				return true;
			case Code.Stloc_0:
				register = 0;
				return true;
			case Code.Stloc_1:
				register = 1;
				return true;
			case Code.Stloc_2:
				register = 2;
				return true;
			case Code.Stloc_3:
				register = 3;
				return true;
			default:
				register = -1;
				return false;
			}
		}

		static bool IsLoadRegister (Instruction instruction, out int register)
		{
			switch (instruction.OpCode.Code) {
			case Code.Ldloc:
			case Code.Ldloc_S:
			case Code.Ldloca:
			case Code.Ldloca_S:
				register = ((VariableReference) instruction.Operand).Index;
				return true;
			case Code.Ldloc_0:
				register = 0;
				return true;
			case Code.Ldloc_1:
				register = 1;
				return true;
			case Code.Ldloc_2:
				register = 2;
				return true;
			case Code.Ldloc_3:
				register = 3;
				return true;
			default:
				register = -1;
				return false;
			}
		}

		protected override void ProcessOneWayBlock (InstructionBlock block)
		{
			var last = block.Last;

			if (IsSimpleGoto (last))
				Annotate (last, Annotation.Goto, Labelize ((Instruction) last.Operand));

			ProcessOneWayBlockSuccessor (block);
		}

		protected void ProcessOneWayBlockSuccessor (InstructionBlock block)
		{
			Process (block.Successors [0]);
		}

		protected override void ProcessTwoWayBlock (InstructionBlock block)
		{
			if (TryProcessConditionExpression (block))
				return;

			var last = block.Last;

			var target = (Instruction) last.Operand;

			Annotate (last, Annotation.Goto, Labelize (target));

			ProcessTwoWayBlockSuccessors (block);
		}

		protected string Labelize (Instruction instruction)
		{
			if (store.IsAnnotated (instruction, Annotation.Labeled))
				return store.GetData<string> (instruction);

			string name = CreateLabelName (instruction);
			Annotate (instruction, Annotation.Labeled, name);
			return name;
		}

		protected void ProcessTwoWayBlockSuccessors (InstructionBlock block)
		{
			Process (block.Successors [1]);
			Process (block.Successors [0]);
		}

		protected override void ProcessMultipleWayBlock (InstructionBlock block)
		{
			var last = block.Last;
			var targets = (Instruction []) last.Operand;
			Annotate (last, Annotation.Switch);

			foreach (var target in targets)
				Annotate (target, Annotation.Labeled);

			ProcessMultipleWayBlockSuccessors (block);
		}

		protected void ProcessMultipleWayBlockSuccessors (InstructionBlock block)
		{
			foreach (var successor in block.Successors)
				Process (successor);
		}

		protected bool TryProcessConditionExpression (InstructionBlock block)
		{
			if (!IsConditionExpression (block))
				return false;

			var last = block.Last;

			if (IsNullCoalescing (block))
				Annotate (last, Annotation.NullCoalescing);
			else
				Annotate (last, Annotation.ConditionExpression);

			ProcessTwoWayBlockSuccessors (block);
			return true;
		}

		protected static bool IsNullCoalescing (InstructionBlock block)
		{
			var last = block.Last;

			if (!IsConditionalBranch (last))
				return false;

			if (!IsOpCode (last, Code.Brtrue) && !IsOpCode (last, Code.Brtrue_S))
				return false;

			if (!IsOpCode (last.Previous, Code.Dup))
				return false;

			return IsOpCode (block.Successors [1].First, Code.Pop);
		}

		protected bool IsConditionExpression (InstructionBlock block)
		{
			var last = block.Last;

			if (!IsConditionalBranch (last))
				return false;

			var child = GetFirstCommonChild (block.Successors [0], block.Successors [1]);
			if (child == null)
				return false;

			return GetStackBefore (child.First) > 0;
		}

		protected static bool IsOpCode (Instruction instruction, Code code)
		{
			return instruction.OpCode.Code == code;
		}

		protected static bool IsBranch (Instruction instruction)
		{
			return instruction.OpCode.FlowControl == FlowControl.Branch;
		}

		protected bool IsSimpleGoto (Instruction instruction)
		{
			return IsGotoNextInstruction (instruction)
				&& GetStackAfter (instruction) == 0
				&& !IsSkipped (instruction);
		}

		protected static bool IsGoto (Instruction instruction)
		{
			return IsOpCode (instruction, Code.Br) || IsOpCode (instruction, Code.Br_S);
		}

		protected static bool IsGotoNextInstruction (Instruction instruction)
		{
			return IsGoto (instruction)
				&& instruction.Next == instruction.Operand;
		}

		protected static bool IsConditionalBranch (Instruction instruction)
		{
			return instruction.OpCode.FlowControl == FlowControl.Cond_Branch;
		}

		protected bool IsSkipped (Instruction instruction)
		{
			return store.IsAnnotated (instruction, Annotation.Skip);
		}

		protected int GetStackBefore (Instruction instruction)
		{
			return cfg.GetData (instruction).StackBefore;
		}

		protected int GetStackAfter (Instruction instruction)
		{
			return cfg.GetData (instruction).StackAfter;
		}

		string CreateLabelName (Instruction instruction)
		{
			return "L_" + instruction.Offset.ToString ("x4");
		}
	}

	class DetailedAnnotationBuilder : BasicAnnotationBuilder {

		List<LoopData> loop_stack = new List<LoopData> ();

		public DetailedAnnotationBuilder (ControlFlowGraph cfg)
			: base (cfg)
		{
		}

		void PushLoopData (LoopData data)
		{
			loop_stack.Add (data);
		}

		LoopData PeekLoopData ()
		{
			if (loop_stack.Count == 0)
				return null;

			return loop_stack [loop_stack.Count - 1];
		}

		void PopLoopData ()
		{
			if (loop_stack.Count == 0)
				throw new InvalidOperationException ();

			loop_stack.RemoveAt (loop_stack.Count - 1);
		}

		protected override void ProcessBlock (InstructionBlock block)
		{
			var data = PeekLoopData ();
			if (data != null && data.Body.End == block)
				PopLoopData ();

			base.ProcessBlock (block);
		}

		protected override void ProcessOneWayBlock (InstructionBlock block)
		{
			if (TryProcessLoopBranching (block))
				return;

			base.ProcessOneWayBlock (block);
		}

		bool TryProcessLoopBranching (InstructionBlock block)
		{
			if (!IsGoto (block))
				return false;

			if (TryProcessContinue (block))
				return true;

			if (TryProcessBreak (block))
				return true;

			if (IsGotoLoopTest (block))
				return true;

			return false;
		}

		bool TryProcessContinue (InstructionBlock block)
		{
			var data = PeekLoopData ();
			if (data == null)
				return false;

			if (block.Successors [0].Index + 1 == data.Body.End.Index - 1) {
				store.RemoveAnnotation (block.Last, Annotation.Skip);
				Annotate (block.Last, Annotation.Continue);
				return true;
			}

			return false;
		}

		bool TryProcessBreak (InstructionBlock block)
		{
			var data = PeekLoopData ();
			if (data == null)
				return false;

			if (block.Successors [0].Index == data.Body.End.Index) {
				store.RemoveAnnotation (block.Last, Annotation.Skip);
				Annotate (block.Last, Annotation.Break);
				return true;
			}

			return false;
		}

		static bool IsGoto (InstructionBlock block)
		{
			return block.Successors.Length == 1
				&& IsGoto (block.Last);
		}

		bool IsGotoLoopTest (InstructionBlock block)
		{
			Process (block.Successors [0]);

			var loop = ParseExpression (block.Successors [0]);

			if (store.IsAnnotated (loop.Last, Annotation.PreTestedLoop))
				return true;

			return false;
		}
		
		InstructionBlock ParseExpression (InstructionBlock block)
		{
			return block;
		}

		protected override void ProcessTwoWayBlock (InstructionBlock block)
		{
			if (TryProcessConditionExpression (block))
				return;

			var last = block.Last;
			var target = (Instruction) last.Operand;

			if (IsLoopBranch (last, target))
				ProcessLoop (block);
			else if (IsConditionBranch (last, target))
				ProcessCondition (block);
			else
				base.ProcessTwoWayBlock (block);
		}

		protected override void ProcessMultipleWayBlock (InstructionBlock block)
		{
			base.ProcessMultipleWayBlock (block);
		}

		void ProcessLoop (InstructionBlock block)
		{
			var start = block.Successors [0];
			var before = cfg.Blocks [start.Index - 1];

			var annotation = IsPreTestedLoop (before, block) ?
				Annotation.PreTestedLoop : Annotation.PostTestedLoop;

			var data = new LoopData (start, cfg.Blocks [block.Index + 1]);

			Annotate (block.Last, annotation, data);

			PushLoopData (data);

			Process (block.Successors [0]);
			Process (block.Successors [1]);
		}

		static bool IsPreTestedLoop (InstructionBlock before, InstructionBlock block)
		{
			return before.Successors [0] == block
				&& before.Last.OpCode.FlowControl == FlowControl.Branch;
		}

		void ProcessCondition (InstructionBlock block)
		{
			var @else = block.Successors [0];
			var then = block.Successors [1];

			var child = GetFirstCommonChild (then, @else);
			if (child == null)
				child = @else;

			var data = new ConditionData (then, @else);

			if (child != @else) {
				var then_end = GetBlock (@else.Index - 1);
				var last = then_end.Last;

				if (IsGoto (last) && then_end.Successors [0] == child)
					Annotate (last, Annotation.Skip);

				data.AddElseData (@else, child);
			}

			Annotate (block.Last, Annotation.Condition, data);

			ProcessTwoWayBlockSuccessors (block);
		}

		InstructionBlock GetBlock (int index)
		{
			return cfg.Blocks [index];
		}

		static bool IsLoopBranch (Instruction instruction, Instruction target)
		{
			return instruction.Offset > target.Offset;
		}

		static bool IsConditionBranch (Instruction instruction, Instruction target)
		{
			return instruction.Offset < target.Offset;
		}
	}
}
