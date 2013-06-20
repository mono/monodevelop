//
// GroupingProviderChainControl.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using Xwt;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.CodeIssues
{
	public class GroupingProviderChainControl: HBox
	{
		IList<Type> availableProviders;
		
		IGroupingProvider rootProvider;
		
		IList<ComboBox> providerPickers = new List<ComboBox>();
		IList<Label> labels = new List<Label>();

		public GroupingProviderChainControl(IGroupingProvider rootProvider, IEnumerable<Type> providers)
		{
			this.rootProvider = rootProvider;
			availableProviders = providers.ToList();
			
			BuildUi ();
		}

		void BuildUi ()
		{
			Clear ();
			var label = new Label ("Group by:");
			labels.Add (label);
			PackStart (label);
			BuildProviderSelectors (null, rootProvider);
		}
		
		void BuildProviderSelectors (IGroupingProvider previousProvider, IGroupingProvider selectedProvider)
		{
			var combo = new ComboBox ();
			combo.Items.Add (typeof (NullGroupingProvider), "Nothing");
			combo.Items.Add (ItemSeparator.Instance);
			foreach (var providerType in availableProviders) {
				var metadata = (GroupingDescriptionAttribute)providerType.GetCustomAttributes (false)
					.FirstOrDefault (attr => attr is GroupingDescriptionAttribute);
				if (metadata == null) {
					LoggingService.LogWarning ("Grouping provider '{0}' does not have a metadata attribute, ignoring provider.", providerType.FullName);
					continue;
				}
				combo.Items.Add (providerType, metadata.Title);
			}
			if (selectedProvider != null) {
				combo.SelectedItem = selectedProvider.GetType ();
			} else {
				combo.SelectedItem = typeof (NullGroupingProvider);
			}
			combo.SelectionChanged += (sender, e) => {
				var newSelected = combo.SelectedItem as Type;
				if (newSelected == null)
					return;
				var newProvider = (IGroupingProvider)Activator.CreateInstance(newSelected);
				
				if (previousProvider != null) {
					previousProvider.Next = newProvider;
				} else {
					rootProvider = newProvider;
				}
				if (newProvider.SupportsNext && selectedProvider.SupportsNext) {
					newProvider.Next = selectedProvider.Next;
				}
				BuildUi ();
			};
			providerPickers.Add (combo);
			PackStart (combo);
			
			if (selectedProvider.SupportsNext) {
				PackStart (new Label ("then by"));
				BuildProviderSelectors (selectedProvider, selectedProvider.Next);
			}
		}
	}
}

