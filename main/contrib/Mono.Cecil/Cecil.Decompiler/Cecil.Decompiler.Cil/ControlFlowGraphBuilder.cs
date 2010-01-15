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

namespace Cecil.Decompiler.Cil {

	class ControlFlowGraphBuilder {

		MethodBody body;
		Dictionary<int, InstructionData> data;
		Dictionary<int, InstructionBlock> blocks = new Dictionary<int, InstructionBlock> ();
		List<ExceptionHandlerData> exception_data;
		HashSet<int> exception_objects_offsets;

		internal ControlFlowGraphBuilder (MethodDefinition method)
		{
			body = method.Body;

			if (body.ExceptionHandlers.Count > 0)
				exception_objects_offsets = new HashSet<int> ();
		}

		public ControlFlowGraph CreateGraph ()
		{
			DelimitBlocks ();
			ConnectBlocks ();
			ComputeInstructionData ();
			ComputeExceptionHandlerData ();

			return new ControlFlowGraph (body, ToArray (), data, exception_data, exception_objects_offsets);
		}

		void DelimitBlocks ()
		{
			var instructions = body.Instructions;
			MarkBlockStarts (instructions);

			var exceptions = body.ExceptionHandlers;
			MarkBlockStarts (exceptions);

			MarkBlockEnds (instructions);
		}

		void MarkBlockStarts (ExceptionHandlerCollection handlers)
		{
			for (int i = 0; i < handlers.Count; i++) {
				var handler = handlers [i];
				MarkBlockStart (handler.TryStart);
				MarkBlockStart (handler.HandlerStart);

				if (handler.Type == ExceptionHandlerType.Filter) {
					MarkExceptionObjectPosition (handler.FilterStart);
					MarkBlockStart (handler.FilterStart);
				} else if (handler.Type == ExceptionHandlerType.Catch)
					MarkExceptionObjectPosition (handler.HandlerStart);
			}
		}

		void MarkExceptionObjectPosition (Instruction instruction)
		{
			exception_objects_offsets.Add (instruction.Offset);
		}

		void MarkBlockStarts (InstructionCollection instructions)
		{
			// the first instruction starts a block
			for (int i = 0; i < instructions.Count; ++i) {
				var instruction = instructions [i];

				if (i == 0)
					MarkBlockStart (instruction);

				if (!IsBlockDelimiter (instruction))
					continue;

				if (HasMultipleBranches (instruction)) {
					// each switch case first instruction starts a block
					foreach (var target in GetBranchTargets (instruction))
						if (target != null)
							MarkBlockStart (target);
				} else {
					// the target of a branch starts a block
					var target = GetBranchTarget (instruction);
					if (target != null)
						MarkBlockStart (target);
				}

				// the next instruction after a branch starts a block
				if (instruction.Next != null)
					MarkBlockStart (instruction.Next);
			}
		}

		void MarkBlockEnds (InstructionCollection instructions)
		{
			var blocks = ToArray ();
			var current = blocks [0];

			for (int i = 1; i < blocks.Length; ++i) {
				var block = blocks [i];
				current.Last = block.First.Previous;
				current = block;
			}

			current.Last = instructions [instructions.Count - 1];
		}

		static bool IsBlockDelimiter (Instruction instruction)
		{
			switch (instruction.OpCode.FlowControl) {
			case FlowControl.Break:
			case FlowControl.Branch:
			case FlowControl.Return:
			case FlowControl.Cond_Branch:
				return true;
			}
			return false;
		}

		void MarkBlockStart (Instruction instruction)
		{
			var block = GetBlock (instruction);
			if (block != null)
				return;

			block = new InstructionBlock (instruction);
			RegisterBlock (block);
		}

		void ComputeInstructionData ()
		{
			data = new Dictionary<int, InstructionData> ();

			var visited = new HashSet<InstructionBlock> ();

			foreach (var block in this.blocks.Values)
				ComputeInstructionData (visited, 0, block);
		}

		void ComputeInstructionData (HashSet<InstructionBlock> visited, int stackHeight, InstructionBlock block)
		{
			if (visited.Contains (block))
				return;

			visited.Add (block);

			foreach (var instruction in block)
				stackHeight = ComputeInstructionData (stackHeight, instruction);

			foreach (var successor in block.Successors)
				ComputeInstructionData (visited, stackHeight, successor);
		}

		bool IsCatchStart (Instruction instruction)
		{
			if (exception_objects_offsets == null)
				return false;

			return exception_objects_offsets.Contains (instruction.Offset);
		}

		int ComputeInstructionData (int stackHeight, Instruction instruction)
		{
			if (IsCatchStart (instruction))
				stackHeight++;

			int before = stackHeight;
			int after = ComputeNewStackHeight (stackHeight, instruction);
			data.Add (instruction.Offset, new InstructionData (before, after));
			return after;
		}

		int ComputeNewStackHeight (int stackHeight, Instruction instruction)
		{
			return stackHeight + GetPushDelta (instruction) - GetPopDelta (stackHeight, instruction);
		}

		static int GetPushDelta (Instruction instruction)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPush) {
			case StackBehaviour.Push0:
				return 0;

			case StackBehaviour.Push1:
			case StackBehaviour.Pushi:
			case StackBehaviour.Pushi8:
			case StackBehaviour.Pushr4:
			case StackBehaviour.Pushr8:
			case StackBehaviour.Pushref:
				return 1;

			case StackBehaviour.Push1_push1:
				return 2;

			case StackBehaviour.Varpush:
				if (code.FlowControl == FlowControl.Call) {
					var method = (IMethodSignature) instruction.Operand;
					return IsVoid (method.ReturnType.ReturnType) ? 0 : 1;
				}

				break;
			}
			throw new ArgumentException (Formatter.FormatInstruction (instruction));
		}

		int GetPopDelta (int stackHeight, Instruction instruction)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPop) {
			case StackBehaviour.Pop0:
				return 0;
			case StackBehaviour.Popi:
			case StackBehaviour.Popref:
			case StackBehaviour.Pop1:
				return 1;

			case StackBehaviour.Pop1_pop1:
			case StackBehaviour.Popi_pop1:
			case StackBehaviour.Popi_popi:
			case StackBehaviour.Popi_popi8:
			case StackBehaviour.Popi_popr4:
			case StackBehaviour.Popi_popr8:
			case StackBehaviour.Popref_pop1:
			case StackBehaviour.Popref_popi:
				return 2;

			case StackBehaviour.Popi_popi_popi:
			case StackBehaviour.Popref_popi_popi:
			case StackBehaviour.Popref_popi_popi8:
			case StackBehaviour.Popref_popi_popr4:
			case StackBehaviour.Popref_popi_popr8:
			case StackBehaviour.Popref_popi_popref:
				return 3;

			case StackBehaviour.PopAll:
				return stackHeight;

			case StackBehaviour.Varpop:
				if (code.FlowControl == FlowControl.Call) {
					var method = (IMethodSignature) instruction.Operand;
					int count = method.Parameters.Count;
					if (method.HasThis && OpCodes.Newobj.Value != code.Value)
						++count;

					return count;
				}

				if (code.Code == Code.Ret)
					return IsVoidMethod () ? 0 : 1;

				break;
			}
			throw new ArgumentException (Formatter.FormatInstruction (instruction));
		}

		bool IsVoidMethod ()
		{
			return IsVoid (body.Method.ReturnType.ReturnType);
		}

		static bool IsVoid (TypeReference type)
		{
			return type.FullName == Constants.Void;
		}

		InstructionBlock [] ToArray ()
		{
			var result = new InstructionBlock [blocks.Count];
			blocks.Values.CopyTo (result, 0);
			Array.Sort (result);
			ComputeIndexes (result);
			return result;
		}

		static void ComputeIndexes (InstructionBlock [] blocks)
		{
			for (int i = 0; i < blocks.Length; i++)
				blocks [i].Index = i;
		}

		void ConnectBlocks ()
		{
			foreach (InstructionBlock block in blocks.Values)
				ConnectBlock (block);
		}

		void ConnectBlock (InstructionBlock block)
		{
			if (block.Last == null)
				throw new ArgumentException ("Undelimited block at offset " + block.First.Offset);

			var instruction = block.Last;
			switch (instruction.OpCode.FlowControl) {
			case FlowControl.Branch:
			case FlowControl.Cond_Branch: {
				if (HasMultipleBranches (instruction)) {
					var blocks = GetBranchTargetsBlocks (instruction);
					if (instruction.Next != null)
						blocks = AddBlock (GetBlock (instruction.Next), blocks);

					block.Successors = blocks;
					break;
				}

				var target = GetBranchTargetBlock (instruction);
				if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch && instruction.Next != null)
					block.Successors = new [] { target, GetBlock (instruction.Next) };
				else
					block.Successors = new [] { target };
				break;
			}
			case FlowControl.Call:
			case FlowControl.Next:
				if (null != instruction.Next)
					block.Successors = new [] { GetBlock (instruction.Next) };

				break;
			case FlowControl.Return:
			case FlowControl.Throw:
				break;
			default:
				throw new NotSupportedException (
					string.Format ("Unhandled instruction flow behavior {0}: {1}",
					               instruction.OpCode.FlowControl,
					               Formatter.FormatInstruction (instruction)));
			}
		}

		static InstructionBlock [] AddBlock (InstructionBlock block, InstructionBlock [] blocks)
		{
			var result = new InstructionBlock [blocks.Length + 1];
			Array.Copy (blocks, result, blocks.Length);
			result [result.Length - 1] = block;

			return result;
		}

		static bool HasMultipleBranches (Instruction instruction)
		{
			return instruction.OpCode.Code == Code.Switch;
		}

		InstructionBlock [] GetBranchTargetsBlocks (Instruction instruction)
		{
			var targets = GetBranchTargets (instruction);
			var blocks = new InstructionBlock [targets.Length];
			for (int i = 0; i < targets.Length; i++)
				blocks [i] = GetBlock (targets [i]);

			return blocks;
		}

		static Instruction [] GetBranchTargets (Instruction instruction)
		{
			return (Instruction []) instruction.Operand;
		}

		InstructionBlock GetBranchTargetBlock (Instruction instruction)
		{
			return GetBlock (GetBranchTarget (instruction));
		}

		static Instruction GetBranchTarget (Instruction instruction)
		{
			return (Instruction) instruction.Operand;
		}

		void RegisterBlock (InstructionBlock block)
		{
			blocks.Add (block.First.Offset, block);
		}

		InstructionBlock GetBlock (Instruction firstInstruction)
		{
			InstructionBlock block;
			blocks.TryGetValue (firstInstruction.Offset, out block);
			return block;
		}

		void ComputeExceptionHandlerData ()
		{
			var handlers = body.ExceptionHandlers;
			if (handlers.Count == 0)
				return;

			var datas = new Dictionary<int, ExceptionHandlerData> ();

			foreach (ExceptionHandler handler in handlers)
				ComputeExceptionHandlerData (datas, handler);

			exception_data = new List<ExceptionHandlerData> (datas.Values);
			exception_data.Sort ();
		}

		void ComputeExceptionHandlerData (Dictionary<int, ExceptionHandlerData> datas, ExceptionHandler handler)
		{
			ExceptionHandlerData data;
			if (!datas.TryGetValue (handler.TryStart.Offset, out data)) {
				data = new ExceptionHandlerData (ComputeRange (handler.TryStart, handler.TryEnd));
				datas.Add (handler.TryStart.Offset, data);
			}

			ComputeExceptionHandlerData (data, handler);
		}

		void ComputeExceptionHandlerData (ExceptionHandlerData data, ExceptionHandler handler)
		{
			var range = ComputeRange (handler.HandlerStart, handler.HandlerEnd);

			switch (handler.Type) {
			case ExceptionHandlerType.Catch:
				data.Catches.Add (new CatchHandlerData (handler.CatchType, range));
				break;
			case ExceptionHandlerType.Fault:
				data.FaultRange = range;
				break;
			case ExceptionHandlerType.Finally:
				data.FinallyRange = range;
				break;
			case ExceptionHandlerType.Filter:
				throw new NotImplementedException ();
			}
		}

		BlockRange ComputeRange (Instruction start, Instruction end)
		{
			return new BlockRange (blocks [start.Offset], blocks [end.Offset]);
		}
	}
}
