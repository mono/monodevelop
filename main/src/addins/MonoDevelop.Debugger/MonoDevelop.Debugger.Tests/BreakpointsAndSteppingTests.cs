//
// BreakpointsAndSteppingTests.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using NUnit.Framework;
using Mono.Debugging.Client;
using System.Collections.Generic;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Tests
{
	[TestFixture]
	public abstract class BreakpointsAndSteppingTests:DebugTests
	{
		protected BreakpointsAndSteppingTests (string de)
			: base (de)
		{
		}

		[TestFixtureSetUp]
		public override void SetUp ()
		{
			base.SetUp ();
			Start ("BreakpointsAndStepping");
		}

		[Test]
		public void OneLineProperty ()
		{
			InitializeTest ();
			AddBreakpoint ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StartTest ("OneLineProperty");
			CheckPosition ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StepIn ("3722cad3-7da1-4c86-a398-bb2cf6cc65a9", "{");
			StepIn ("3722cad3-7da1-4c86-a398-bb2cf6cc65a9", "return");
			StepIn ("3722cad3-7da1-4c86-a398-bb2cf6cc65a9", "}");
			StepIn ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StepIn ("36c0a44a-44ac-4676-b99b-9a58b73bae9d");
		}

		/// <summary>
		/// Bug 775
		/// </summary>
		[Test]
		public void StepOverPropertiesAndOperatorsSetting ()
		{
			InitializeTest ();
			//This is default but lets set again for code readability
			Session.Options.StepOverPropertiesAndOperators = false;
			AddBreakpoint ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StartTest ("OneLineProperty");
			CheckPosition ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StepIn ("3722cad3-7da1-4c86-a398-bb2cf6cc65a9");


			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StartTest ("OneLineProperty");
			CheckPosition ("8e7787ed-699f-4512-b52a-5a0629a0b9eb");
			StepIn ("36c0a44a-44ac-4676-b99b-9a58b73bae9d");


			InitializeTest ();
			//This is default but lets set again for code readability
			Session.Options.StepOverPropertiesAndOperators = false;
			AddBreakpoint ("6049ea77-e04a-43ba-907a-5d198727c448");
			StartTest ("TestOperators");
			CheckPosition ("6049ea77-e04a-43ba-907a-5d198727c448");
			StepIn ("5a3eb8d5-88f5-49c0-913f-65018e5a1c5c");


			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("6049ea77-e04a-43ba-907a-5d198727c448");
			StartTest ("TestOperators");
			CheckPosition ("6049ea77-e04a-43ba-907a-5d198727c448");
			StepIn ("49737db6-e62b-4c5e-8758-1a9d655be11a");
		}

		[Test]
		public void StaticConstructorStepping ()
		{
			InitializeTest ();
			AddBreakpoint ("6c42f31b-ca4f-4963-bca1-7d7c163087f1");
			StartTest ("StaticConstructorStepping");
			CheckPosition ("6c42f31b-ca4f-4963-bca1-7d7c163087f1");
			StepOver ("7e6862cd-bf31-486c-94fe-19933ae46094");
		}

		[Test]
		public void SteppingInsidePropertyWhenStepInPropertyDisabled ()
		{
			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("e0082b9a-26d7-4279-8749-31cd13866abf");
			StartTest ("SteppingInsidePropertyWhenStepInPropertyDisabled");
			CheckPosition ("e0082b9a-26d7-4279-8749-31cd13866abf");
			StepIn ("04f1ce38-121a-4ce7-b4ba-14fb3f6184a2");
		}

		[Test]
		public void CheckIfNull ()
		{
			InitializeTest ();
			AddBreakpoint ("d42a19ec-98db-4166-a3b4-fc102ebd7905");
			StartTest ("CheckIfNull");
			CheckPosition ("d42a19ec-98db-4166-a3b4-fc102ebd7905");
			StepIn ("c5361deb-aff5-468f-9293-0d2e50fc62fd");
			StepIn ("10e0f5c7-4c77-4897-8324-deef9aae0192");
			StepIn ("40f0acc2-2de2-44c8-8e18-3867151ba8da");
			StepIn ("3c0316e9-eace-48e8-b9ed-03a8c6306c66", 1);
			StepIn ("d42a19ec-98db-4166-a3b4-fc102ebd7905");
			StepIn ("f633d197-cb92-418a-860c-4d8eadbe2342");
			StepIn ("c5361deb-aff5-468f-9293-0d2e50fc62fd");
			StepIn ("10e0f5c7-4c77-4897-8324-deef9aae0192");
			StepIn ("ae71a41d-0c90-433d-b925-0b236b8119a9");
			StepIn ("3c0316e9-eace-48e8-b9ed-03a8c6306c66", 1);
			StepIn ("f633d197-cb92-418a-860c-4d8eadbe2342");
			StepIn ("6d50c480-1cd1-49a9-9758-05f65c07c037");
		}

		/// <summary>
		/// Bug 4015
		/// </summary>
		[Test]
		public void SimpleConstrutor ()
		{
			InitializeTest ();
			AddBreakpoint ("d62ff7ab-02fa-4205-a432-b4569709eab6");
			StartTest ("SimpleConstrutor");
			CheckPosition ("d62ff7ab-02fa-4205-a432-b4569709eab6");
			StepIn ("1f37aea1-77a1-40c1-9ea5-797db48a14f9", 1, "public");
			StepIn ("494fddfb-85f1-4ad0-b5b3-9b2f990bb6d0", -1, "{");
			StepIn ("494fddfb-85f1-4ad0-b5b3-9b2f990bb6d0", "int");
			StepIn ("494fddfb-85f1-4ad0-b5b3-9b2f990bb6d0", 1, "}");
			StepIn ("d62ff7ab-02fa-4205-a432-b4569709eab6", "var");
			StepIn ("d62ff7ab-02fa-4205-a432-b4569709eab6", 1, "}");
		}

		/// <summary>
		/// Bug 3262
		/// </summary>
		[Test]
		public void NoConstructor ()
		{
			InitializeTest ();
			Session.Options.ProjectAssembliesOnly = true;
			AddBreakpoint ("84fc04b2-ede2-4d8b-acc4-28441e1c5f55");
			StartTest ("NoConstructor");
			CheckPosition ("84fc04b2-ede2-4d8b-acc4-28441e1c5f55");
			StepIn ("84fc04b2-ede2-4d8b-acc4-28441e1c5f55", 1);
		}

		[Test]
		public void IfPropertyStepping ()
		{
			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("0c64d51c-40b3-4d20-b7e3-4e3e641ec52a");
			StartTest ("IfPropertyStepping");
			CheckPosition ("0c64d51c-40b3-4d20-b7e3-4e3e641ec52a");
			StepIn ("ac7625ef-ebbd-4543-b7ff-c9c5d26fd8b4");
		}

		/// <summary>
		/// Bug 3565
		/// </summary>
		[Test]
		public void DebuggerHiddenMethod ()
		{
			InitializeTest ();
			AddBreakpoint ("b0abae8d-fbd0-4bde-b586-bb511b954d8a");
			StartTest ("DebuggerHiddenMethod");
			CheckPosition ("b0abae8d-fbd0-4bde-b586-bb511b954d8a");
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("b0abae8d-fbd0-4bde-b586-bb511b954d8a");
			StepIn ("b0abae8d-fbd0-4bde-b586-bb511b954d8a", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (3).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (4).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (5).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (3).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (4).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (3).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("b0abae8d-fbd0-4bde-b586-bb511b954d8a", 1);
			StepIn ("b0abae8d-fbd0-4bde-b586-bb511b954d8a", 2);
			StepIn ("b0abae8d-fbd0-4bde-b586-bb511b954d8a", 3);
		}

		/// <summary>
		/// Bug 3565
		/// </summary>
		[Test]
		public void DebuggerNonUserCodeMethod ()
		{
			InitializeTest ();
			Session.Options.ProjectAssembliesOnly = false;
			AddBreakpoint ("02757896-0e76-40b8-8235-d09d2110da78");
			StartTest ("DebuggerNonUserCodeMethod");
			CheckPosition ("02757896-0e76-40b8-8235-d09d2110da78");
			//entering testClass.DebuggerNonUserCodeMethod (true);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", -1);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -2);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			//entering EmptyTestMethod
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			//exited EmptyTestMethod
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 2);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78");
			//exited testClass.DebuggerNonUserCodeMethod (true);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 1);
			//entering testClass.DebuggerNonUserCodeMethod (true, 3); starts here
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", -1);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", 1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", -2);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", -1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7");
			//entering resursion
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", -1);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", 1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", -2);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", -1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7");
			//entering resursion
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", -1);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", 1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", -2);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", -1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7");
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", -1);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -2);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			//entering EmptyTestMethod
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			//exited EmptyTestMethod
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 2);
			//returning resursion
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7");
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", 1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", 2);

			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -2);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			//entering EmptyTestMethod
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			//exited EmptyTestMethod
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 2);
			//returning resursion
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7");
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", 1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", 2);

			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -2);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			//entering EmptyTestMethod
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			//exited EmptyTestMethod
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 2);
			//returning resursion
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7");
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", 1);
			StepIn ("6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7", 2);

			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -2);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			//entering EmptyTestMethod
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			//exited EmptyTestMethod
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce");
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 2);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 1);
			//exited testClass.DebuggerNonUserCodeMethod (true, 3);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 2);
			//entering testClass.DebuggerNonUserCodeMethod (false);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", -1);
			StepIn ("5b9b96b6-ce24-413f-8660-715fccfc412f", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -2);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", -1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 1);
			StepIn ("754272b8-a14b-4de0-9075-6a911c37e6ce", 2);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 2);
			//exited testClass.DebuggerNonUserCodeMethod (false);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 3);

			InitializeTest ();
			Session.Options.ProjectAssembliesOnly = true;
			AddBreakpoint ("02757896-0e76-40b8-8235-d09d2110da78");
			StartTest ("DebuggerNonUserCodeMethod");
			CheckPosition ("02757896-0e76-40b8-8235-d09d2110da78");
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78");
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (3).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (4).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (5).IsExternalCode);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (3).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (4).IsExternalCode);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (3).IsExternalCode);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 1);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 2);
			StepIn ("02757896-0e76-40b8-8235-d09d2110da78", 3);
		}

		/// <summary>
		/// Bug 3565
		/// </summary>
		[Test]
		public void DebuggerStepperBoundaryMethod ()
		{
			InitializeTest ();
			AddBreakpoint ("0b7eef17-af79-4b34-b4fc-cede110f20fe");
			AddBreakpoint ("806c13f8-8a59-4ae0-83a2-33191368af47");
			StartTest ("DebuggerStepperBoundaryMethod");
			CheckPosition ("0b7eef17-af79-4b34-b4fc-cede110f20fe");
			StepIn ("806c13f8-8a59-4ae0-83a2-33191368af47");//This actually means it hit 2nd breakpoint
			//because [DebuggerStepperBoundary] actually means if you step into this method
			//its looks like pressing F5
		}

		/// <summary>
		/// Bug 21510
		/// </summary>
		[Test]
		public void DebuggerStepperBoundaryMethod2ProjectAssembliesOnly ()
		{
			InitializeTest ();
			Session.Options.ProjectAssembliesOnly = true;
			AddBreakpoint ("f3a22b38-596a-4463-a562-20b342fdec12");
			AddBreakpoint ("4721f27a-a268-4529-b327-c39f208c08c5");
			StartTest ("DebuggerStepperBoundaryMethod2");
			CheckPosition ("f3a22b38-596a-4463-a562-20b342fdec12");
			StepIn ("4721f27a-a268-4529-b327-c39f208c08c5");
		}

		/// <summary>
		/// Bug 21510
		/// </summary>
		[Test]
		public void DebuggerStepperBoundaryMethod2 ()
		{
			InitializeTest ();
			Session.Options.ProjectAssembliesOnly = false;
			AddBreakpoint ("f3a22b38-596a-4463-a562-20b342fdec12");
			AddBreakpoint ("4721f27a-a268-4529-b327-c39f208c08c5");
			StartTest ("DebuggerStepperBoundaryMethod2");
			CheckPosition ("f3a22b38-596a-4463-a562-20b342fdec12");
			StepIn ("d110546f-a622-4ec3-9564-1c51bfec28f9", -1);
			StepIn ("d110546f-a622-4ec3-9564-1c51bfec28f9");
			StepIn ("4721f27a-a268-4529-b327-c39f208c08c5");
		}

		/// <summary>
		/// Bug 3565
		/// </summary>
		[Test]
		public void DebuggerStepThroughMethod ()
		{
			InitializeTest ();
			AddBreakpoint ("707ccd6c-3464-4700-8487-a83c948aa0c3");
			StartTest ("DebuggerStepThroughMethod");
			CheckPosition ("707ccd6c-3464-4700-8487-a83c948aa0c3");
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (0).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsDebuggerHidden);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (2).IsDebuggerHidden);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("707ccd6c-3464-4700-8487-a83c948aa0c3");
			StepIn ("707ccd6c-3464-4700-8487-a83c948aa0c3", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", -1);
			StepIn ("49326780-f51b-4510-a52c-03e7af442dda", 1);
			StepIn ("707ccd6c-3464-4700-8487-a83c948aa0c3", 1);
			StepIn ("707ccd6c-3464-4700-8487-a83c948aa0c3", 2);
			StepIn ("707ccd6c-3464-4700-8487-a83c948aa0c3", 3);
		}

		/// <summary>
		/// This test is very specific because of Win32 debugger bug
		/// Placing breakpoint inside delegate fails if some other
		/// breakpoint adding failed before this(invalidBreakpointAtEndOfFile)
		/// </summary>
		[Test]
		public void BreakpointInsideDelegate ()
		{
			InitializeTest ();
			AddBreakpoint ("f3b6862d-732b-4f68-81f5-f362d5a092e2");
			StartTest ("BreakpointInsideDelegate");
			CheckPosition ("f3b6862d-732b-4f68-81f5-f362d5a092e2");
			AddBreakpoint ("invalidBreakpointAtEndOfFile");
			AddBreakpoint ("ffde3c82-4310-43d3-93d1-4c39e9cf615e");
			Continue ("ffde3c82-4310-43d3-93d1-4c39e9cf615e");
		}

		[Test]
		public void BreakpointInsideOneLineDelegateNoDisplayClass ()
		{
			InitializeTest ();
			AddBreakpoint ("e0a96c37-577f-43e3-9a20-2cdd8bf7824e");
			AddBreakpoint ("e72a2fa6-2d95-4f96-b3d0-ba321da3cb55", statement: "Console.WriteLine");
			StartTest ("BreakpointInsideOneLineDelegateNoDisplayClass");
			CheckPosition ("e0a96c37-577f-43e3-9a20-2cdd8bf7824e");
			StepOver ("e72a2fa6-2d95-4f96-b3d0-ba321da3cb55", "Console.WriteLine");
			StepOut ("3be64647-76c1-455b-a4a7-a21b37383dcb");
			StepOut ("e0a96c37-577f-43e3-9a20-2cdd8bf7824e");
		}

		[Test]
		public void BreakpointInsideOneLineDelegate ()
		{
			InitializeTest ();
			AddBreakpoint ("67ae4cce-22b3-49d8-8221-7e5b26a5e79b");
			AddBreakpoint ("22af08d6-dafc-47f1-b8d1-bee1526840fd", statement: "button.SetTitle");
			StartTest ("BreakpointInsideOneLineDelegate");
			CheckPosition ("67ae4cce-22b3-49d8-8221-7e5b26a5e79b");
			StepOver ("22af08d6-dafc-47f1-b8d1-bee1526840fd", "button.SetTitle");
			StepOut ("3be64647-76c1-455b-a4a7-a21b37383dcb");
			StepOut ("67ae4cce-22b3-49d8-8221-7e5b26a5e79b");
		}

		[Test]
		public void BreakpointInsideOneLineDelegateAsync ()
		{
			InitializeTest ();
			AddBreakpoint ("b6a65e9e-5db2-4850-969a-b3747b2459af", statement: "button.SetTitle");
			AddBreakpoint ("b6a65e9e-5db2-4850-969a-b3747b2459af", 1);
			StartTest ("BreakpointInsideOneLineDelegateAsync");
			CheckPosition ("b6a65e9e-5db2-4850-969a-b3747b2459af", 1);
			StepOver ("b6a65e9e-5db2-4850-969a-b3747b2459af", "button.SetTitle");
			if (Session is SoftDebuggerSession) {
				StepOut ("3be64647-76c1-455b-a4a7-a21b37383dcb");
			} else {
				StepOut ("3be64647-76c1-455b-a4a7-a21b37383dcb", 1);//Feels like CorDebugger bug
			}
			StepOut ("b6a65e9e-5db2-4850-969a-b3747b2459af", 1);
		}

		/// <summary>
		/// Bug 2851
		/// </summary>
		[Test]
		public void ForeachEnumerable ()
		{
			IgnoreSoftDebugger ("Sdb has some problems when stepping into yeild methods. Have to investigate");

			InitializeTest ();
			AddBreakpoint ("b73bec88-2c43-4157-8574-ad517730bc74");
			StartTest ("ForeachEnumerable");
			CheckPosition ("b73bec88-2c43-4157-8574-ad517730bc74");
			StepOver ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "foreach");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "testClass.Iter_1");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			StepIn ("1463a77d-f27e-4bcd-8f92-89a682faa1c7", -1, "{");
			StepIn ("1463a77d-f27e-4bcd-8f92-89a682faa1c7", "yield return 1;");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "var");
			StepIn ("69dba3ab-0941-47e9-99fa-10222a2e894d", -1, "{");
			StepIn ("69dba3ab-0941-47e9-99fa-10222a2e894d", 1, "}");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			StepIn ("1463a77d-f27e-4bcd-8f92-89a682faa1c7", 1, "yield return 2;");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "var");
			StepIn ("69dba3ab-0941-47e9-99fa-10222a2e894d", -1, "{");
			StepIn ("69dba3ab-0941-47e9-99fa-10222a2e894d", 1, "}");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			StepIn ("1463a77d-f27e-4bcd-8f92-89a682faa1c7", 2, "}");
			StepIn ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			StepIn ("e01a5428-b067-4ca3-ac8c-a19d5d800228", 1, "}");
		}

		[Test]
		public void SetBreakpointOnColumn ()
		{
			InitializeTest ();
			AddBreakpoint ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "testClass.Iter_1");
			AddBreakpoint ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			AddBreakpoint ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "var");
			AddBreakpoint ("e01a5428-b067-4ca3-ac8c-a19d5d800228", 1);//end of method
			StartTest ("ForeachEnumerable");
			CheckPosition ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "testClass.Iter_1");
			Continue ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			Continue ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "var");
			Continue ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			Continue ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "var");
			Continue ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			Continue ("e01a5428-b067-4ca3-ac8c-a19d5d800228", 1);//end of method
		}

		[Test]
		public void RunToCursorTest ()
		{
			InitializeTest ();
			AddBreakpoint ("b73bec88-2c43-4157-8574-ad517730bc74");
			StartTest ("ForeachEnumerable");
			CheckPosition ("b73bec88-2c43-4157-8574-ad517730bc74");
			RunToCursor ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "testClass.Iter_1");
			RunToCursor ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "var");
			RunToCursor ("b73bec88-2c43-4157-8574-ad517730bc74", 1, "in");
			RunToCursor ("69dba3ab-0941-47e9-99fa-10222a2e894d", 1, "}");
			RunToCursor ("e01a5428-b067-4ca3-ac8c-a19d5d800228", 1);
		}

		[Test]
		public void RunToCursorTest2 ()
		{
			InitializeTest ();
			AddBreakpoint ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 1);
			StartTest ("SimpleMethod");
			CheckPosition ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 1);
			RunToCursor ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 3);
		}

		/// <summary>
		/// Bug 4032
		/// </summary>
		[Test]
		public void PListSchemeTest ()
		{
			IgnoreSoftDebugger ("Sdb is reapeating StepIn in StaticConstructor instead of StepOut. Resulting in step stopping at unexpected location.");

			InitializeTest ();
			AddBreakpoint ("41eb3a30-3b19-4ea5-a7dc-e4c76871f391");
			StartTest ("PListSchemeTest");
			CheckPosition ("41eb3a30-3b19-4ea5-a7dc-e4c76871f391");
			StepIn ("c9b18785-1348-42e3-a479-9cac1e7c5360", -1);
		}

		/// <summary>
		/// Bug 4433 StepOverPropertiesAndOperators = true
		/// </summary>
		[Test]
		public void Bug4433StepOverProperties ()
		{
			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("a062e69c-e3f7-4fd7-8985-fc7abd5c27d2");
			StartTest ("Bug4433Test");
			CheckPosition ("a062e69c-e3f7-4fd7-8985-fc7abd5c27d2");
			StepIn ("ad9b8803-eef0-438c-bf2b-9156782f4027", -1);
		}

		/// <summary>
		/// Bug 4433 StepOverPropertiesAndOperators = false
		/// </summary>
		[Test]
		public void Bug4433 ()
		{
			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = false;
			AddBreakpoint ("a062e69c-e3f7-4fd7-8985-fc7abd5c27d2");
			StartTest ("Bug4433Test");
			CheckPosition ("a062e69c-e3f7-4fd7-8985-fc7abd5c27d2");
			StepIn ("ad9b8803-eef0-438c-bf2b-9156782f4027", -1);
		}

		/// <summary>
		/// Bug 5386
		/// </summary>
		[Test]
		public void EmptyForLoopTest ()
		{
			InitializeTest ();
			AddBreakpoint ("946d5781-a162-4cd9-a7b6-c320564cc594", -1);
			StartTest ("EmptyForLoopTest");
			CheckPosition ("946d5781-a162-4cd9-a7b6-c320564cc594", -1);
			//make 3 loops...
			StepIn ("a2ff92da-3796-47e3-886a-4bd786a07547", -1);
			StepIn ("a2ff92da-3796-47e3-886a-4bd786a07547", 1);
			StepIn ("a2ff92da-3796-47e3-886a-4bd786a07547", -1);
			StepIn ("a2ff92da-3796-47e3-886a-4bd786a07547", 1);
			StepIn ("a2ff92da-3796-47e3-886a-4bd786a07547", -1);
			StepIn ("a2ff92da-3796-47e3-886a-4bd786a07547", 1);
		}

		/// <summary>
		/// Bug 6724
		/// </summary>
		[Test]
		public void CallMethodWithPropertyAsArgument ()
		{
			InitializeTest ();
			Session.Options.StepOverPropertiesAndOperators = true;
			AddBreakpoint ("1c3e65ca-3201-42ba-9c6e-6f9a45ddac44");
			StartTest ("CallMethodWithPropertyAsArgument");
			CheckPosition ("1c3e65ca-3201-42ba-9c6e-6f9a45ddac44");
			StepIn ("c25be44e-ead3-4891-ab42-0e4cf8450f7a", -1);
			StepOut ("1c3e65ca-3201-42ba-9c6e-6f9a45ddac44");
			StepIn ("1c3e65ca-3201-42ba-9c6e-6f9a45ddac44", 1);
			StepIn ("c25be44e-ead3-4891-ab42-0e4cf8450f7a", -1);
		}

		/// <summary>
		/// Bug 7901
		/// </summary>
		[Test]
		public void Bug7901 ()
		{
			InitializeTest ();
			AddBreakpoint ("956bd9fd-39fe-4587-9d9e-a2a817d76286");
			StartTest ("TestBug7901");
			CheckPosition ("956bd9fd-39fe-4587-9d9e-a2a817d76286");
			StepIn ("f456a9b0-9c1a-4b34-bef4-d80b8541ebdb", -1);
			StepIn ("f456a9b0-9c1a-4b34-bef4-d80b8541ebdb", 1);
			StepIn ("11259de1-944d-4052-b970-62662e21876a", -1);
			StepIn ("11259de1-944d-4052-b970-62662e21876a");
			StepIn ("11259de1-944d-4052-b970-62662e21876a", 1);
			StepIn ("11259de1-944d-4052-b970-62662e21876a", 2);
			StepIn ("4863ebb7-8c90-4704-af8b-66a9f53657b9");
			StepOut ("956bd9fd-39fe-4587-9d9e-a2a817d76286");
		}

		/// <summary>
		/// Bug 10782
		/// </summary>
		[Test]
		public void Bug10782 ()
		{
			InitializeTest ();
			AddBreakpoint ("cdcabe93-4f55-4dbb-821e-912097c4f727");
			StartTest ("TestBug10782");
			CheckPosition ("cdcabe93-4f55-4dbb-821e-912097c4f727");
			StepIn ("1f37aea1-77a1-40c1-9ea5-797db48a14f9", 1);
			StepOut ("cdcabe93-4f55-4dbb-821e-912097c4f727");
			StepIn ("3bda6643-6d06-4504-a4da-91bc8c5eb887", -1);
		}

		/// <summary>
		/// Bug 11868
		/// </summary>
		[Test]
		[Ignore ("Todo")]
		public void AwaitCall ()
		{
			InitializeTest ();
			AddBreakpoint ("a221c9d4-6d00-4fce-99e6-d712e9a23c02", -1);
			StartTest ("TestAwaitCall");
			CheckPosition ("a221c9d4-6d00-4fce-99e6-d712e9a23c02", -1);
			StepOver ("a221c9d4-6d00-4fce-99e6-d712e9a23c02");
			StepOver ("a221c9d4-6d00-4fce-99e6-d712e9a23c02", 1);
			StepOver ("a221c9d4-6d00-4fce-99e6-d712e9a23c02", 2);
		}

		/// <summary>
		/// Bug 13396
		/// </summary>
		[Test]
		[Ignore ("This is not working in VS as well is this doable or should bug be closed as invalid?")]
		public void StepInsideAwaitTaskRun ()
		{
			InitializeTest ();
			AddBreakpoint ("a221c9d4-6d00-4fce-99e6-d712e9a23c02", -1);
			StartTest ("StepInsideAwaitTaskRun");
			CheckPosition ("a221c9d4-6d00-4fce-99e6-d712e9a23c02", -1);
			StepIn ("a221c9d4-6d00-4fce-99e6-d712e9a23c02");
			StepIn ("a221c9d4-6d00-4fce-99e6-d712e9a23c02");//Now we are on delegate
			//entering EmptyMethod
			StepIn ("3c27f60f-fdfa-44c0-b58f-552ecaaa77f1", -1);
			StepIn ("3c27f60f-fdfa-44c0-b58f-552ecaaa77f1", 1);
			StepIn ("a221c9d4-6d00-4fce-99e6-d712e9a23c02");//Back at delegate
			StepIn ("a221c9d4-6d00-4fce-99e6-d712e9a23c02");//Back at await?
			StepIn ("a221c9d4-6d00-4fce-99e6-d712e9a23c02", 1);
		}

		/// <summary>
		/// Bug 13640
		/// </summary>
		[Test]
		public void Bug13640 ()
		{
			InitializeTest ();
			AddBreakpoint ("b64e6497-e976-4125-9741-801909e5eeb1");
			StartTest ("Bug13640");
			CheckPosition ("b64e6497-e976-4125-9741-801909e5eeb1");
			StepIn ("b64e6497-e976-4125-9741-801909e5eeb1", 1, "foreach");
			StepIn ("b64e6497-e976-4125-9741-801909e5eeb1", 1, "l");
			StepIn ("b64e6497-e976-4125-9741-801909e5eeb1", 1, "in");
			StepIn ("a90ba766-0891-4837-9b1d-e5458f6b8e07", "return");
			StepIn ("a90ba766-0891-4837-9b1d-e5458f6b8e07", 1, "}");
		}

		[Test]
		public void SetNextStatementTest ()
		{
			InitializeTest ();
			AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			SetNextStatement ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			StepIn ("3e2e4759-f6d9-4839-98e6-4fa96b227458", 1);
		}


		[Test]
		public void SetNextStatementTest2 ()
		{
			InitializeTest ();
			AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			SetNextStatement ("c35046f7-e87d-4b8f-b260-43e181a0a07c", -1, "{");
			StepIn ("c35046f7-e87d-4b8f-b260-43e181a0a07c", 1, "int");
		}

		[Test]
		public void SetNextStatementTest3 ()
		{
			InitializeTest ();
			AddBreakpoint ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 1);
			StartTest ("SimpleMethod");
			CheckPosition ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 1);
			StepOver ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 2);
			StepOver ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 3);
			StepOver ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 4);
			SetNextStatement ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 1);
			StepOver ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 2);
			StepOver ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 3);
			SetNextStatement ("f4e3a214-229e-44dd-9da2-db82ddfbec11", -1);
			StepOver ("f4e3a214-229e-44dd-9da2-db82ddfbec11", 1);
		}

		[Test]
		public void CatchPointTest1 ()
		{
			InitializeTest ();
			AddBreakpoint ("fcdc2412-c00e-4c95-b2ea-e3cf5d5bf856");
			AddCatchpoint ("System.Exception", true);
			StartTest ("Catchpoint1");
			if (!CheckPosition ("526795d3-ee9e-44a7-8423-df0b406e9e8d", 1, null, true))//Workaround for Win32 debugger which stops at +1 line
				CheckPosition ("526795d3-ee9e-44a7-8423-df0b406e9e8d");
			var ops = Session.EvaluationOptions.Clone ();
			ops.MemberEvaluationTimeout = 0;
			ops.EvaluationTimeout = 0;
			ops.EllipsizeStrings = false;

			var val = Frame.GetException (ops);
			Assert.AreEqual ("System.NotImplementedException", val.Type);

			InitializeTest ();
			AddBreakpoint ("fcdc2412-c00e-4c95-b2ea-e3cf5d5bf856");
			AddCatchpoint ("System.Exception", false);
			StartTest ("Catchpoint1");
			CheckPosition ("fcdc2412-c00e-4c95-b2ea-e3cf5d5bf856");
		}

		[Test]
		public void CatchPointTest2 ()
		{
			IgnoreSoftDebugger ("I'm having problem testing this because. There is error nonstop happening in framework about CurrentCulture featching.");

			InitializeTest ();
			AddCatchpoint ("System.Exception", true);
			StartTest ("Catchpoint2");
			CheckPosition ("d24b1c9d-3944-4f0d-be31-5556251fbdf5");
			Assert.IsTrue (Session.ActiveThread.Backtrace.GetFrame (0).IsExternalCode);
			Assert.IsFalse (Session.ActiveThread.Backtrace.GetFrame (1).IsExternalCode);
		}

		[Test]
		public void CatchpointIgnoreExceptionsInNonUserCodeTest ()
		{
			//It seems CorDebugger has different definition of what is user code and what is not.
			IgnoreCorDebugger ("CorDebugger: TODO");

			InitializeTest ();
			Session.Options.ProjectAssembliesOnly = true;
			AddBreakpoint ("999b8a83-8c32-4640-a8e1-f74309cda79c");
			AddCatchpoint ("System.Exception", true);
			StartTest ("CatchpointIgnoreExceptionsInNonUserCode");
			CheckPosition ("999b8a83-8c32-4640-a8e1-f74309cda79c");

			InitializeTest ();
			Session.Options.ProjectAssembliesOnly = false;
			AddCatchpoint ("System.Exception", true);
			AddBreakpoint ("999b8a83-8c32-4640-a8e1-f74309cda79c");
			StartTest ("CatchpointIgnoreExceptionsInNonUserCode");
			WaitStop (2000);
			Assert.AreEqual ("3913936e-3f89-4f07-a863-7275aaaa5fc9", Session.ActiveThread.Backtrace.GetFrame (0).GetException ().Message);
		}

		[Test]
		public void ConditionalBreakpoints ()
		{
			ObjectValue val;
			Breakpoint bp;

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.ConditionExpression = "i==2";
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("2", val.Value);
			Continue ("3e2e4759-f6d9-4839-98e6-4fa96b227458");

			IgnoreCorDebugger ("TODO: Conditional breakpoints with compare against string or enum is not working on CorDebugger");

			InitializeTest ();
			bp = AddBreakpoint ("033dd01d-6cb4-4e1a-b445-de6d7fa0d2a7");
			bp.ConditionExpression = "str == \"bbb\"";
			StartTest ("ConditionalBreakpointString");
			CheckPosition ("033dd01d-6cb4-4e1a-b445-de6d7fa0d2a7");
			val = Eval ("str");
			Assert.AreEqual ("\"bbb\"", val.Value);

			InitializeTest ();
			bp = AddBreakpoint ("ecf764bf-9182-48d6-adb0-0ba36e2653a7");
			bp.ConditionExpression = "en == BooleanEnum.False";
			StartTest ("ConitionalBreakpointEnum");
			CheckPosition ("ecf764bf-9182-48d6-adb0-0ba36e2653a7");
			val = Eval ("en");
			Assert.AreEqual ("BooleanEnum.False", val.Value);
		}

		[Test]
		public void HitCountBreakpoints ()
		{
			ObjectValue val;
			Breakpoint bp;

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.HitCount = 3;
			bp.HitCountMode = HitCountMode.EqualTo;
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("2", val.Value);
			Continue ("3e2e4759-f6d9-4839-98e6-4fa96b227458");

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.HitCount = 3;
			bp.HitCountMode = HitCountMode.GreaterThan;
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("3", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("4", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("5", val.Value);

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.HitCount = 3;
			bp.HitCountMode = HitCountMode.GreaterThanOrEqualTo;
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("2", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("3", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("4", val.Value);

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.HitCount = 3;
			bp.HitCountMode = HitCountMode.LessThan;
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("0", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("1", val.Value);
			Continue ("3e2e4759-f6d9-4839-98e6-4fa96b227458");

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.HitCount = 3;
			bp.HitCountMode = HitCountMode.LessThanOrEqualTo;
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("0", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("1", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("2", val.Value);
			Continue ("3e2e4759-f6d9-4839-98e6-4fa96b227458");

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.HitCount = 3;
			bp.HitCountMode = HitCountMode.MultipleOf;
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("2", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("5", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("8", val.Value);
			Continue ("3e2e4759-f6d9-4839-98e6-4fa96b227458");

			InitializeTest ();
			AddBreakpoint ("3e2e4759-f6d9-4839-98e6-4fa96b227458");
			bp = AddBreakpoint ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			bp.HitCount = 3;
			bp.HitCountMode = HitCountMode.None;
			StartTest ("ForLoop10");
			CheckPosition ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("0", val.Value);
			Continue ("eef5bea2-aaa6-4718-b26f-b35be6a6a13e");
			val = Eval ("i");
			Assert.AreEqual ("1", val.Value);
		}

		[Test]
		public void Bug13401 ()
		{
			InitializeTest ();
			AddBreakpoint ("977ee8ce-ee61-4de0-9fc1-138fa164870b");
			StartTest ("Bug13401");
			CheckPosition ("977ee8ce-ee61-4de0-9fc1-138fa164870b");
			var val = Eval ("s");
			Assert.AreEqual ("string", val.TypeName);
			Assert.AreEqual ("\"Hello from Bar\"", val.Value);
		}

		[Test]
		public void OutputAndDebugWriter ()
		{
			//Interesting fact... Debug.Write(""); produces log entry
			//but Console.Write(""); does not

			InitializeTest ();
			AddBreakpoint ("5070ed1c-593d-4cbe-b4fa-b2b0c7b25289");
			var errorsList = new List<string> ();
			errorsList.Add ("ErrorText");
			var outputList = new HashSet<string> ();
			outputList.Add ("NormalText");
			var debugList = new List<Tuple<int,string,string>> ();
			debugList.Add (new Tuple<int,string,string> (0, "", "DebugText"));
			debugList.Add (new Tuple<int, string, string> (3, "SomeCategory", "DebugText2"));

			var unexpectedOutput = new List<string> ();
			var unexpectedError = new List<string> ();
			var unexpectedDebug = new List<Tuple<int,string,string>> ();

			Session.DebugWriter = delegate(int level, string category, string message) {
				var entry = new Tuple<int,string,string> (level, category, message);
				if (entry.Equals (new Tuple<int,string,string> (0, "", "")))//Sdb is emitting some empty messages :S 
					return;
				if (debugList.Contains (entry)) {
					debugList.Remove (entry);
				} else {
					unexpectedDebug.Add (entry);
				}
			};
			Session.OutputWriter = delegate(bool isStderr, string text) {
				if (isStderr) {
					if (errorsList.Contains (text))
						errorsList.Remove (text);
					else
						unexpectedError.Add (text);
				} else {
					if (outputList.Contains (text))
						outputList.Remove (text);
					else
						unexpectedOutput.Add (text);
				}
			};
			StartTest ("OutputAndDebugWriter");
			CheckPosition ("5070ed1c-593d-4cbe-b4fa-b2b0c7b25289");
			if (outputList.Count > 0)
				Assert.Fail ("Output list still has following items:" + string.Join (",", outputList));
			if (errorsList.Count > 0)
				Assert.Fail ("Error list still has following items:" + string.Join (",", errorsList));
			if (debugList.Count > 0)
				Assert.Fail ("Debug list still has following items:" + string.Join (",", debugList));
			if (unexpectedOutput.Count > 0)
				Assert.Fail ("Unexcpected Output list has following items:" + string.Join (",", unexpectedOutput));
			if (unexpectedError.Count > 0)
				Assert.Fail ("Unexcpected Error list has following items:" + string.Join (",", unexpectedError));
			if (unexpectedDebug.Count > 0)
				Assert.Fail ("Unexcpected Debug list has following items:" + string.Join (",", unexpectedDebug));
		}

		[Test]
		public void Bug25358 ()
		{
			InitializeTest ();
			AddBreakpoint ("4b30f826-2ba0-4b53-ab36-85b2cdde1069");
			StartTest ("TestBug25358");
			CheckPosition ("4b30f826-2ba0-4b53-ab36-85b2cdde1069");
			var val = Eval ("e");
			val = val.GetChildSync ("Message", EvaluationOptions.DefaultOptions);
			Assert.AreEqual ("\"2b2c4423-accf-4c2c-af31-7d8dcee31c32\"", val.Value);
		}

		[Test]
		public void Bug21410 ()
		{
			IgnoreSoftDebugger ("Runtime bug.");

			InitializeTest ();
			AddBreakpoint ("5e6663d0-9088-40ad-914d-0fcc05b2d0d5");
			StartTest ("TestBug21410");
			CheckPosition ("5e6663d0-9088-40ad-914d-0fcc05b2d0d5");
			StepOver ("5e6663d0-9088-40ad-914d-0fcc05b2d0d5", 1);
		}
	}
}

