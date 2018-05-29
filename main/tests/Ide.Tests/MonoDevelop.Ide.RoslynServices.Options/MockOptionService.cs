//
// MockOptionService.cs
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
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	class MockOptionService : IGlobalOptionService, IOptionService
	{
		IOptionPersister persister;
		object _gate = new object ();
		HashSet<IOption> _options = new HashSet<IOption> ();
		ImmutableDictionary<OptionKey, object> _currentValues = ImmutableDictionary.Create<OptionKey, object> ();

		public void RegisterPersister (IOptionPersister persister) => this.persister = persister;

		public void RegisterOption (IOption option) => _options.Add (option);

		public IEnumerable<IOption> GetRegisteredOptions () => _options;

		public T GetOption<T> (Option<T> option) => (T)GetOption (new OptionKey (option, language: null));

		public T GetOption<T> (PerLanguageOption<T> option, string language) => (T)GetOption (new OptionKey (option, language));

		public bool GetOptionInternal (OptionKey optionKey, out object value)
		{
			return _currentValues.TryGetValue (optionKey, out value);
		}

		public object GetOption (OptionKey optionKey)
		{
			lock (_gate) {
				if (GetOptionInternal (optionKey, out var value))
					return value;

				value = LoadOptionFromSerializerOrGetDefault (optionKey);
				_currentValues = _currentValues.Add (optionKey, value);
				return value;
			}
		}

		object LoadOptionFromSerializerOrGetDefault (OptionKey optionKey)
		{
			if (persister.TryFetch (optionKey, out var deserializedValue))
				return deserializedValue;

			return optionKey.Option.DefaultValue;
		}

		public void SetOptions (OptionSet optionSet)
		{
			var workspaceOptionSet = (WorkspaceOptionSet)optionSet;

			var changedOptions = new List<OptionChangedEventArgs> ();

			lock (_gate) {
				foreach (var optionKey in workspaceOptionSet.GetAccessedOptions ()) {
					var setValue = optionSet.GetOption (optionKey);
					var currentValue = GetOption (optionKey);

					if (object.Equals (currentValue, setValue)) {
						// Identical, so nothing is changing
						continue;
					}

					// The value is actually changing, so update
					changedOptions.Add (new OptionChangedEventArgs (optionKey, setValue));

					_currentValues = _currentValues.SetItem (optionKey, setValue);

					persister.TryPersist (optionKey, setValue);
				}
			}

			// Outside of the lock, raise the events on our task queue.
			RaiseEvents (changedOptions);
		}

		public void RefreshOption (OptionKey optionKey, object newValue)
		{
			lock (_gate) {
				if (_currentValues.TryGetValue (optionKey, out var oldValue)) {
					if (object.Equals (oldValue, newValue)) {
						// Value is still the same, no reason to raise events
						return;
					}
				}

				_currentValues = _currentValues.SetItem (optionKey, newValue);
			}

			RaiseEvents (new List<OptionChangedEventArgs> { new OptionChangedEventArgs (optionKey, newValue) });
		}

		void RaiseEvents (List<OptionChangedEventArgs> changedOptions)
		{
			var optionChanged = OptionChanged;
			if (optionChanged != null) {
				foreach (var changedOption in changedOptions) {
					optionChanged (this, changedOption);
				}
			}
		}

		public OptionSet GetOptions () =>  new WorkspaceOptionSet (this);

		public void RegisterDocumentOptionsProvider (IDocumentOptionsProvider documentOptionsProvider) => throw new NotImplementedException ();

		public Task<OptionSet> GetUpdatedOptionSetForDocumentAsync (Document document, OptionSet optionSet, CancellationToken cancellationToken) => throw new NotImplementedException ();

		public event EventHandler<OptionChangedEventArgs> OptionChanged;
	}
}
