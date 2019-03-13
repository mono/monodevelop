//
// DesignerSupportService.cs: Service that provides facilities useful 
//    for visual designers.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2006 Michael Hutchinson
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.DesignerSupport
{
	public class DesignerSupportService
	{
		IPropertyPad propertyPad = null;
		ToolboxService toolboxService = null;
		IPropertyProvider[] providers;
		
		IPropertyPadProvider lastPadProvider;
		object lastComponent;
		ICustomPropertyPadProvider lastCustomProvider;
		
		#region PropertyPad
		
		public IPropertyPad PropertyPad {
			get {
				return propertyPad;
			}
		}
		
		internal void SetPad (IPropertyPad pad)
		{
			propertyPad = pad;
			
			if (propertyPad != null) {
				if (lastPadProvider != null) {
					object[] provs = GetProvidersForObject (lastComponent, lastPadProvider.GetProvider ());
					if (provs.Length > 0)
						propertyPad.SetCurrentObject (lastComponent, provs);
					else
						propertyPad.BlankPad ();

					if (lastPadProvider is IPropertyPadCustomizer customizer && pad is PropertyPad ppad) {
						customizer.Customize (ppad.PadWindow, ppad.PropertyGrid);
					}
					propertyPad.PropertyGridChanged += OnPropertyGridChanged;		
				}
				else if (lastCustomProvider != null) {
					if (propertyPad is PropertyPad ppad) {
						try {
							var currentCustomWidget = lastCustomProvider.GetCustomPropertyWidget ();
							if (currentCustomWidget != null) {
								ppad.UseCustomWidget (currentCustomWidget);
								if (lastCustomProvider is IPropertyPadCustomizer customizer)
									customizer.Customize (ppad.PadWindow, null);
							}
						} catch (Exception ex) {
							LoggingService.LogInternalError ($"There was an error trying to GetCustomPropertyWidget from '{lastCustomProvider.GetType ()}' provider", ex);
							ReSetPad ();
						}
					}
				}
			}
		}
		
		void DisposePropertyPadProvider ()
		{
			if (lastPadProvider != null) {
				if (propertyPad != null)
					propertyPad.PropertyGridChanged -= OnPropertyGridChanged;
				lastPadProvider.OnEndEditing (lastComponent);
				lastPadProvider = null;
				lastComponent = null;
			}
		}
		
		void DisposeCustomPropertyPadProvider ()
		{
			if (lastCustomProvider != null) {
				lastCustomProvider.DisposeCustomPropertyWidget ();
				lastCustomProvider = null;
			}
		}
		
		internal void ReSetPad ()
		{
			DisposePropertyPadProvider ();
			DisposeCustomPropertyPadProvider ();
			if (propertyPad != null)
				propertyPad.BlankPad ();
		}

		public void SetPadContent (IPropertyPadProvider provider)
		{
			SetPadContent (provider, null);
		}

		public void SetPadContent (IPropertyPadProvider provider, object commandRouteOrigin)
		{
			if (provider != null) {
				// If there was a custom provider, reset it now
				DisposeCustomPropertyPadProvider ();
				
				object comp = provider.GetActiveComponent ();
				if (lastPadProvider != null && comp == lastComponent)
					return;

				DisposePropertyPadProvider ();
				
				lastPadProvider = provider;
				lastComponent = comp;
				
				if (propertyPad == null)
					return;
					
				object[] provs = GetProvidersForObject (comp, provider.GetProvider ());
				if (provs.Length > 0) {
					propertyPad.SetCurrentObject (comp, provs);

					if (propertyPad is PropertyPad propPad) {
						propPad.CommandRouteOrigin = commandRouteOrigin;
					}
				}
				else
					propertyPad.BlankPad ();

				if (provider is IPropertyPadCustomizer customizer && propertyPad is PropertyPad ppad)
					customizer.Customize (ppad.PadWindow, ppad.PropertyGrid);

				propertyPad.PropertyGridChanged += OnPropertyGridChanged;
			}
			else {
				ReSetPad ();
			}
		}
		
		public void SetPadContent (ICustomPropertyPadProvider provider)
		{
			SetPadContent (provider, null);
		}

		public void SetPadContent (ICustomPropertyPadProvider provider, Gtk.Widget commandRouteOrigin)
		{
			if (provider != null) {
				
				if (lastCustomProvider == provider)
					return;

				// If there was a pad provider reset it now.
				DisposePropertyPadProvider ();
				DisposeCustomPropertyPadProvider ();

				lastCustomProvider = provider;

				if (propertyPad != null && propertyPad is PropertyPad ppad) {
					try {
						var customWidget = provider.GetCustomPropertyWidget ();
						if (customWidget != null) {
							ppad.UseCustomWidget (customWidget);
							ppad.CommandRouteOrigin = commandRouteOrigin;
							var customizer = provider as IPropertyPadCustomizer;
							if (customizer != null)
								customizer.Customize (ppad.PadWindow, null);
						} else {
							propertyPad?.BlankPad ();
							return;
						}
					} catch (Exception ex) {
						LoggingService.LogInternalError ($"There was an error trying to GetCustomPropertyWidget from '{lastCustomProvider.GetType ()}' provider", ex);
						propertyPad?.BlankPad ();
						return;
					}
				}
			}
			else {
				ReSetPad ();
			}
		}
		
		internal object[] GetProvidersForObject (object obj, object firstProvider)
		{
			if (providers == null)
				providers = (IPropertyProvider[]) AddinManager.GetExtensionObjects ("/MonoDevelop/DesignerSupport/PropertyProviders", typeof(IPropertyProvider), true);
			
			ArrayList list = new ArrayList ();
			if (firstProvider != null)
				list.Add (firstProvider);
			foreach (IPropertyProvider prov in providers)
				if (prov.SupportsObject (obj))
					list.Add (prov.CreateProvider (obj));
			return list.ToArray ();
		}
			
		void OnPropertyGridChanged (object s, EventArgs a)
		{
			if (lastPadProvider != null)
				lastPadProvider.OnChanged (lastComponent);
		}
		
#endregion
		
#region Toolbox
		
		public ToolboxService ToolboxService {
			get{
				//lazy load of toolbox contents
				if (toolboxService == null) {
					toolboxService = new ToolboxService ();
				}
				
				return toolboxService;
			}
		}
		
#endregion
		
		internal DesignerSupportService ()
		{
			IdeApp.CommandService.RegisterCommandTargetVisitor (new PropertyPadVisitor ());
			AddinManager.ExtensionChanged += OnExtensionChanged;
		}
		
		void OnExtensionChanged (object s, ExtensionEventArgs args)
		{
			if (args.PathChanged ("MonoDevelop/DesignerSupport/PropertyProviders")) {
				providers = null;
				ReSetPad ();
			}
		}
	}
	
	public static class DesignerSupport
	{
		static DesignerSupportService designerSupportService;
		
		public static DesignerSupportService Service {
			get {
				if (designerSupportService == null)
					designerSupportService = new DesignerSupportService ();
				return designerSupportService;
			}
		}
	}
}
