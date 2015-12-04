//
// BreakpointsAndStepping.cs
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;
using MonoDevelop.Debugger.Tests.NonUserCodeTestLib;

namespace MonoDevelop.Debugger.Tests.TestApp
{
	public class BreakpointsAndStepping
	{
		public static void RunTest ()
		{
			var obj = new BreakpointsAndStepping ();
			obj.Test ();
		}

		public static string NextMethodToCall = "";

		public void Test ()
		{
			while (true) {
				Console.Write ("");/*break*/
				try {
					typeof(BreakpointsAndStepping).GetMethod (NextMethodToCall).Invoke (this, null);
				} catch {
				}
			}
		}

		public void OutputAndDebugWriter ()
		{
			Console.Write ("NormalText");
			Debug.Write ("DebugText");
			Debug.Write ("");
			System.Diagnostics.Debugger.Log (3, "SomeCategory", "DebugText2");
			Console.Error.Write ("ErrorText");
			Console.Write ("");
			Console.Write ("");/*5070ed1c-593d-4cbe-b4fa-b2b0c7b25289*/
		}

		public void OneLineProperty ()
		{
			var testClass = new TestClass ();
			var a = testClass.OneLineProperty;/*8e7787ed-699f-4512-b52a-5a0629a0b9eb*/
			var b = a;/*36c0a44a-44ac-4676-b99b-9a58b73bae9d*/
		}

		public void StaticConstructorStepping ()
		{
			var test = new DontUseThisClassInOtherTests ();
		}

		public void IfPropertyStepping ()
		{
			var test = new TestClass ();
			if (test.OneLineProperty == "someInvalidValue6049e709-7271-41a1-bc0a-f1f1b80d4125")/*0c64d51c-40b3-4d20-b7e3-4e3e641ec52a*/
				return;
			Console.Write ("");/*ac7625ef-ebbd-4543-b7ff-c9c5d26fd8b4*/
		}

		public void SteppingInsidePropertyWhenStepInPropertyDisabled ()
		{
			var testClass = new TestClass ();
			var a = testClass.MultiLineProperty;
			var b = a;
		}

		public void CheckIfNull ()
		{
			var testClass = new TestClass ();
			testClass.TestMethod (null);/*d42a19ec-98db-4166-a3b4-fc102ebd7905*/
			testClass.TestMethod ("notNull");/*f633d197-cb92-418a-860c-4d8eadbe2342*/
			Console.Write ("");/*6d50c480-1cd1-49a9-9758-05f65c07c037*/
		}

		public void TestOperators ()
		{
			var testClass = new TestClass ();
			var output = testClass == testClass;/*6049ea77-e04a-43ba-907a-5d198727c448*/
			var a = output;/*49737db6-e62b-4c5e-8758-1a9d655be11a*/
		}

		public void DebuggerHiddenMethod ()
		{
			var testClass = new TestClass ();
			testClass.DebuggerHiddenMethod (true);/*b0abae8d-fbd0-4bde-b586-bb511b954d8a*/
			testClass.DebuggerHiddenMethod (true, 3);
			testClass.DebuggerHiddenMethod (false);
		}

		public void DebuggerNonUserCodeMethod ()
		{
			var testClass = new TestClass ();
			testClass.DebuggerNonUserCodeMethod (true);/*02757896-0e76-40b8-8235-d09d2110da78*/
			testClass.DebuggerNonUserCodeMethod (true, 3);
			testClass.DebuggerNonUserCodeMethod (false);
		}

		public void DebuggerStepperBoundaryMethod ()
		{
			var testClass = new TestClass ();
			testClass.DebuggerStepperBoundaryMethod (true);/*0b7eef17-af79-4b34-b4fc-cede110f20fe*/
			var a = testClass;
			var b = testClass;/*806c13f8-8a59-4ae0-83a2-33191368af47*/
			testClass.DebuggerStepperBoundaryMethod (false);/*d105feb1-2cc1-49a5-a01e-f199c29ca7b7*/
			a = testClass;
			b = testClass;/*f86fa865-ed31-4c9f-8280-a54c3f06ee29*/
		}

		public void DebuggerStepperBoundaryMethod2 ()
		{
			var a = new TestStepperBoundary ();
			a.Test1 ();/*f3a22b38-596a-4463-a562-20b342fdec12*/
			a.Test3 ();
			a.Test3 ();/*4721f27a-a268-4529-b327-c39f208c08c5*/
		}

		[DebuggerNonUserCode]
		class TestStepperBoundary
		{
			public void Test1 ()
			{
				Test2 ();/*d110546f-a622-4ec3-9564-1c51bfec28f9*/
			}

			[DebuggerStepperBoundary]
			public void Test2 ()
			{
				Test3 ();
			}

			public void Test3 ()
			{
			}
		}

		public void DebuggerStepThroughMethod ()
		{
			var testClass = new TestClass ();
			testClass.DebuggerStepThroughMethod (true);/*707ccd6c-3464-4700-8487-a83c948aa0c3*/
			testClass.DebuggerStepThroughMethod (true, 3);
			testClass.DebuggerStepThroughMethod (false);
		}

		public void BreakpointInsideDelegate ()
		{
			var action = new Action (delegate {
				int i = 0;/*ffde3c82-4310-43d3-93d1-4c39e9cf615e*/
			});
			action ();/*f3b6862d-732b-4f68-81f5-f362d5a092e2*/
		}

		public static void BreakpointInsideOneLineDelegateNoDisplayClass ()
		{
			var button = new Button ();
			button.Clicked += (sender, e) => Console.WriteLine (); /*e72a2fa6-2d95-4f96-b3d0-ba321da3cb55*/
			button.MakeClick ();/*e0a96c37-577f-43e3-9a20-2cdd8bf7824e*/
		}

		public void BreakpointInsideOneLineDelegate ()
		{
			var button = new Button ();
			int numClicks = 0;
			button.Clicked += (object sender, EventArgs e) => button.SetTitle (String.Format ("clicked {0} times", numClicks++));/*22af08d6-dafc-47f1-b8d1-bee1526840fd*/
			button.MakeClick ();/*67ae4cce-22b3-49d8-8221-7e5b26a5e79b*/
		}

		public void BreakpointInsideOneLineDelegateAsync ()
		{
			var button = new Button ();
			int numClicks = 0;
			button.Clicked += async (sender, e) => button.SetTitle (String.Format ("clicked {0} times", numClicks++));/*b6a65e9e-5db2-4850-969a-b3747b2459af*/
			button.MakeClick ();
		}

		public class Button
		{
			public event EventHandler Clicked;

			public void MakeClick ()
			{
				Clicked (null, EventArgs.Empty);/*3be64647-76c1-455b-a4a7-a21b37383dcb*/
			}

			public void SetTitle (string message)
			{

			}
		}

		public void ForeachEnumerable ()
		{
			var testClass = new TestClass ();/*b73bec88-2c43-4157-8574-ad517730bc74*/
			foreach (var a in testClass.Iter_1()) {
				/*69dba3ab-0941-47e9-99fa-10222a2e894d*/
			}
			/*e01a5428-b067-4ca3-ac8c-a19d5d800228*/
		}

		public void SimpleConstrutor ()
		{
			var obj = new EmptyClassWithConstructor ();/*d62ff7ab-02fa-4205-a432-b4569709eab6*/
		}

		public void NoConstructor ()
		{
			var obj = new EmptyClassWithoutConstructor ();/*84fc04b2-ede2-4d8b-acc4-28441e1c5f55*/
		}

		static async Task<string> AsyncBug13401 ()
		{
			return "Hello from Bar";
		}

		public static async Task Bug13401 ()
		{
			string s = await AsyncBug13401 ();
			Console.Write ("");/*977ee8ce-ee61-4de0-9fc1-138fa164870b*/
		}

		public PListScheme PListSchemeTest ()
		{
			string value = "<xml></xml>";
			using (var reader = System.Xml.XmlReader.Create (new StringReader (value)))
				return PListScheme.Load (reader);/*41eb3a30-3b19-4ea5-a7dc-e4c76871f391*/
		}

		public class Key
		{
		}

		public partial class PListScheme
		{
			public static readonly PListScheme Empty = new PListScheme () { keys = new Key [0] };

			IList<Key> keys = new List<Key> ();

			public IList<Key> Keys {
				get {
					return keys;
				}
			}

			public static PListScheme Load (System.Xml.XmlReader reader)
			{
				/*c9b18785-1348-42e3-a479-9cac1e7c5360*/
				var result = new PListScheme ();
				var doc = new System.Xml.XmlDocument ();
				doc.Load (reader);
				return result;
			}
		}

		public void Bug4433Test ()
		{
			Bug4433.Method ();
		}

		public class Bug4433
		{
			void Test ()
			{
				return;/*ad9b8803-eef0-438c-bf2b-9156782f4027*/
			}

			static Bug4433 Instance { get; set; }

			public static void Method ()
			{
				Instance = new Bug4433 ();
				Instance.Test ();/*a062e69c-e3f7-4fd7-8985-fc7abd5c27d2*/
			}
		}

		public void EmptyForLoopTest ()
		{
			Thread t = new Thread (new ThreadStart (delegate {
				try {
					EmptyForLoop ();
				} catch {
				}
			}));
			t.Start ();
			Thread.Sleep (1000);//This migth need to be increased
			t.Abort ();
			t.Join ();
		}

		private void EmptyForLoop ()
		{
			/*946d5781-a162-4cd9-a7b6-c320564cc594*/
			for (; ;) {
				/*a2ff92da-3796-47e3-886a-4bd786a07547*/
			}
		}

		public void ForLoop10 ()
		{
			/*c35046f7-e87d-4b8f-b260-43e181a0a07c*/
			for (int i = 0; i < 10; i++) {
				Console.Write ("");/*eef5bea2-aaa6-4718-b26f-b35be6a6a13e*/
			}
			var a = 0;/*3e2e4759-f6d9-4839-98e6-4fa96b227458*/
			var b = 1;
			var c = a + b;
			Console.Write (c);
		}

		public void CallMethodWithPropertyAsArgument ()
		{
			var obj = new TestClass ();
			obj.CallMethodWithPropertyAsArgument ();
		}

		public void TestBug7901 ()
		{
			var obj = new TestClass ();
			obj.Bug7901 ();
		}

		public void TestBug10782 ()
		{
			DoStuff (new EmptyClassWithConstructor ());/*cdcabe93-4f55-4dbb-821e-912097c4f727*/
		}

		private void DoStuff (EmptyClassWithConstructor asdf)
		{
			string bar = asdf.ToString ();/*3bda6643-6d06-4504-a4da-91bc8c5eb887*/
		}

		public async void TestAwaitCall ()
		{
			int a = 0;
			await Task.Delay (100);/*a221c9d4-6d00-4fce-99e6-d712e9a23c02*/
			int b = 0;
		}

		public async void StepInsideAwaitTaskRun ()
		{
			int a = 0;
			await Task.Run (() => EmptyMethod ());/*a221c9d4-6d00-4fce-99e6-d712e9a23c02*/
			int b = 0;
		}

		private void EmptyMethod ()
		{
			/*3c27f60f-fdfa-44c0-b58f-552ecaaa77f1*/
		}

		public void ConitionalBreakpointEnum ()
		{
			SomeMethod (BooleanEnum.True);
			SomeMethod (BooleanEnum.False);
		}

		private void SomeMethod (BooleanEnum en)
		{
			int i = 0;/*ecf764bf-9182-48d6-adb0-0ba36e2653a7*/
		}

		public void ConditionalBreakpointString ()
		{
			SomeMethod ("aaa");
			SomeMethod ("bbb");
			SomeMethod ("ccc");
		}

		private void SomeMethod (string str)
		{
			int i = 0;/*033dd01d-6cb4-4e1a-b445-de6d7fa0d2a7*/
		}

		public void Catchpoint1 ()
		{
			try {
				throw new NotImplementedException ();/*526795d3-ee9e-44a7-8423-df0b406e9e8d*/
			} catch {
			}
			var a = 0;/*fcdc2412-c00e-4c95-b2ea-e3cf5d5bf856*/
		}

		public void Catchpoint2 ()
		{
			try {
				//If you wonder why I didn't use just simple File.Open("unexistingFile.txt") is
				//that FrameStack inside Mono and .Net are different and same goes for 10+ other calls I tried...
				new Socket (AddressFamily.InterNetwork, SocketType.Unknown, ProtocolType.Ggp);/*d24b1c9d-3944-4f0d-be31-5556251fbdf5*/
			} catch {

			}
		}

		/// <summary>
		/// Bug 9615
		/// </summary>
		public void CatchpointIgnoreExceptionsInNonUserCode ()
		{
			NonUserCodeClass.ThrowDelayedHandledException ();
			Thread.Sleep (200);
			var a = 0;/*999b8a83-8c32-4640-a8e1-f74309cda79c*/
		}

		public void SimpleMethod ()
		{
			/*f4e3a214-229e-44dd-9da2-db82ddfbec11*/
			int a = 1;
			int b = 2;
			int c = a + b;
			Console.Write (c);
		}

		public void Bug13640 ()
		{
			var l = new List<int> ();/*b64e6497-e976-4125-9741-801909e5eeb1*/
			foreach (var x in l)
				foreach (var y in l) // XS hits this line if it should not
					Console.Write (y);
			return;/*a90ba766-0891-4837-9b1d-e5458f6b8e07*/
		}

		class EmptyClassWithConstructor
		{
			/*1f37aea1-77a1-40c1-9ea5-797db48a14f9*/
			public EmptyClassWithConstructor ()
			{
				int i = 0;/*494fddfb-85f1-4ad0-b5b3-9b2f990bb6d0*/
			}
		}

		class EmptyClassWithoutConstructor
		{
		}

		class DontUseThisClassInOtherTests
		{
			//Or StaticConstructorStepping will fail because
			//static constructor could be invoked in other test
			static DontUseThisClassInOtherTests ()
			{
				int a = 1;/*6c42f31b-ca4f-4963-bca1-7d7c163087f1*/
				int b = 2;/*7e6862cd-bf31-486c-94fe-19933ae46094*/
			}
		}

		public class TestClass
		{
			private string oneLineProperty = "";

			public string OneLineProperty {
				get{ return oneLineProperty; }/*3722cad3-7da1-4c86-a398-bb2cf6cc65a9*/
				set{ oneLineProperty = value; }
			}

			private string multiLineProperty = "";

			public string MultiLineProperty {
				get {
					var b = multiLineProperty;/*e0082b9a-26d7-4279-8749-31cd13866abf*/
					return multiLineProperty;/*04f1ce38-121a-4ce7-b4ba-14fb3f6184a2*/
				}
				set {
					multiLineProperty = value;
				}
			}

			/// <summary>
			/// This is used only for test so don't use for compering
			/// </summary>
			public static bool operator == (TestClass a, TestClass b)
			{/*5a3eb8d5-88f5-49c0-913f-65018e5a1c5c*/
				return a.oneLineProperty == b.oneLineProperty &&
				a.multiLineProperty == b.multiLineProperty;
			}

			/// <summary>
			/// This is used only for test so don't use for compering
			/// </summary>
			public static bool operator != (TestClass a, TestClass b)
			{
				return !(a == b);
			}

			public object TestMethod (object obj)
			{/*c5361deb-aff5-468f-9293-0d2e50fc62fd*/
				if (obj == null)/*10e0f5c7-4c77-4897-8324-deef9aae0192*/
					return null;/*40f0acc2-2de2-44c8-8e18-3867151ba8da*/
				return null;/*ae71a41d-0c90-433d-b925-0b236b8119a9*/
				/*3c0316e9-eace-48e8-b9ed-03a8c6306c66*/
			}

			public void EmptyTestMethod ()
			{
				/*49326780-f51b-4510-a52c-03e7af442dda*/
			}

			[System.Diagnostics.DebuggerHidden]
			public void DebuggerHiddenMethod (bool callEmptyMethod, int resursive = 0)
			{
				if (resursive > 0) {
					Console.Write ("");
					DebuggerHiddenMethod (callEmptyMethod, resursive - 1);
					Console.Write ("");
				}
				Console.Write ("");
				if (callEmptyMethod)
					EmptyTestMethod ();
				Console.Write ("");
			}

			[System.Diagnostics.DebuggerNonUserCode]
			public void DebuggerNonUserCodeMethod (bool callEmptyMethod, int resursive = 0)
			{
				/*5b9b96b6-ce24-413f-8660-715fccfc412f*/
				if (resursive > 0) {
					Console.Write ("");
					DebuggerNonUserCodeMethod (callEmptyMethod, resursive - 1);/*6b2c05cd-1cb8-48fe-b6bf-c4949121d4c7*/
					Console.Write ("");
				}
				Console.Write ("");
				if (callEmptyMethod)
					EmptyTestMethod ();/*754272b8-a14b-4de0-9075-6a911c37e6ce*/
				Console.Write ("");
			}

			[System.Diagnostics.DebuggerStepperBoundary]
			public void DebuggerStepperBoundaryMethod (bool callEmptyMethod, int resursive = 0)
			{
				if (resursive > 0) {
					Console.Write ("");
					DebuggerStepThroughMethod (callEmptyMethod, resursive - 1);
					Console.Write ("");
				}
				Console.Write ("");
				if (callEmptyMethod)
					EmptyTestMethod ();
				Console.Write ("");
			}

			[System.Diagnostics.DebuggerStepThrough]
			public void DebuggerStepThroughMethod (bool callEmptyMethod, int resursive = 0)
			{
				if (resursive > 0) {
					Console.Write ("");
					DebuggerStepThroughMethod (callEmptyMethod, resursive - 1);
					Console.Write ("");
				}
				Console.Write ("");
				if (callEmptyMethod)
					EmptyTestMethod ();
				Console.Write ("");
			}

			public IEnumerable<int> Iter_1 ()
			{
				yield return 1;/*1463a77d-f27e-4bcd-8f92-89a682faa1c7*/
				yield return 2;
			}

			public void CallMethodWithPropertyAsArgument ()
			{
				ConsoleWriteline ("hello");/*1c3e65ca-3201-42ba-9c6e-6f9a45ddac44*/
				ConsoleWriteline (OneLineProperty);
			}

			private void ConsoleWriteline (string arg)
			{
				Console.WriteLine (arg);/*c25be44e-ead3-4891-ab42-0e4cf8450f7a*/
			}

			private class ScrollView
			{
				public string VisbleContentRect{ get; set; }

				public string ZoomScale{ get; set; }
			}

			private ScrollView myScrollView = new ScrollView ();

			string curRect;

			string curZoom;

			bool EventsNeedRefresh {
				get;
				set;
			}

			DateTime CurrentDate {
				get;
				set;
			}

			DateTime FirstDayOfWeek {
				get;
				set;
			}

			public void Bug7901 ()
			{
				SetDayOfWeek (DateTime.UtcNow);/*956bd9fd-39fe-4587-9d9e-a2a817d76286*/
			}

			public void SetDayOfWeek (DateTime date)
			{
				/*f456a9b0-9c1a-4b34-bef4-d80b8541ebdb*/
				if (myScrollView != null) {
					curRect = myScrollView.VisbleContentRect;/*11259de1-944d-4052-b970-62662e21876a*/
					curZoom = myScrollView.ZoomScale;
				}
				EventsNeedRefresh = true;/*4863ebb7-8c90-4704-af8b-66a9f53657b9*/
				CurrentDate = date;
				FirstDayOfWeek = date.AddDays (-1 * (int)date.DayOfWeek);
			}
		}

		public void TestBug25358 ()
		{
			try {
				throw new Exception ("2b2c4423-accf-4c2c-af31-7d8dcee31c32");
			} catch (IOException e) {
				Console.WriteLine (e);
			} catch (Exception e) {
				Console.WriteLine (e);/*4b30f826-2ba0-4b53-ab36-85b2cdde1069*/
			}
		}

		public void TestBug21410 ()
		{
			Bug21410.Test ();
		}

		class Bug21410
		{

			interface willy
			{
			}

			class snarf
			{
			}

			class flap : snarf, willy
			{
			}

			class point
			{
				public string Acme = "";
				public snarf Lst = new flap ();
			}

			class zork
			{
				public point Point = new point ();
			}

			class narf
			{
				public zork Zork = new zork ();
			}

			static narf _narf = new narf ();

			public static void Test ()
			{
				doStuff (_narf.Zork.Point.Acme, _narf.Zork.Point.Acme, (willy)_narf.Zork.Point.Lst, _narf.Zork.Point.Acme, (willy)_narf.Zork.Point.Lst);/*5e6663d0-9088-40ad-914d-0fcc05b2d0d5*/
				doStuff (_narf.Zork.Point.Acme, _narf.Zork.Point.Acme, (willy)_narf.Zork.Point.Lst, _narf.Zork.Point.Acme, (willy)_narf.Zork.Point.Lst);
			}

			static void doStuff (string str, string str2, willy lst, string str3,
			                     willy lst2)
			{
			}
		}
	}
}

public enum BooleanEnum
{
	False,
	True
}
/*invalidBreakpointAtEndOfFile*/