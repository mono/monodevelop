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

// Warning: generated do not edit

using System;
using System.Collections.Generic;

using Mono.Cecil.Cil;

namespace Cecil.Decompiler.Cil {

	class InstructionDispatcher {

		public static void Dispatch (Instruction instruction, IInstructionVisitor visitor)
		{
			switch (instruction.OpCode.Code) {
			case Code.Nop:
				visitor.OnNop (instruction);
				return;
			case Code.Break:
				visitor.OnBreak (instruction);
				return;
			case Code.Ldarg_0:
				visitor.OnLdarg_0 (instruction);
				return;
			case Code.Ldarg_1:
				visitor.OnLdarg_1 (instruction);
				return;
			case Code.Ldarg_2:
				visitor.OnLdarg_2 (instruction);
				return;
			case Code.Ldarg_3:
				visitor.OnLdarg_3 (instruction);
				return;
			case Code.Ldloc_0:
				visitor.OnLdloc_0 (instruction);
				return;
			case Code.Ldloc_1:
				visitor.OnLdloc_1 (instruction);
				return;
			case Code.Ldloc_2:
				visitor.OnLdloc_2 (instruction);
				return;
			case Code.Ldloc_3:
				visitor.OnLdloc_3 (instruction);
				return;
			case Code.Stloc_0:
				visitor.OnStloc_0 (instruction);
				return;
			case Code.Stloc_1:
				visitor.OnStloc_1 (instruction);
				return;
			case Code.Stloc_2:
				visitor.OnStloc_2 (instruction);
				return;
			case Code.Stloc_3:
				visitor.OnStloc_3 (instruction);
				return;
			case Code.Ldarg:
			case Code.Ldarg_S:
				visitor.OnLdarg (instruction);
				return;
			case Code.Ldarga:
			case Code.Ldarga_S:
				visitor.OnLdarga (instruction);
				return;
			case Code.Starg:
			case Code.Starg_S:
				visitor.OnStarg (instruction);
				return;
			case Code.Ldloc:
			case Code.Ldloc_S:
				visitor.OnLdloc (instruction);
				return;
			case Code.Ldloca:
			case Code.Ldloca_S:
				visitor.OnLdloca (instruction);
				return;
			case Code.Stloc:
			case Code.Stloc_S:
				visitor.OnStloc (instruction);
				return;
			case Code.Ldnull:
				visitor.OnLdnull (instruction);
				return;
			case Code.Ldc_I4_M1:
				visitor.OnLdc_I4_M1 (instruction);
				return;
			case Code.Ldc_I4_0:
				visitor.OnLdc_I4_0 (instruction);
				return;
			case Code.Ldc_I4_1:
				visitor.OnLdc_I4_1 (instruction);
				return;
			case Code.Ldc_I4_2:
				visitor.OnLdc_I4_2 (instruction);
				return;
			case Code.Ldc_I4_3:
				visitor.OnLdc_I4_3 (instruction);
				return;
			case Code.Ldc_I4_4:
				visitor.OnLdc_I4_4 (instruction);
				return;
			case Code.Ldc_I4_5:
				visitor.OnLdc_I4_5 (instruction);
				return;
			case Code.Ldc_I4_6:
				visitor.OnLdc_I4_6 (instruction);
				return;
			case Code.Ldc_I4_7:
				visitor.OnLdc_I4_7 (instruction);
				return;
			case Code.Ldc_I4_8:
				visitor.OnLdc_I4_8 (instruction);
				return;
			case Code.Ldc_I4:
			case Code.Ldc_I4_S:
				visitor.OnLdc_I4 (instruction);
				return;
			case Code.Ldc_I8:
				visitor.OnLdc_I8 (instruction);
				return;
			case Code.Ldc_R4:
				visitor.OnLdc_R4 (instruction);
				return;
			case Code.Ldc_R8:
				visitor.OnLdc_R8 (instruction);
				return;
			case Code.Dup:
				visitor.OnDup (instruction);
				return;
			case Code.Pop:
				visitor.OnPop (instruction);
				return;
			case Code.Jmp:
				visitor.OnJmp (instruction);
				return;
			case Code.Call:
				visitor.OnCall (instruction);
				return;
			case Code.Calli:
				visitor.OnCalli (instruction);
				return;
			case Code.Ret:
				visitor.OnRet (instruction);
				return;
			case Code.Br:
			case Code.Br_S:
				visitor.OnBr (instruction);
				return;
			case Code.Brfalse:
			case Code.Brfalse_S:
				visitor.OnBrfalse (instruction);
				return;
			case Code.Brtrue:
			case Code.Brtrue_S:
				visitor.OnBrtrue (instruction);
				return;
			case Code.Beq:
			case Code.Beq_S:
				visitor.OnBeq (instruction);
				return;
			case Code.Bge:
			case Code.Bge_S:
				visitor.OnBge (instruction);
				return;
			case Code.Bgt:
			case Code.Bgt_S:
				visitor.OnBgt (instruction);
				return;
			case Code.Ble:
			case Code.Ble_S:
				visitor.OnBle (instruction);
				return;
			case Code.Blt:
			case Code.Blt_S:
				visitor.OnBlt (instruction);
				return;
			case Code.Bne_Un:
			case Code.Bne_Un_S:
				visitor.OnBne_Un (instruction);
				return;
			case Code.Bge_Un:
			case Code.Bge_Un_S:
				visitor.OnBge_Un (instruction);
				return;
			case Code.Bgt_Un:
			case Code.Bgt_Un_S:
				visitor.OnBgt_Un (instruction);
				return;
			case Code.Ble_Un:
			case Code.Ble_Un_S:
				visitor.OnBle_Un (instruction);
				return;
			case Code.Blt_Un:
			case Code.Blt_Un_S:
				visitor.OnBlt_Un (instruction);
				return;
			case Code.Switch:
				visitor.OnSwitch (instruction);
				return;
			case Code.Ldind_I1:
				visitor.OnLdind_I1 (instruction);
				return;
			case Code.Ldind_U1:
				visitor.OnLdind_U1 (instruction);
				return;
			case Code.Ldind_I2:
				visitor.OnLdind_I2 (instruction);
				return;
			case Code.Ldind_U2:
				visitor.OnLdind_U2 (instruction);
				return;
			case Code.Ldind_I4:
				visitor.OnLdind_I4 (instruction);
				return;
			case Code.Ldind_U4:
				visitor.OnLdind_U4 (instruction);
				return;
			case Code.Ldind_I8:
				visitor.OnLdind_I8 (instruction);
				return;
			case Code.Ldind_I:
				visitor.OnLdind_I (instruction);
				return;
			case Code.Ldind_R4:
				visitor.OnLdind_R4 (instruction);
				return;
			case Code.Ldind_R8:
				visitor.OnLdind_R8 (instruction);
				return;
			case Code.Ldind_Ref:
				visitor.OnLdind_Ref (instruction);
				return;
			case Code.Stind_Ref:
				visitor.OnStind_Ref (instruction);
				return;
			case Code.Stind_I1:
				visitor.OnStind_I1 (instruction);
				return;
			case Code.Stind_I2:
				visitor.OnStind_I2 (instruction);
				return;
			case Code.Stind_I4:
				visitor.OnStind_I4 (instruction);
				return;
			case Code.Stind_I8:
				visitor.OnStind_I8 (instruction);
				return;
			case Code.Stind_R4:
				visitor.OnStind_R4 (instruction);
				return;
			case Code.Stind_R8:
				visitor.OnStind_R8 (instruction);
				return;
			case Code.Add:
				visitor.OnAdd (instruction);
				return;
			case Code.Sub:
				visitor.OnSub (instruction);
				return;
			case Code.Mul:
				visitor.OnMul (instruction);
				return;
			case Code.Div:
				visitor.OnDiv (instruction);
				return;
			case Code.Div_Un:
				visitor.OnDiv_Un (instruction);
				return;
			case Code.Rem:
				visitor.OnRem (instruction);
				return;
			case Code.Rem_Un:
				visitor.OnRem_Un (instruction);
				return;
			case Code.And:
				visitor.OnAnd (instruction);
				return;
			case Code.Or:
				visitor.OnOr (instruction);
				return;
			case Code.Xor:
				visitor.OnXor (instruction);
				return;
			case Code.Shl:
				visitor.OnShl (instruction);
				return;
			case Code.Shr:
				visitor.OnShr (instruction);
				return;
			case Code.Shr_Un:
				visitor.OnShr_Un (instruction);
				return;
			case Code.Neg:
				visitor.OnNeg (instruction);
				return;
			case Code.Not:
				visitor.OnNot (instruction);
				return;
			case Code.Conv_I1:
				visitor.OnConv_I1 (instruction);
				return;
			case Code.Conv_I2:
				visitor.OnConv_I2 (instruction);
				return;
			case Code.Conv_I4:
				visitor.OnConv_I4 (instruction);
				return;
			case Code.Conv_I8:
				visitor.OnConv_I8 (instruction);
				return;
			case Code.Conv_R4:
				visitor.OnConv_R4 (instruction);
				return;
			case Code.Conv_R8:
				visitor.OnConv_R8 (instruction);
				return;
			case Code.Conv_U4:
				visitor.OnConv_U4 (instruction);
				return;
			case Code.Conv_U8:
				visitor.OnConv_U8 (instruction);
				return;
			case Code.Callvirt:
				visitor.OnCallvirt (instruction);
				return;
			case Code.Cpobj:
				visitor.OnCpobj (instruction);
				return;
			case Code.Ldobj:
				visitor.OnLdobj (instruction);
				return;
			case Code.Ldstr:
				visitor.OnLdstr (instruction);
				return;
			case Code.Newobj:
				visitor.OnNewobj (instruction);
				return;
			case Code.Castclass:
				visitor.OnCastclass (instruction);
				return;
			case Code.Isinst:
				visitor.OnIsinst (instruction);
				return;
			case Code.Conv_R_Un:
				visitor.OnConv_R_Un (instruction);
				return;
			case Code.Unbox:
				visitor.OnUnbox (instruction);
				return;
			case Code.Throw:
				visitor.OnThrow (instruction);
				return;
			case Code.Ldfld:
				visitor.OnLdfld (instruction);
				return;
			case Code.Ldflda:
				visitor.OnLdflda (instruction);
				return;
			case Code.Stfld:
				visitor.OnStfld (instruction);
				return;
			case Code.Ldsfld:
				visitor.OnLdsfld (instruction);
				return;
			case Code.Ldsflda:
				visitor.OnLdsflda (instruction);
				return;
			case Code.Stsfld:
				visitor.OnStsfld (instruction);
				return;
			case Code.Stobj:
				visitor.OnStobj (instruction);
				return;
			case Code.Conv_Ovf_I1_Un:
				visitor.OnConv_Ovf_I1_Un (instruction);
				return;
			case Code.Conv_Ovf_I2_Un:
				visitor.OnConv_Ovf_I2_Un (instruction);
				return;
			case Code.Conv_Ovf_I4_Un:
				visitor.OnConv_Ovf_I4_Un (instruction);
				return;
			case Code.Conv_Ovf_I8_Un:
				visitor.OnConv_Ovf_I8_Un (instruction);
				return;
			case Code.Conv_Ovf_U1_Un:
				visitor.OnConv_Ovf_U1_Un (instruction);
				return;
			case Code.Conv_Ovf_U2_Un:
				visitor.OnConv_Ovf_U2_Un (instruction);
				return;
			case Code.Conv_Ovf_U4_Un:
				visitor.OnConv_Ovf_U4_Un (instruction);
				return;
			case Code.Conv_Ovf_U8_Un:
				visitor.OnConv_Ovf_U8_Un (instruction);
				return;
			case Code.Conv_Ovf_I_Un:
				visitor.OnConv_Ovf_I_Un (instruction);
				return;
			case Code.Conv_Ovf_U_Un:
				visitor.OnConv_Ovf_U_Un (instruction);
				return;
			case Code.Box:
				visitor.OnBox (instruction);
				return;
			case Code.Newarr:
				visitor.OnNewarr (instruction);
				return;
			case Code.Ldlen:
				visitor.OnLdlen (instruction);
				return;
			case Code.Ldelema:
				visitor.OnLdelema (instruction);
				return;
			case Code.Ldelem_I1:
				visitor.OnLdelem_I1 (instruction);
				return;
			case Code.Ldelem_U1:
				visitor.OnLdelem_U1 (instruction);
				return;
			case Code.Ldelem_I2:
				visitor.OnLdelem_I2 (instruction);
				return;
			case Code.Ldelem_U2:
				visitor.OnLdelem_U2 (instruction);
				return;
			case Code.Ldelem_I4:
				visitor.OnLdelem_I4 (instruction);
				return;
			case Code.Ldelem_U4:
				visitor.OnLdelem_U4 (instruction);
				return;
			case Code.Ldelem_I8:
				visitor.OnLdelem_I8 (instruction);
				return;
			case Code.Ldelem_I:
				visitor.OnLdelem_I (instruction);
				return;
			case Code.Ldelem_R4:
				visitor.OnLdelem_R4 (instruction);
				return;
			case Code.Ldelem_R8:
				visitor.OnLdelem_R8 (instruction);
				return;
			case Code.Ldelem_Ref:
				visitor.OnLdelem_Ref (instruction);
				return;
			case Code.Stelem_I:
				visitor.OnStelem_I (instruction);
				return;
			case Code.Stelem_I1:
				visitor.OnStelem_I1 (instruction);
				return;
			case Code.Stelem_I2:
				visitor.OnStelem_I2 (instruction);
				return;
			case Code.Stelem_I4:
				visitor.OnStelem_I4 (instruction);
				return;
			case Code.Stelem_I8:
				visitor.OnStelem_I8 (instruction);
				return;
			case Code.Stelem_R4:
				visitor.OnStelem_R4 (instruction);
				return;
			case Code.Stelem_R8:
				visitor.OnStelem_R8 (instruction);
				return;
			case Code.Stelem_Ref:
				visitor.OnStelem_Ref (instruction);
				return;
			case Code.Ldelem_Any:
				visitor.OnLdelem_Any (instruction);
				return;
			case Code.Stelem_Any:
				visitor.OnStelem_Any (instruction);
				return;
			case Code.Unbox_Any:
				visitor.OnUnbox_Any (instruction);
				return;
			case Code.Conv_Ovf_I1:
				visitor.OnConv_Ovf_I1 (instruction);
				return;
			case Code.Conv_Ovf_U1:
				visitor.OnConv_Ovf_U1 (instruction);
				return;
			case Code.Conv_Ovf_I2:
				visitor.OnConv_Ovf_I2 (instruction);
				return;
			case Code.Conv_Ovf_U2:
				visitor.OnConv_Ovf_U2 (instruction);
				return;
			case Code.Conv_Ovf_I4:
				visitor.OnConv_Ovf_I4 (instruction);
				return;
			case Code.Conv_Ovf_U4:
				visitor.OnConv_Ovf_U4 (instruction);
				return;
			case Code.Conv_Ovf_I8:
				visitor.OnConv_Ovf_I8 (instruction);
				return;
			case Code.Conv_Ovf_U8:
				visitor.OnConv_Ovf_U8 (instruction);
				return;
			case Code.Refanyval:
				visitor.OnRefanyval (instruction);
				return;
			case Code.Ckfinite:
				visitor.OnCkfinite (instruction);
				return;
			case Code.Mkrefany:
				visitor.OnMkrefany (instruction);
				return;
			case Code.Ldtoken:
				visitor.OnLdtoken (instruction);
				return;
			case Code.Conv_U2:
				visitor.OnConv_U2 (instruction);
				return;
			case Code.Conv_U1:
				visitor.OnConv_U1 (instruction);
				return;
			case Code.Conv_I:
				visitor.OnConv_I (instruction);
				return;
			case Code.Conv_Ovf_I:
				visitor.OnConv_Ovf_I (instruction);
				return;
			case Code.Conv_Ovf_U:
				visitor.OnConv_Ovf_U (instruction);
				return;
			case Code.Add_Ovf:
				visitor.OnAdd_Ovf (instruction);
				return;
			case Code.Add_Ovf_Un:
				visitor.OnAdd_Ovf_Un (instruction);
				return;
			case Code.Mul_Ovf:
				visitor.OnMul_Ovf (instruction);
				return;
			case Code.Mul_Ovf_Un:
				visitor.OnMul_Ovf_Un (instruction);
				return;
			case Code.Sub_Ovf:
				visitor.OnSub_Ovf (instruction);
				return;
			case Code.Sub_Ovf_Un:
				visitor.OnSub_Ovf_Un (instruction);
				return;
			case Code.Endfinally:
				visitor.OnEndfinally (instruction);
				return;
			case Code.Leave:
			case Code.Leave_S:
				visitor.OnLeave (instruction);
				return;
			case Code.Stind_I:
				visitor.OnStind_I (instruction);
				return;
			case Code.Conv_U:
				visitor.OnConv_U (instruction);
				return;
			case Code.Arglist:
				visitor.OnArglist (instruction);
				return;
			case Code.Ceq:
				visitor.OnCeq (instruction);
				return;
			case Code.Cgt:
				visitor.OnCgt (instruction);
				return;
			case Code.Cgt_Un:
				visitor.OnCgt_Un (instruction);
				return;
			case Code.Clt:
				visitor.OnClt (instruction);
				return;
			case Code.Clt_Un:
				visitor.OnClt_Un (instruction);
				return;
			case Code.Ldftn:
				visitor.OnLdftn (instruction);
				return;
			case Code.Ldvirtftn:
				visitor.OnLdvirtftn (instruction);
				return;
			case Code.Localloc:
				visitor.OnLocalloc (instruction);
				return;
			case Code.Endfilter:
				visitor.OnEndfilter (instruction);
				return;
			case Code.Unaligned:
				visitor.OnUnaligned (instruction);
				return;
			case Code.Volatile:
				visitor.OnVolatile (instruction);
				return;
			case Code.Tail:
				visitor.OnTail (instruction);
				return;
			case Code.Initobj:
				visitor.OnInitobj (instruction);
				return;
			case Code.Cpblk:
				visitor.OnCpblk (instruction);
				return;
			case Code.Initblk:
				visitor.OnInitblk (instruction);
				return;
			case Code.Rethrow:
				visitor.OnRethrow (instruction);
				return;
			case Code.Sizeof:
				visitor.OnSizeof (instruction);
				return;
			case Code.Refanytype:
				visitor.OnRefanytype (instruction);
				return;
			default:
				throw new ArgumentException (Formatter.FormatInstruction (instruction), "instruction");
			}
		}
	}
}
