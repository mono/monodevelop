//
// RoamingMonoDevelopProfileOptionPersisterTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using System.Collections.Immutable;
using System.Threading;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	[TestFixture]
	public class MonoDevelopOptionPersisterTests : OptionsTestBase
	{
		public abstract class SerializationTestCase : IOption
		{
			public readonly object Value;
			public readonly object SerializedValue;
			public readonly bool Success;

			SerializationTestCase (object value, object serializedValue, bool success, Type type)
			{
				Value = value;
				SerializedValue = serializedValue;
				Success = success;
				Type = type;
			}

			internal abstract OptionKey Wrap (RoslynPreferences preferences);

			// Mostly pass through.
			public static SerializationTestCase Ok<T> (T value) => new SerializationTestCaseT<T> (value, value, true, typeof (T));

			// Serialized value is transformed.
			public static SerializationTestCase Ok<T> (T value, object serializedValue) => new SerializationTestCaseT<T> (value, serializedValue, true, typeof (T));

			public static SerializationTestCase Fail<T> (T value, object serializedValue) => new SerializationTestCaseT<T> (value, serializedValue, false, typeof (T));

			// It fails, whatever happens.
			public static SerializationTestCase Fail<T> (object serializedValue) => new SerializationTestCaseT<T> (null, serializedValue, false, typeof (T));

			public string Feature => "feature";
			public string Name => "name";
			public Type Type { get; }
			public object DefaultValue => Type.IsValueType ? Activator.CreateInstance (Type) : null;
			public bool IsPerLanguage => false;
			public ImmutableArray<OptionStorageLocation> StorageLocations { get; } = ImmutableArray.Create<OptionStorageLocation> (new RoamingProfileStorageLocation ("feature.C#"));

			class SerializationTestCaseT<T> : SerializationTestCase
			{
				public SerializationTestCaseT (object value, object serializedValue, bool success, Type type) : base (value, serializedValue, success, type)
				{
				}

				internal override OptionKey Wrap (RoslynPreferences preferences)
				{
					var optionKey = new OptionKey (this);
					preferences.Wrap<T> (optionKey);
					return optionKey;
				}
			}
		}

		class MyCustomClass { public int X; }

		static CodeStyleOption<bool> boolOption =
			new CodeStyleOption<bool> (true, NotificationOption.Suggestion);
		static CodeStyleOption<ExpressionBodyPreference> enumOption =
			new CodeStyleOption<ExpressionBodyPreference> (ExpressionBodyPreference.WhenOnSingleLine, NotificationOption.Suggestion);
		static CodeStyleOption<string> stringOption =
			new CodeStyleOption<string> ("testString", NotificationOption.Error);
		static CodeStyleOption<MyCustomClass> classOption =
			new CodeStyleOption<MyCustomClass> (new MyCustomClass { X = 1 }, NotificationOption.Error);
		static NamingStylePreferences namingOption = NamingStylePreferences.Default;

		SerializationTestCase [] SerializationTestCases = {
			// Passing test cases
			SerializationTestCase.Ok (true),
			SerializationTestCase.Ok (PlatformID.Unix),
			SerializationTestCase.Ok (boolOption, boolOption.ToXElement ().ToString ()),
			SerializationTestCase.Ok (enumOption, enumOption.ToXElement ().ToString ()),
			SerializationTestCase.Ok (stringOption, stringOption.ToXElement ().ToString ()),
			SerializationTestCase.Ok (namingOption, namingOption.CreateXElement ().ToString ()),
			SerializationTestCase.Ok ("string"),
			SerializationTestCase.Fail (classOption, stringOption.ToXElement ().ToString ()),
		};

		[TestCaseSource (nameof (SerializationTestCases))]
		public void TestSerialization (SerializationTestCase testCase)
		{
			var (preferences, persister) = Setup ();

			using (persister) {
				var optionKey = testCase.Wrap (preferences);

				var property = optionKey.GetPropertyName ();
				PropertyService.Set (property, null);

				// Try persisting it.
				Assert.AreEqual (testCase.Success, persister.TryPersist (optionKey, testCase.Value));
				if (!testCase.Success) {
					Assert.IsFalse (PropertyService.HasValue (property));
					return;
				}

				// See if we can deserialize it back as-is
				Assert.AreEqual (testCase.SerializedValue, PropertyService.Get<object> (property));
				Assert.AreEqual (testCase.Success, persister.TryFetch (optionKey, out var deserializedValue));
				Assert.AreEqual (testCase.Value, deserializedValue);
			}
		}

		SerializationTestCase [] DeserializationTestCases = {
			// Simple types
			SerializationTestCase.Ok(true, "True"),
			SerializationTestCase.Ok(false, "False"),
			SerializationTestCase.Ok<bool?>(true, "True"),
			SerializationTestCase.Ok<bool?>(false, "False"),
			SerializationTestCase.Ok<bool?>(null, null),
			SerializationTestCase.Ok<string>("test", "test"),
			SerializationTestCase.Ok(1, "1"),
			SerializationTestCase.Ok(1L, "1"),
			SerializationTestCase.Ok(1U, "1"),
			SerializationTestCase.Ok(1D, "1"),
			SerializationTestCase.Ok(1F, "1"),
			SerializationTestCase.Ok(ExpressionBodyPreference.Never, "Never"),
			SerializationTestCase.Ok<int?>(1, "1"),
			SerializationTestCase.Fail<bool>(0),
			SerializationTestCase.Fail<bool>(0L),
			SerializationTestCase.Fail<ExpressionBodyPreference> (1.0),
			SerializationTestCase.Fail<ExpressionBodyPreference> (ulong.MaxValue),
			SerializationTestCase.Ok(namingOption, namingOption.CreateXElement ().ToString()),
			SerializationTestCase.Ok(boolOption, boolOption.ToXElement ().ToString()),
			SerializationTestCase.Ok(enumOption, enumOption.ToXElement ().ToString()),
			SerializationTestCase.Fail<CodeStyleOption<MyCustomClass>>(stringOption.ToXElement ().ToString()),
		};

		[TestCaseSource (nameof (DeserializationTestCases))]
		public void TestDeserialization (SerializationTestCase testCase)
		{
			var (preferences, persister) = Setup ();

			using (persister) {
				var optionKey = testCase.Wrap (preferences);
				var property = optionKey.GetPropertyName ();

				// Set the value and deserialize it
				PropertyService.Set (property, testCase.SerializedValue);
				var success = persister.TryFetch (optionKey, out var deserialized);
				Assert.AreEqual (testCase.Success, success);
				Assert.AreEqual (testCase.Value, deserialized, $"Could not convert {testCase.SerializedValue} to {testCase.Value}");
			}
		}

		[TestCaseSource (nameof(allKinds))]
		public void TestFetchPersistString (StorageLocationKind kind)
		{
			TestFetchPersist (kind, "a", "b");
		}

		[TestCaseSource (nameof (allKinds))]
		public void TestFetchPersistCodeOption (StorageLocationKind kind)
		{
			var value1 = new CodeStyleOption<bool> (true, NotificationOption.Suggestion);
			var value2 = new CodeStyleOption<bool> (false, NotificationOption.Error);
			TestFetchPersist (kind, value1, value2, CodeStyleOption<bool>.Default);
		}

		[TestCaseSource (nameof (allKinds))]
		public void TestFetchPersistNamingStyle (StorageLocationKind kind)
		{
			var defaultValue = NamingStylePreferences.Default;
			var value1 = new NamingStylePreferences (
				defaultValue.SymbolSpecifications,
				defaultValue.NamingStyles.WhereAsArray (x => x.Prefix == "I"),
				defaultValue.NamingRules
			);
			var value2 = new NamingStylePreferences (
				defaultValue.SymbolSpecifications,
				defaultValue.NamingStyles.WhereAsArray (x => x.Prefix != "I"),
				defaultValue.NamingRules
			);

			TestFetchPersist (kind, value1, value2, defaultValue);
		}

		void TestFetchPersist<T> (StorageLocationKind kind, T value1, T value2, T defaultValue = default(T))
		{
			var (preferences, persister) = Setup ();
			bool shouldPersist = kind != StorageLocationKind.None;

			using (persister) {
				foreach (var optionKey in GetOptionKeys (kind, defaultValue)) {
					string propertyName = optionKey.GetPropertyName ();
					shouldPersist = propertyName != null;
					ConfigurationProperty<T> wrap = null;
					if (propertyName != null)
						wrap = preferences.Wrap<T> (optionKey);

					// Fetch, no items.
					AssertFetch (optionKey, optionKey.Option.DefaultValue);

					// Persist with no item set
					AssertPersist (optionKey, value1, wrap);
					if (!shouldPersist)
						continue;

					// Fetch after persist
					AssertFetch (optionKey, value1);

					// Check if setting config property persists.
					wrap.Value = value2;
					AssertFetch (optionKey, value2);

					// Set null.
					AssertPersist (optionKey, null, wrap);
					AssertFetch (optionKey, defaultValue);
				}
			}

			void AssertFetch (OptionKey optionKey, object expectedValue)
			{
				// Check that we can grab the value and it is grabbed properly
				Assert.AreEqual (shouldPersist, persister.TryFetch (optionKey, out var value));
				Assert.AreEqual (shouldPersist ? expectedValue : null, value);
			}

			void AssertPersist (OptionKey optionKey, object value, ConfigurationProperty<T> wrap)
			{
				// Check that it either got persisted, on an option with a roaming profile
				// or that it got discarded
				Assert.AreEqual (shouldPersist, persister.TryPersist (optionKey, value));
				if (shouldPersist)
					Assert.AreEqual (value, wrap.Value);
			}
		}

		[Test]
		public void TestPropertiesModification ()
		{
			var (preferences, persister, optionService) = SetupOptions ();

			using (persister) {
				int optionChangedCount = 0;
				optionService.OptionChanged += (o, e) => optionChangedCount++;

				var option = GetOption (defaultValue: "c");
				var propName = new OptionKey (option).GetPropertyName ();
				PropertyService.Set (propName, "a");

				Assert.AreEqual (0, optionChangedCount);

				// Notifications happen after the option is first queried.
				Assert.AreEqual ("a", optionService.GetOption (option));
				bool changed = false;
				optionService.OptionChanged += (o, e) => {
					Assert.AreEqual ("b", e.Value);
					Assert.AreEqual (option, e.Option);
					changed = true;
				};

				PropertyService.Set (propName, "b");
				Assert.AreEqual (true, changed);
			}
		}

		[Test]
		public void TestRoslynPropertyWrapAndMonitor ()
		{
			var (preferences, persister, optionService) = SetupOptions ();

			using (persister) {
				foreach (var option in GetOptionKeys<string> ()) {
					var prop = preferences.Wrap (option, default (string));

					// Initial values
					Assert.AreEqual (null, prop.Value);
					Assert.AreEqual (false, optionService.GetOptionInternal (option, out var tempValue));
					Assert.AreEqual (null, tempValue);

					// Check without monitor.
					prop.Value = "a";
					Assert.AreEqual ("a", prop.Value);
					Assert.AreEqual (false, optionService.GetOptionInternal (option, out tempValue));
					Assert.AreEqual (null, tempValue);

					// Check when monitoring.
					persister.MonitorChanges (option.GetPropertyName (), option);
					prop.Value = "b";
					Assert.AreEqual ("b", prop.Value);
					Assert.AreEqual ("b", optionService.GetOption (option));

					// Check option changes reflect in property
					optionService.SetOptions (optionService.GetOptions ().WithChangedOption (option, "c"));
					Assert.AreEqual ("c", prop.Value);
					Assert.AreEqual ("c", optionService.GetOption (option));
				}
			}
		}
	}
}
