// 
// MimeTypePolicyOptionsPanel.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Gtk;
using System.Linq;

using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal interface IMimeTypePolicyOptionsPanel: IOptionsPanel
	{
		void InitializePolicy (PolicyContainer policyContainer, PolicyContainer defaultPolicyContainer, string mimeType, bool isExactMimeType);
		void SetParentSection (MimeTypePolicyOptionsSection section);
		
		string Label { get; set; }
		Widget CreateMimePanelWidget ();
		
		void LoadCurrentPolicy ();
		void LoadParentPolicy ();
		void LoadSetPolicy (PolicyContainer pset);
		void StorePolicy ();
		
		bool HasCustomPolicy { get; }
		void RemovePolicy (PolicyContainer bag);
		IEnumerable<PolicySet> GetPolicySets ();
		PolicySet GetMatchingSet (IEnumerable<PolicySet> candidateSets);
		bool Modified { get; }
		bool HandlesPolicyType (Type type, string scope);

		void PanelSelected ();
	}
	
	public abstract class MimeTypePolicyOptionsPanel<T>: ItemOptionsPanel, IMimeTypePolicyOptionsPanel where T : class, IEquatable<T>, new ()
	{
		MimeTypePolicyOptionsSection section;
		string label;
		string mimeType;
		IEnumerable<string> mimeTypeScopes;
		PolicyContainer policyContainer;
		PolicyContainer defaultPolicyContainer;
		bool loaded;
		object cachedPolicy;
		bool hasCachedPolicy;
		CheckButton defaultSettingsButton;
		Widget panelWidget;
		bool isExactMimeType;

		void IMimeTypePolicyOptionsPanel.InitializePolicy (PolicyContainer policyContainer, PolicyContainer defaultPolicyContainer, string mimeType, bool isExactMimeType)
		{
			this.mimeType = mimeType;
			this.policyContainer = policyContainer;
			this.defaultPolicyContainer = defaultPolicyContainer;
			this.isExactMimeType = isExactMimeType;
			mimeTypeScopes = DesktopService.GetMimeTypeInheritanceChain (mimeType);
		}
		
		void IMimeTypePolicyOptionsPanel.SetParentSection (MimeTypePolicyOptionsSection section)
		{
			this.section = section;
		}
		
		void IMimeTypePolicyOptionsPanel.StorePolicy ()
		{
			if (loaded) {
				if (defaultSettingsButton != null && defaultSettingsButton.Active)
					policyContainer.Set<T> (null, mimeType);
				else
					policyContainer.Set<T> (GetPolicy (), mimeType);
			} else if (hasCachedPolicy) {
				policyContainer.Set<T> ((T) cachedPolicy, mimeType);
			}
			OnPolicyStored ();
		}

		protected virtual void OnPolicyStored ()
		{ 
		}
		void IMimeTypePolicyOptionsPanel.LoadParentPolicy ()
		{
			T policy = GetInheritedPolicy (mimeTypeScopes);

			if (loaded) {
				UpdateDefaultSettingsButton (policyContainer.ParentPolicies);
				LoadFrom ((T)policy);
			} else {
				cachedPolicy = policy;
				if (GetDirectInherited (policyContainer.ParentPolicies) == null)
					cachedPolicy = null;
				hasCachedPolicy = true;
			}
		}
		
		T GetInheritedPolicy (IEnumerable<string> scopes)
		{
			foreach (string scope in scopes) {
				PolicyContainer currentBag = scope == mimeType ? policyContainer.ParentPolicies : policyContainer;
				while (currentBag != null) {
					if (currentBag.DirectHas<T> (scope)) {
						T pol = currentBag.DirectGet<T> (scope);
						if (pol != null)
							return pol;
						// Default settings requested for this scope. Start looking from the original
						// bag now using the next scope in the chain
						break;
					} else
						currentBag = currentBag.ParentPolicies;
				}
			}
			return PolicyService.GetDefaultPolicy<T>(scopes);
		}
		
		void IMimeTypePolicyOptionsPanel.LoadSetPolicy (PolicyContainer pset)
		{
			object selected = pset.Get<T> (mimeTypeScopes);
			if (selected == null)
				selected = PolicyService.GetDefaultPolicy<T> (mimeTypeScopes);

			if (loaded) {
				if (defaultSettingsButton != null) {
					defaultSettingsButton.Active = false;
					panelWidget.Sensitive = true;
				}
				LoadFrom ((T)selected);
			} else {
				cachedPolicy = selected;
				hasCachedPolicy = true;
			}
		}
		
		bool IMimeTypePolicyOptionsPanel.HasCustomPolicy {
			get {
				return policyContainer.DirectHas<T> (mimeType);
			}
		}
		
		IEnumerable<PolicySet> IMimeTypePolicyOptionsPanel.GetPolicySets ()
		{
			return PolicyService.GetPolicySets<T> (mimeTypeScopes);
		}
		
		PolicySet IMimeTypePolicyOptionsPanel.GetMatchingSet (IEnumerable<PolicySet> candidateSets)
		{
			T pol = GetCurrentPolicy ();
			if (candidateSets != null)
				return PolicyService.GetMatchingSet (pol, candidateSets, mimeTypeScopes, false);
			else
				return PolicyService.GetMatchingSet (pol, mimeTypeScopes, false);
		}
		
		void IMimeTypePolicyOptionsPanel.RemovePolicy (PolicyContainer bag)
		{
			bag.Remove<T> (mimeType);
		}
		
		bool IMimeTypePolicyOptionsPanel.HandlesPolicyType (Type type, string scope)
		{
			return type == typeof(T) && scope == mimeType;
		}

		
		T GetDirectInherited (PolicyContainer initialContainer)
		{
			if (initialContainer == policyContainer && !loaded && hasCachedPolicy)
				return (T)cachedPolicy;
			PolicyContainer pc = initialContainer;
			while (pc != null) {
				if (pc.DirectHas<T> (mimeType))
					return pc.DirectGet<T> (mimeType);
				pc = pc.ParentPolicies;
			}
			return PolicyService.GetUserDefaultPolicySet ().Get<T> (mimeType);
		}
		
		void UpdateDefaultSettingsButton (PolicyContainer initialContainer)
		{
			if (defaultSettingsButton != null) {
				T pol = GetDirectInherited (initialContainer);
				if (pol != null) {
					panelWidget.Sensitive = true;
					defaultSettingsButton.Active = false;
				} else {
					panelWidget.Sensitive = false;
					defaultSettingsButton.Active = true;
				}
			}
		}
		
		void IMimeTypePolicyOptionsPanel.LoadCurrentPolicy ()
		{
			T policy = GetCurrentPolicy ();
			UpdateDefaultSettingsButton (policyContainer);
			loaded = true;
			hasCachedPolicy = false;
			LoadFrom (policy);
		}
		
		T GetCurrentPolicy ()
		{
			object pol = null;
			if (loaded)
				pol = GetPolicy ();
			else if (hasCachedPolicy)
				pol = cachedPolicy;

			if (pol == null) {
				pol = policyContainer.Get<T> (mimeTypeScopes);
				if (pol == null && defaultPolicyContainer != null)
					return defaultPolicyContainer.Get<T> (mimeTypeScopes);

				// If the policy container being edited doesn't have this policy defined (and doesn't inherit it from anyhwere)
				// then try getting the policy from defaultPolicyContainer.
			}
			return (T) pol;
		}
		
		string IMimeTypePolicyOptionsPanel.Label {
			get {
				return label;
			}
			set {
				label = value;
			}
		}

		Widget IMimeTypePolicyOptionsPanel.CreateMimePanelWidget ()
		{
			panelWidget = CreatePanelWidget ();
			//HACK: work around bug 469427 - broken themes match on widget names
			if (panelWidget.Name.IndexOf ("Panel") > 0)
				panelWidget.Name = panelWidget.Name.Replace ("Panel", "_");
			if (isExactMimeType)
				return panelWidget;
			
			VBox box = new VBox ();
			box.Spacing = 6;
			
			string baseType = mimeTypeScopes.ElementAt (1);
			baseType = DesktopService.GetMimeTypeDescription (baseType);
			defaultSettingsButton = new CheckButton (GettextCatalog.GetString ("Use default settings from '{0}'", baseType));
			defaultSettingsButton.Clicked += delegate {
				if (defaultSettingsButton.Active) {
					panelWidget.Sensitive = false;
					List<string> baseTypes = new List<string> (mimeTypeScopes);
					baseTypes.RemoveAt (0);
					LoadFrom (GetInheritedPolicy (baseTypes));
				} else {
					panelWidget.Sensitive = true;
				}
			};

			defaultSettingsButton.SetCommonAccessibilityAttributes ("MimePanel.DefaultCheckbox", "",
			                                                        GettextCatalog.GetString ("Check to use the default settings from '{0}'", baseType));
			defaultSettingsButton.Accessible.AddLinkedUIElement (panelWidget.Accessible);

			box.PackStart (defaultSettingsButton, false, false, 0);
			var hsep = new HSeparator ();
			hsep.Accessible.SetShouldIgnore (true);

			box.PackStart (hsep, false, false, 0);
			box.ShowAll ();
			box.PackStart (panelWidget, true, true, 0);
			panelWidget.Show ();
			return box;
		}
		
		public bool Modified {
			get {
				T pol = policyContainer.Get<T> (mimeTypeScopes) ?? PolicyService.GetDefaultPolicy<T> (mimeTypeScopes);
				return !pol.Equals (GetCurrentPolicy ());
			}
		}

		/// <summary>
		/// Gets the current policy from the same section.
		/// </summary>
		protected S GetCurrentOtherPolicy<S> () where S : class, IEquatable<S>, new ()
		{
			foreach (IMimeTypePolicyOptionsPanel p in section.Panels) {
				var panel = p as MimeTypePolicyOptionsPanel<S>;
				if (panel == null)
					continue;
				return panel.GetCurrentPolicy ();
			}

			return policyContainer.Get<S> (mimeTypeScopes);
		}
		
		protected abstract void LoadFrom (T policy);
		
		protected abstract T GetPolicy ();
		
		public override void ApplyChanges ()
		{
		}
		
		public void UpdateSelectedNamedPolicy ()
		{
			section.UpdateSelectedNamedPolicy ();
		}

		public virtual void PanelSelected ()
		{
		}
	}
}
