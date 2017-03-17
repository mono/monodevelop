//
// MultiItemSolutionRunConfiguration.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Serialization;
using System.Linq;
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public sealed class MultiItemSolutionRunConfiguration : SolutionRunConfiguration
	{
		internal MultiItemSolutionRunConfiguration ()
		{
			Items = new StartupItemCollection ();
		}

		public MultiItemSolutionRunConfiguration (string id, string name) : base (id, name)
		{
			Items = new StartupItemCollection ();
		}

		public MultiItemSolutionRunConfiguration (MultiItemSolutionRunConfiguration other, string newName = null) : base (newName ?? other.Id, newName ?? other.Name)
		{
			Items = new StartupItemCollection ();
			Items.AddRange (other.Items.Select (it => new StartupItem (it.SolutionItem, it.RunConfiguration)));
		}

		public void CopyFrom (MultiItemSolutionRunConfiguration other)
		{
			Items.Clear ();
			Items.AddRange (other.Items.Select (it => new StartupItem (it.SolutionItem, it.RunConfiguration)));
			OnRunConfigurationsChanged ();
		}

		public StartupItemCollection Items { get; }

		[ItemProperty ("Items")]
		[ItemProperty ("StartupItem", ValueType = typeof (StartupItem), Scope = "*")]
		StartupItem [] ItemArray {
			get {
				return Items.ToArray ();
			}
			set {
				Items.Clear ();
				Items.AddRange (value);
			}
		}

		internal void ReplaceItem (SolutionItem oldItem, SolutionItem newItem)
		{
			foreach (var si in Items)
				if (si.SolutionItem == oldItem)
					si.SolutionItem = newItem;
		}

		internal void RemoveItem (SolutionItem item)
		{
			var i = Items.FindIndex (si => si.SolutionItem == item);
			if (i != -1)
				Items.RemoveAt (i);
		}

		internal void OnRunConfigurationsChanged ()
		{
			ParentSolution?.NotifyRunConfigurationsChanged ();
		}

		internal void ResolveObjects (Solution sol)
		{
			for (int i = 0; i < Items.Count; i++) {
				var it = Items [i];
				it.ResolveObjects (sol);
				if (it.SolutionItem == null) {//If we can't resolve project, it was probably removed from solution
					Items.RemoveAt (i);
					i--;
				}
			}
		}

		public override string Summary {
			get {
				if (Items.Count == 0)
					return GettextCatalog.GetString ("No projects selected to run");
				var sb = new StringBuilder ();
				foreach (var it in Items) {
					if (sb.Length > 0)
						sb.Append (", ");
					sb.Append (it.SolutionItem.Name).Append (" (").Append (it.RunConfiguration.Name).Append (")");
				}
				return sb.ToString ();
			}
		}

}

	public sealed class StartupItem
	{
		StartupItem ()
		{
		}

		public StartupItem (SolutionItem item, SolutionItemRunConfiguration configuration)
		{
			SolutionItem = item;
			RunConfiguration = configuration;
		}

		string itemId;
		string itemName;
		string configurationId;

		[ItemProperty]
		string ItemId {
			get { return itemId ?? SolutionItem?.ItemId; }
			set { itemId = value; }
		}

		[ItemProperty]
		string ItemName {
			get { return itemName ?? SolutionItem?.Name; }
			set { itemName = value; }
		}

		[ItemProperty]
		string ConfigurationId {
			get { return configurationId ?? RunConfiguration?.Id; }
			set { configurationId = value; }
		}

		internal void ResolveObjects (Solution sol)
		{
			if (ItemId != null) {
				SolutionItem = sol.GetSolutionItem (ItemId) as SolutionItem;
				if (SolutionItem == null)
					SolutionItem = sol.FindProjectByName (ItemName) as SolutionItem;
				if (SolutionItem != null && ConfigurationId != null)
					RunConfiguration = SolutionItem.GetRunConfigurations ().FirstOrDefault (c => c.Id == ConfigurationId);
				ItemId = null;
				ConfigurationId = null;
			}
		}
	
		public SolutionItem SolutionItem { get; internal set; }
		public SolutionItemRunConfiguration RunConfiguration { get; internal set; }
	}

	public sealed class MultiItemSolutionRunConfigurationCollection : ItemCollection<MultiItemSolutionRunConfiguration>
	{
		Solution parentSolution;

		public MultiItemSolutionRunConfigurationCollection ()
		{
		}

		internal MultiItemSolutionRunConfigurationCollection (Solution parentSolution)
		{
			this.parentSolution = parentSolution;
		}

		protected override void OnItemsAdded (IEnumerable<MultiItemSolutionRunConfiguration> items)
		{
			if (parentSolution != null) {
				foreach (var conf in items) {
					conf.ParentSolution = parentSolution;
					conf.ResolveObjects (parentSolution);
				}
			}
			base.OnItemsAdded (items);
			parentSolution.OnRunConfigurationsAdded (items);
		}

		protected override void OnItemsRemoved (IEnumerable<MultiItemSolutionRunConfiguration> items)
		{
			if (parentSolution != null) {
				foreach (var conf in items)
					conf.ParentSolution = null;
			}
			base.OnItemsRemoved (items);
			parentSolution.OnRunConfigurationRemoved (items);
		}
	}

	public sealed class StartupItemCollection : ItemCollection<StartupItem>
	{
		MultiItemSolutionRunConfiguration parent;

		public StartupItemCollection ()
		{
		}

		internal StartupItemCollection (MultiItemSolutionRunConfiguration parent)
		{
			this.parent = parent;
		}

		protected override void OnItemsAdded (IEnumerable<StartupItem> items)
		{
			base.OnItemsAdded (items);
			parent?.OnRunConfigurationsChanged ();
		}

		protected override void OnItemsRemoved (IEnumerable<StartupItem> items)
		{
			base.OnItemsRemoved (items);
			parent?.OnRunConfigurationsChanged ();
		}
	}}

