// 
// StackFrameTests.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Mono.Debugging.Client;
using NUnit.Framework;

namespace MonoDevelop.Debugger.Tests
{
	public class StackFrameTests: DebugTests
	{
		DebuggerSession ds;
		StackFrame frame;
		
		public StackFrameTests (string de): base (de)
		{
		}
		
		public override void Setup ()
		{
			base.Setup ();
			ds = Start ("TestEvaluation");
			frame = ds.ActiveThread.Backtrace.GetFrame (0);
		}
		
		public override void TearDown ()
		{
			base.TearDown ();
			ds.Stop ();
			ds.Dispose ();
		}
		
		public StackFrame Frame {
			get { return frame; }
		}
		
		[Test]
		public void VirtualProperty ()
		{
			EvaluationOptions ops = EvaluationOptions.DefaultOptions.Clone ();
			ops.FlattenHierarchy = false;
			
			ObjectValue val = Frame.GetExpressionValue ("c", ops);
			Assert.IsNotNull (val);
			val.WaitHandle.WaitOne ();
			Assert.IsFalse (val.IsError);
			Assert.IsFalse (val.IsUnknown);
			
			// The C class does not have a Prop property
			
			ObjectValue prop = val.GetChild ("Prop");
			Assert.IsNull (prop);
			
			prop = val.GetChild ("PropNoVirt1");
			Assert.IsNull (prop);
			
			prop = val.GetChild ("PropNoVirt2");
			Assert.IsNull (prop);
			
			val = val.GetChild ("base");
			Assert.IsNotNull (val);
			val.WaitHandle.WaitOne ();
			Assert.IsFalse (val.IsError);
			Assert.IsFalse (val.IsUnknown);
			
			// The B class has a Prop property, value is 2
			
			prop = val.GetChild ("Prop");
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);
			
			prop = val.GetChild ("PropNoVirt1");
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);
			
			prop = val.GetChild ("PropNoVirt2");
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);
			
			val = val.GetChild ("base");
			Assert.IsNotNull (val);
			val.WaitHandle.WaitOne ();
			Assert.IsFalse (val.IsError);
			Assert.IsFalse (val.IsUnknown);
			
			// The A class has a Prop property, value is 1, but must return 2 becasue it is overriden
			
			prop = val.GetChild ("Prop");
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);
			
			prop = val.GetChild ("PropNoVirt1");
			Assert.IsNotNull (prop);
			Assert.AreEqual ("1", prop.Value);
			
			prop = val.GetChild ("PropNoVirt2");
			Assert.IsNotNull (prop);
			Assert.AreEqual ("1", prop.Value);
		}
		
	}
}

