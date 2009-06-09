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

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Projects.Gui.Dialogs
{
	internal interface IMimeTypePolicyOptionsPanel: IOptionsPanel
	{
		void InitializePolicy (IPolicyContainer policyContainer, string mimeType, bool isExactMimeType);
		void SetParentSection (MimeTypePolicyOptionsSection section);
		
		string Label { get; set; }
		Widget CreateMimePanelWidget ();
		
		void LoadCurrentPolicy ();
		void LoadParentPolicy ();
		void LoadSetPolicy (PolicySet pset);
		void StorePolicy ();
		
		bool HasCustomPolicy { get; }
		void RemovePolicy (IPolicyContainer bag);
		IEnumerable<PolicySet> GetPolicySets ();
		PolicySet GetMatchingSet ();
	}
	
	public abstract class MimeTypePolicyOptionsPanel<T>: ItemOptionsPanel, IMimeTypePolicyOptionsPanel where T : class, IEquatable<T>, new ()
	{
		MimeTypePolicyOptionsSection section;
		string label;
		string mimeType;
		IEnumerable<string> mimeTypeScopes;
		IPolicyContainer policyContainer;
		bool loaded;
		object cachedPolicy;
		bool hasCachedPolicy;
		CheckButton defaultSettingsButton;
		Widget panelWidget;
		bool isExactMimeType;
		
		void IMimeTypePolicyOptionsPanel.InitializePolicy (IPolicyContainer policyContainer, string mimeType, bool isExactMimeType)
		{
			this.mimeType = mimeType;
			this.policyContainer = policyContainer;
			this.isExactMimeType = isExactMimeType;
			mimeTypeScopes = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeInheritanceChain (mimeType);
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
				IPolicyContainer currentBag = scope == mimeType ? policyContainer.ParentPolicies : policyContainer;
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
		
		void IMimeTypePolicyOptionsPanel.LoadSetPolicy (PolicySet pset)
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
		
		PolicySet IMimeTypePolicyOptionsPanel.GetMatchingSet ()
		{
			T pol = GetCurrentPolicy ();
			return PolicyService.GetMatchingSet (pol, mimeTypeScopes);
		}
		
		void IMimeTypePolicyOptionsPanel.RemovePolicy (IPolicyContainer bag)
		{
			bag.Remove<T> (mimeType);
		}
		
		T GetDirectInherited (IPolicyContainer initialContainer)
		{
			if (initialContainer == policyContainer && !loaded && hasCachedPolicy)
				return (T)cachedPolicy;
			IPolicyContainer pc = initialContainer;
			while (pc != null) {
				if (pc.DirectHas<T> (mimeType))
					return pc.DirectGet<T> (mimeType);
				pc = pc.ParentPolicies;
			}
			return PolicyService.GetUserDefaultPolicySet ().Get<T> (mimeType);
		}
		
		void UpdateDefaultSettingsButton (IPolicyContainer initialContainer)
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

			if (pol == null)
				pol = policyContainer.Get<T> (mimeTypeScopes) ?? PolicyService.GetDefaultPolicy<T> (mimeTypeScopes);
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
			if (isExactMimeType)
				return panelWidget;
			
			VBox box = new VBox ();
			box.Spacing = 6;
			
			string baseType = mimeTypeScopes.ElementAt (1);
			baseType = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeDescription (baseType);
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
			
			box.PackStart (defaultSettingsButton, false, false, 0);
			box.PackStart (new HSeparator (), false, false, 0);
			box.ShowAll ();
			box.PackStart (panelWidget, true, true, 0);
			panelWidget.Show ();
			return box;
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
	}
}
