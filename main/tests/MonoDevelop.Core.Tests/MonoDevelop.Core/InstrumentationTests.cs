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
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

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

		[Test]
		public void SerializeCounters ()
		{
			InstrumentationService.Enabled = true;
			var timer = InstrumentationService.CreateCounter<CustomCounterMetadata> ("TestCounter", "IDEGroup", id: "IDE.TestCounter");
			timer.Inc (1, "First Trace", new CustomCounterMetadata () { SomeMeasure = 1 });
			timer.Inc (1, "Second Trace", new CustomCounterMetadata () { SomeMeasure = 2 });
			timer.Inc (1, "Third Trace", new CustomCounterMetadata () { SomeMeasure = 3 });

			InstrumentationService.SaveJson ("serialize_counters.json");
			using (var textReader = new StreamReader("serialize_counters.json")) {
				using (var jsonTextReader = new JsonTextReader (textReader)) {
					var jsonRootObj = JObject.Load (jsonTextReader);

					var startTimeToken = jsonRootObj ["StartTime"];
					Assert.IsNotNull (startTimeToken);
					Assert.That (startTimeToken.ToString (), Is.EqualTo (InstrumentationService.StartTime.ToString()));
					Assert.IsNotNull (jsonRootObj ["EndTime"]);

					var counters = jsonRootObj ["Counters"];
					Assert.IsNotNull (counters);
					var testCounter = counters ["TestCounter"];
					var actualTestCounter = InstrumentationService.GetCounter ("TestCounter");
					Assert.IsNotNull (actualTestCounter);

					var storeValuesToken = testCounter ["StoreValues"];
					Assert.That (bool.Parse(storeValuesToken.ToString ()), Is.EqualTo (actualTestCounter.StoreValues));

					var totalCountToken = testCounter ["TotalCount"];
					Assert.IsNotNull (totalCountToken);
					Assert.That (int.Parse (totalCountToken.ToString ()), Is.EqualTo (actualTestCounter.TotalCount));

					var nameToken = testCounter ["Name"];
					Assert.IsNotNull (nameToken);
					Assert.That (nameToken.ToString(), Is.EqualTo (actualTestCounter.Name));

					var idToken = testCounter ["Id"];
					Assert.IsNotNull (idToken);
					Assert.That (idToken.ToString (), Is.EqualTo (actualTestCounter.Id));

					var countToken = testCounter ["Count"];
					Assert.IsNotNull (countToken);
					Assert.That (int.Parse (countToken.ToString ()), Is.EqualTo (actualTestCounter.Count));

					var enabledToken = testCounter ["Enabled"];
					Assert.IsNotNull (enabledToken);
					Assert.That (bool.Parse (enabledToken.ToString ()), Is.EqualTo (actualTestCounter.Enabled));

					Assert.IsNotNull (testCounter ["Category"]);
					Assert.IsNotNull (testCounter ["AllValues"]);

					foreach (var val in testCounter ["AllValues"]) {
						var timeStamp = DateTime.Parse (val ["TimeStamp"].ToString());
						var message = val ["Message"].ToString ();
						var metaData = (int)val ["Metadata"] ["SomeMeasure"];
						Assert.That (actualTestCounter.AllValues
							.FirstOrDefault (x => x.Message == message && (int)x.Metadata["SomeMeasure"] == metaData && x.TimeStamp == timeStamp), Is.Not.Null);
					}
				}
			}
		}
	}
}
