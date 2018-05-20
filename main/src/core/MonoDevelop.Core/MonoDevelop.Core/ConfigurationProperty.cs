//
// PropertyService.ConfigurationProperty.cs
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

namespace MonoDevelop.Core
{
	/// <summary>
	/// The Property wrapper wraps a global property service value as an easy to use object.
	/// </summary>
	public abstract class ConfigurationProperty<T>
	{
		public T Value {
			get => OnGetValue ();
			set => OnSetValue (value);
		}

		/// <summary>
		/// Set the property to the specified value.
		/// </summary>
		/// <param name='newValue'>
		/// The new value.
		/// </param>
		/// <returns>
		/// true, if the property has changed, false otherwise.
		/// </returns>
		public bool Set (T newValue) => OnSetValue (newValue);

		public static implicit operator T (ConfigurationProperty<T> watch) => watch.Value;

		protected abstract T OnGetValue ();

		protected abstract bool OnSetValue (T value);

		protected void OnChanged () => Changed?.Invoke (this, EventArgs.Empty);

		public event EventHandler Changed;
	}

	class CoreConfigurationProperty<T> : ConfigurationProperty<T>
	{
		T value;
		public string PropertyName { get; }

		public CoreConfigurationProperty (string name, T defaultValue, string oldName = null)
		{
			PropertyName = name ?? throw new ArgumentNullException (nameof(name));

			// Migrate the property from oldName to name.
			if (!string.IsNullOrEmpty (oldName) && PropertyService.HasValue (oldName)) {
				// Migrate the old value if the new one is not set.
				if (!PropertyService.HasValue (PropertyName)) {
					var oldValue = PropertyService.Get<T> (oldName);
					PropertyService.Set (PropertyName, oldValue);
				}
				PropertyService.Set (oldName, null);
			}

			value = PropertyService.Get (PropertyName, defaultValue);
		}

		protected override T OnGetValue () => value;

		protected override bool OnSetValue (T value)
		{
			if (EqualityComparer<T>.Default.Equals (this.value, value))
				return false;

			this.value = value;
			PropertyService.Set (PropertyName, value);
			OnChanged ();
			return true;
		}
	}

	class ObsoleteConfigurationProperty<T> : ConfigurationProperty<T>
	{
		readonly T value;

		public ObsoleteConfigurationProperty (T value) => this.value = value;
		protected override T OnGetValue () => value;
		protected override bool OnSetValue (T value) => false;
	}

	public abstract class ConfigurationProperty
	{
		public static ConfigurationProperty<T> Create<T> (string propertyName, T defaultValue, string oldName = null)
			=> new CoreConfigurationProperty<T> (propertyName, defaultValue, oldName);

		public static ConfigurationProperty<T> CreateObsolete<T> (T value) => new ObsoleteConfigurationProperty<T> (value);
	}
}
