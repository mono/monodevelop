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

using Mono.Debugging.Soft;
using Mono.Debugging.Client;

using NUnit.Framework;

namespace MonoDevelop.Debugger.Tests
{
	[TestFixture]
	public abstract class StackFrameTests: DebugTests
	{
		protected StackFrameTests (string de, bool allowTargetInvoke): base (de)
		{
			AllowTargetInvokes = allowTargetInvoke;
		}

		[TestFixtureSetUp]
		public override void SetUp ()
		{
			base.SetUp ();

			Start ("TestEvaluation");
		}

		[Test]
		public void VirtualProperty ()
		{
			var soft = Session as SoftDebuggerSession;
			if (soft != null && soft.ProtocolVersion < new Version (2, 31))
				Assert.Ignore ("A newer version of the Mono runtime is required.");

			var ops = EvaluationOptions.DefaultOptions.Clone ();
			ops.FlattenHierarchy = false;

			ObjectValue val = Frame.GetExpressionValue ("c", ops);
			Assert.IsNotNull (val);
			val = val.Sync ();
			Assert.IsFalse (val.IsError);
			Assert.IsFalse (val.IsUnknown);

			// The C class does not have a Prop property

			ObjectValue prop = val.GetChildSync ("Prop", ops);
			Assert.IsNull (prop);

			prop = val.GetChildSync ("PropNoVirt1", ops);
			Assert.IsNull (prop);

			prop = val.GetChildSync ("PropNoVirt2", ops);
			Assert.IsNull (prop);

			val = val.GetChildSync ("base", ops);
			Assert.IsNotNull (val);
			val.WaitHandle.WaitOne ();
			Assert.IsFalse (val.IsError);
			Assert.IsFalse (val.IsUnknown);

			// The B class has a Prop property, value is 2

			prop = val.GetChildSync ("Prop", ops);
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);

			prop = val.GetChildSync ("PropNoVirt1", ops);
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);

			prop = val.GetChildSync ("PropNoVirt2", ops);
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);

			val = val.GetChildSync ("base", ops);
			Assert.IsNotNull (val);
			val.WaitHandle.WaitOne ();
			Assert.IsFalse (val.IsError);
			Assert.IsFalse (val.IsUnknown);

			// The A class has a Prop property, value is 1, but must return 2 becasue it is overriden

			prop = val.GetChildSync ("Prop", ops);
			Assert.IsNotNull (prop);
			Assert.AreEqual ("2", prop.Value);

			prop = val.GetChildSync ("PropNoVirt1", ops);
			Assert.IsNotNull (prop);
			Assert.AreEqual ("1", prop.Value);

			prop = val.GetChildSync ("PropNoVirt2", ops);
			Assert.IsNotNull (prop);
			Assert.AreEqual ("1", prop.Value);
		}
	}
}
