//
// InstrumentationTests.cs
//
// Author:
//       lluis <>
//
// Copyright (c) 2018 
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
using MonoDevelop.Core.Instrumentation;
using NUnit.Framework;
using System.Threading;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class InstrumentationTests
	{
		static List<CounterValue> counterValues = new List<CounterValue> ();
		TestInstrumentationConsumer testConsumer;

		class CustomCounterMetadata: CounterMetadata
		{
			public int SomeMeasure {
				get => GetProperty<int> ();
				set => SetProperty (value);
			}
		}

		class TestInstrumentationConsumer : InstrumentationConsumer
		{
			public TestInstrumentationConsumer ()
			{
			}

			public override bool SupportsCounter (Counter counter)
			{
				return counter.Name == "TestCounter";
			}

			public override IDisposable BeginTimer (TimerCounter counter, CounterValue value)
			{
				return new TimerTrackerTest { Value = value };
			}

			public override void ConsumeValue (Counter counter, CounterValue value)
			{
				counterValues.Add (value);
			}
		}

		class TimerTrackerTest: IDisposable
		{
			public CounterValue Value;

			public void Dispose ()
			{
				counterValues.Add (Value);
			}
		}

		[SetUp]
		public void Setup ()
		{
			counterValues.Clear ();
			testConsumer = new TestInstrumentationConsumer ();
			InstrumentationService.RegisterInstrumentationConsumer (testConsumer);
		}

		[TearDown]
		public void TearDown ()
		{
			InstrumentationService.UnregisterInstrumentationConsumer (testConsumer);
			testConsumer = null;
		}

		[Test]
		public void TimerCounterWithMetadata ()
		{
			var timer = InstrumentationService.CreateTimerCounter<CustomCounterMetadata> ("TestCounter");
			var t = timer.BeginTiming ();
			t.Metadata.SomeMeasure = 4;
			t.Dispose ();
			Assert.AreEqual (1, counterValues.Count);
			var value = counterValues [0];

			Assert.IsNotNull (value.Metadata);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CustomCounterMetadata.SomeMeasure), out var measure));
			Assert.AreEqual (4, measure);
			Assert.IsFalse (value.Metadata.TryGetValue (nameof (CounterMetadata.Result), out var result));
		}

		[Test]
		public void TimerCounterWithNoMetadata ()
		{
			var timer = InstrumentationService.CreateTimerCounter<CustomCounterMetadata> ("TestCounter");
			var t = timer.BeginTiming ();
			t.Dispose ();
			Assert.AreEqual (1, counterValues.Count);
			var value = counterValues [0];

			Assert.IsNull (value.Metadata);
		}

		[Test]
		public void TimerCounterWithCustomMetadata ()
		{
			var timer = InstrumentationService.CreateTimerCounter<CustomCounterMetadata> ("TestCounter");
			var md = new CustomCounterMetadata {
				SomeMeasure = 5,
			};
			md.SetSuccess ();
			var t = timer.BeginTiming (md);
			t.Dispose ();
			Assert.AreEqual (1, counterValues.Count);
			var value = counterValues [0];

			Assert.IsNotNull (value.Metadata);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CustomCounterMetadata.SomeMeasure), out var measure));
			Assert.AreEqual (5, measure);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CounterMetadata.Result), out var result));
			Assert.AreEqual ("Success", result);
		}

		[Test]
		[TestCase (CounterResult.Failure)]
		[TestCase (CounterResult.Success)]
		[TestCase (CounterResult.UserCancel)]
		[TestCase (CounterResult.UserFault)]
		public void TimerCounterWithFailure (CounterResult testResult)
		{
			counterValues.Clear ();
			var timer = InstrumentationService.CreateTimerCounter<CustomCounterMetadata> ("TestCounter");
			var t = timer.BeginTiming ();
			t.Metadata.Result = testResult;
			t.Dispose ();
			Assert.AreEqual (1, counterValues.Count);
			var value = counterValues [0];

			Assert.IsNotNull (value.Metadata);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CounterMetadata.Result), out var result));
			Assert.AreEqual (testResult.ToString(), result);
		}

		[Test]
		public void TimerCounterCancel ()
		{
			var timer = InstrumentationService.CreateTimerCounter<CustomCounterMetadata> ("TestCounter");
			var cs = new CancellationTokenSource ();
			var t = timer.BeginTiming (cs.Token);
			t.Metadata.SomeMeasure = 4;
			cs.Cancel ();
			t.Dispose ();
			Assert.AreEqual (1, counterValues.Count);
			var value = counterValues [0];

			Assert.IsNotNull (value.Metadata);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CustomCounterMetadata.SomeMeasure), out var measure));
			Assert.AreEqual (4, measure);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CounterMetadata.Result), out var result));
			Assert.AreEqual ("UserCancel", result);
		}

		[Test]
		public void CounterWithMetadata ()
		{
			var counter = InstrumentationService.CreateCounter<CustomCounterMetadata> ("TestCounter");

			counter.Inc (new CustomCounterMetadata {
				SomeMeasure = 5,
				Result = CounterResult.Success
			});

			Assert.AreEqual (1, counterValues.Count);
			var value = counterValues [0];

			Assert.IsNotNull (value.Metadata);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CustomCounterMetadata.SomeMeasure), out var measure));
			Assert.AreEqual (5, measure);
			Assert.IsTrue (value.Metadata.TryGetValue (nameof (CounterMetadata.Result), out var result));
			Assert.AreEqual ("Success", result);
		}

		[Test]
		public void CounterWithNoMetadata ()
		{
			var counter = InstrumentationService.CreateCounter<CustomCounterMetadata> ("TestCounter");

			counter.Inc ();

			Assert.AreEqual (1, counterValues.Count);
			var value = counterValues [0];

			Assert.IsNull (value.Metadata);
		}

		[Test]
		public void CounterMetadataQueryingDoesNotCrash ()
		{
			var metadata = new CustomCounterMetadata ();

			Assert.AreEqual (default (int), metadata.SomeMeasure);
		}
	}
}
