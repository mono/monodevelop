//
// RoslynPreferences.Bound.cs
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
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	partial class RoslynPreferences
	{
		// Holds the mapping of key -> property updater
		readonly Dictionary<string, Action<object>> wrapMap = new Dictionary<string, Action<object>> ();

		internal bool TryGet (OptionKey key, out string propertyKey, out object value)
		{
			propertyKey = null;
			value = null;

			var type = key.Option.Type;
			var defaultValue = key.Option.DefaultValue;
			if (TryGetSerializationMethods<object> (type, out var serializer, out var deserializer)) {
				defaultValue = serializer (defaultValue);
				type = typeof (object);
			}

			// Check for roaming/profile properties
			foreach (var keyToCheck in key.GetPropertyNames ()) {
				propertyKey = propertyKey ?? keyToCheck;
				value = value ?? defaultValue;
				if (keyToCheck == null)
					continue;

				if (PropertyService.HasValue (keyToCheck)) {
					value = PropertyService.GlobalInstance.Get (keyToCheck, defaultValue, type);
					return true;
				}
			}

			return propertyKey != null;
		}

		internal bool TryGetUpdater (OptionKey key, out string propertyKey, out Action<object> updater) {
			propertyKey = key.GetPropertyName ();
			if (propertyKey != null)
				return wrapMap.TryGetValue (propertyKey, out updater);

			updater = null;
			return false;
		}

		/// <summary>
		/// Wrap the specified <see cref="IOption"/> using its default value, optionally migrating an old <see cref="ConfigurationProperty{T}"/>.
		/// This is a two-way binding of a property, both update sources (roslyn, IDE) update the other.
		/// The key for the property is computed by the underlying option's <see cref="OptionStorageLocation"/>s.
		/// </summary>
		/// <returns>The configuration property.</returns>
		/// <param name="optionKey">Roslyn option.</param>
		/// <param name="monodevelopPropertyName">The property name to migrate from.</param>
		/// <typeparam name="T">The property type</typeparam>
		public ConfigurationProperty<T> Wrap<T> (OptionKey optionKey, string monodevelopPropertyName = null)
			=> Wrap (optionKey, (T)optionKey.Option.DefaultValue, monodevelopPropertyName);

		/// <summary>
		/// Wrap the specified <see cref="IOption"/> using its default value, optionally migrating an old <see cref="ConfigurationProperty{T}"/>.
		/// This is a two-way binding of a property, both update sources (roslyn, IDE) update the other.
		/// The key for the property is computed by the underlying option's <see cref="OptionStorageLocation"/>s.
		/// </summary>
		/// <returns>The configuration property.</returns>
		/// <param name="optionKey">Roslyn option.</param>
		/// <param name="defaultValue">The overridden default value.</param>
		/// <param name="monodevelopPropertyName">The property name to migrate from.</param>
		/// <typeparam name="T">The property type</typeparam>
		public ConfigurationProperty<T> Wrap<T> (OptionKey optionKey, T defaultValue, string monodevelopPropertyName = null)
		{
			var name = optionKey.GetPropertyName ();

			ConfigurationProperty<T> result;

			// It is unfortunate, but roslyn has a special serialization mechanism, so use that.
			if (TryGetSerializationMethods<T> (optionKey.Option.Type, out var serializer, out var deserializer)) {
				var prop = new WrappedConfigurationProperty<T> (name, monodevelopPropertyName, defaultValue, serializer, deserializer);
				wrapMap.Add (name, value => prop.SetSerializedValue ((string)value));
				result = prop;
			} else {
				result = ConfigurationProperty.Create (name, defaultValue, monodevelopPropertyName);
				wrapMap.Add (name, value => result.Value = (T)value);
			}

			return result;
		}

		internal static bool TryGetSerializationMethods<T> (Type type, out Func<T, string> serializer, out Func<string, T> deserializer)
		{
			// It is unfortunate, but roslyn has a special serialization mechanism, so use that.
			if (IsOfGenericType (type, typeof (CodeStyleOption<>))) {
				var fromXElement = type.GetMethod ("FromXElement", BindingFlags.Public | BindingFlags.Static);

				serializer = value => ((ICodeStyleOption)value)?.ToXElement ().ToString ();
				deserializer = serializedValue => serializedValue != null ? (T)fromXElement.Invoke (null, new object [] { XElement.Parse (serializedValue) }) : default(T);
				return true;
			}

			if (typeof (NamingStylePreferences) == type) {
				serializer = value => ((NamingStylePreferences)(object)value)?.CreateXElement ().ToString ();
				deserializer = serializedValue => serializedValue != null ? (T)(object)NamingStylePreferences.FromXElement (XElement.Parse (serializedValue)) : default(T);
				return true;
			}

			serializer = null;
			deserializer = null;
			return false;
		}

		static bool IsOfGenericType (Type type, Type genericType)
		{
			while (type != null && type != typeof (object)) {
				var cur = type.IsGenericType ? type.GetGenericTypeDefinition () : type;
				if (genericType == cur)
					return true;

				type = type.BaseType;
			}
			return false;
		}

		class WrappedConfigurationProperty<T> : ConfigurationProperty<T>
		{
			readonly ConfigurationProperty<string> underlying;
			readonly Func<T, string> serializer;
			readonly Func<string, T> deserializer;

			public WrappedConfigurationProperty (string name, string monodevelopPropertyName, T defaultValue, Func<T, string> serializer, Func<string, T> deserializer)
			{
				this.serializer = serializer;
				this.deserializer = deserializer;

				underlying = ConfigurationProperty.Create(name, serializer (defaultValue), monodevelopPropertyName);
			}

			protected sealed override T OnGetValue () => deserializer (underlying.Value);
			protected sealed override bool OnSetValue (T value)
			{
				underlying.Value = serializer (value);
				return true;
			}

			internal void SetSerializedValue (string value)
			{
				underlying.Value = value;
			}
		}
	}
}
