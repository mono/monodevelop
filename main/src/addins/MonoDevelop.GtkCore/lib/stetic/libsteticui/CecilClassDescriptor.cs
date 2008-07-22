using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Reflection;
using Mono.Cecil;

namespace Stetic
{
	internal class CecilClassDescriptor: Stetic.ClassDescriptor
	{
		string wrappedTypeName;
		ClassDescriptor typeClassDescriptor;
		ClassDescriptor wrapperClassDescriptor;
		Gdk.Pixbuf icon;
		TypeDefinition type;
		XmlElement steticDefinition;
		CecilWidgetLibrary cecilLib;
		bool useCustomWidgetBox;
		string widgetId;
		bool canGenerateCode;
		
		public CecilClassDescriptor (CecilWidgetLibrary lib, XmlElement element, ClassDescriptor typeClassDescriptor, XmlElement steticDefinition, TypeDefinition cls)
		{
			this.cecilLib = lib;
			this.steticDefinition = steticDefinition;
			this.typeClassDescriptor = typeClassDescriptor;
			wrappedTypeName = element.GetAttribute ("type");
			type = cls;
			Load (element);
			type = null;
			canGenerateCode = true;
			
			string baseType = element.GetAttribute ("base-type");
			if (baseType.Length > 0) {
				wrapperClassDescriptor = Registry.LookupClassByName (baseType);
				if (wrapperClassDescriptor == null)
					throw new InvalidOperationException ("Unknown base type: " + baseType);
			} else {
				wrapperClassDescriptor = typeClassDescriptor;
			}
			
			if (steticDefinition == null && !AllowChildren && NeedsBlackBox (typeClassDescriptor.Name)) {
				// It is not possible to create instances of that widget, instead we'll have
				// to create the typical custom widget black box.
				
				if (!CanCreateWidgetInstance (wrapperClassDescriptor.Name))
					throw new InvalidOperationException ("Can't load widget type '" + Name + "'. Instances of that type can't be created because the type can't be loaded into the process.");
				
				useCustomWidgetBox = true;
			}
			
			widgetId = Name.ToLower ();
			int i = widgetId.LastIndexOf ('.');
			if (i != -1) {
				if (i != widgetId.Length - 1)
					widgetId = widgetId.Substring (i+1);
				else
					widgetId = widgetId.Replace (".", "");
			}
			
			string iconName = element.GetAttribute ("icon");
			icon = lib.GetEmbeddedIcon (iconName);
			
			// If the class is a custom widget created using stetic, it means that it has
			// simple property and there is no custom logic, so it is safe to generate code
			// for this class.
			if (steticDefinition != null)
				canGenerateCode = true;

			// If it has a custom wrapper, then it definitely has custom logic, so it can't generate code 				
			if (element.HasAttribute ("wrapper"))
				canGenerateCode = false;
		}

		public override string WrappedTypeName {
			get { return wrappedTypeName; }
		}
		
		public override Gdk.Pixbuf Icon {
			get { return icon; }
		}
		
		public bool CanGenerateCode {
			get { return canGenerateCode; }
		}
		
		public override object CreateInstance (Stetic.IProject proj)
		{
			Gtk.Widget res;
			
			if (useCustomWidgetBox) {
				res = CreateFakeWidget (wrapperClassDescriptor.Name);
				res.ShowAll ();
			}
			else if (steticDefinition != null) {
				Gtk.Container w = Stetic.WidgetUtils.ImportWidget (proj, steticDefinition) as Gtk.Container;
				MakeChildrenUnselectable (w);
				res = w;
			}
			else {
				res = (Gtk.Widget) typeClassDescriptor.CreateInstance (proj);
				
				// If it is a custom widget and there is no stetic project for it, just
				// show it as a regular custom widget.
				Stetic.CustomWidget custom = res as Stetic.CustomWidget;
				if (custom != null) {
					Stetic.Custom c = new Stetic.Custom ();
					// Give it some default size
					c.WidthRequest = 20;
					c.HeightRequest = 20;
					custom.Add (c);
					custom.ShowAll ();
					res = custom;
				}
			}
			
			res.Name = widgetId;
			return res;
		}
		
		public override Stetic.ObjectWrapper CreateWrapper ()
		{
			return wrapperClassDescriptor.CreateWrapper ();
		}
		
		protected override Stetic.ItemDescriptor CreateItemDescriptor (XmlElement elem, Stetic.ItemGroup group)
		{
			string mname = elem.GetAttribute ("name");
			if (elem.Name == "property") {
				if (type != null) {
					PropertyDefinition propInfo = FindProperty (type, mname);
					if (propInfo != null)
						return new CecilPropertyDescriptor (cecilLib, elem, group, this, propInfo);
				}
				else
					return new CecilPropertyDescriptor (cecilLib, elem, group, this, null);
			}
			else if (elem.Name == "signal") {
				if (type != null) {
					EventDefinition signalInfo = FindEvent (type, mname);
					if (signalInfo != null)
						return new CecilSignalDescriptor (cecilLib, elem, group, this, signalInfo);
				}
				else
					return new CecilSignalDescriptor (cecilLib, elem, group, this, null);
			}
			else
				return base.CreateItemDescriptor (elem, group);

			return null;
		}
		
		PropertyDefinition FindProperty (TypeDefinition cls, string name)
		{
			foreach (PropertyDefinition propInfo in cls.Properties)
				if (propInfo.Name == name)
					return propInfo;
			
			if (cls.BaseType == null)
				return null;

			string baseType = cls.BaseType.FullName;
			Type t = Registry.GetType (baseType, false);
			if (t != null) {
				PropertyInfo prop = t.GetProperty (name);
				if (prop != null) {
					TypeReference tref  = new TypeReference (prop.PropertyType.Name, prop.PropertyType.Namespace, null, prop.PropertyType.IsValueType);
					PropertyDefinition pdef = new PropertyDefinition (name, tref, (Mono.Cecil.PropertyAttributes) 0);
					PropertyDefinition.CreateGetMethod (pdef);
					PropertyDefinition.CreateSetMethod (pdef);
					return pdef;
				}
			}
			
			TypeDefinition bcls = cecilLib.FindTypeDefinition (baseType);
			if (bcls != null)
				return FindProperty (bcls, name);
			else
				return null;
		}
		
		EventDefinition FindEvent (TypeDefinition cls, string name)
		{
			foreach (EventDefinition eventInfo in cls.Events)
				if (eventInfo.Name == name)
					return eventInfo;
			
			if (cls.BaseType == null)
				return null;
			
			string baseType = cls.BaseType.FullName;
			Type t = Registry.GetType (baseType, false);
			if (t != null) {
				EventInfo ev = t.GetEvent (name);
				if (ev != null) {
					TypeReference tref  = new TypeReference (ev.EventHandlerType.Name, ev.EventHandlerType.Namespace, null, ev.EventHandlerType.IsValueType);
					return new EventDefinition (name, tref, (Mono.Cecil.EventAttributes) 0);
				}
			}
			
			TypeDefinition bcls = cecilLib.FindTypeDefinition (baseType);
			if (bcls != null)
				return FindEvent (bcls, name);
			else
				return null;
		}
		
		void MakeChildrenUnselectable (Gtk.Widget w)
		{
			// Remove the registered signals, since those signals are bound
			// to the custom widget class, not the widget container class.
			Stetic.Wrapper.Widget ww = Stetic.Wrapper.Widget.Lookup (w);
			if (ww == null)
				return;
			ww.Signals.Clear ();
			
			foreach (Gtk.Widget child in (Gtk.Container)w) {
				Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (child);
				if (wrapper != null) {
					wrapper.Signals.Clear ();
					wrapper.Unselectable = true;
				}
				if (child is Gtk.Container)
					MakeChildrenUnselectable (child);
			}
		}
		
		bool CanCreateWidgetInstance (string typeName)
		{
			switch (typeName) {
				case "Gtk.Fixed":
					return false;
			}
			return true;
		}
		
		bool NeedsBlackBox (string typeName)
		{
			switch (typeName) {
				case "Gtk.Widget":
				case "Gtk.Container":
				case "Gtk.Alignment":
				case "Gtk.Fixed":
				case "Gtk.Frame":
				case "Gtk.HBox":
				case "Gtk.VBox":
				case "Gtk.Box":
				case "Gtk.ButtonBox":
				case "Gtk.Paned":
				case "Gtk.VPaned":
				case "Gtk.HPaned":
				case "Gtk.Notebook":
				case "Gtk.ScrolledWindow":
				case "Gtk.Table":
				case "Gtk.Bin":
					return true;
			}
			return false;
		}
		
		Gtk.Widget CreateFakeWidget (string typeName)
		{
			Stetic.Custom c = new Stetic.Custom ();
			// Give it some default size
			c.WidthRequest = 20;
			c.HeightRequest = 20;
			
			Gtk.Container box = null;
			
			switch (typeClassDescriptor.Name) {
				case "Gtk.Alignment":
					box = new Gtk.Alignment (0.5f, 0.5f, 1f, 1f);
					break;
				case "Gtk.Fixed":
					box = new Gtk.Alignment (0.5f, 0.5f, 1f, 1f);
					break;
				case "Gtk.Frame":
					box = new Gtk.Frame ();
					break;
				case "Gtk.Box":
				case "Gtk.HBox": {
					Gtk.HBox cc = new Gtk.HBox ();
					cc.PackStart (c, true, true, 0);
					return cc;
				}
				case "Gtk.VBox": {
					Gtk.VBox cc = new Gtk.VBox ();
					cc.PackStart (c, true, true, 0);
					return cc;
				}
				case "Gtk.Paned":
				case "Gtk.VPaned": {
					Gtk.VPaned cc = new Gtk.VPaned ();
					cc.Add1 (c);
					return cc;
				}
				case "Gtk.HPaned": {
					Gtk.HPaned cc = new Gtk.HPaned ();
					cc.Add1 (c);
					return cc;
				}
				case "Gtk.Notebook": {
					Gtk.Notebook nb = new Gtk.Notebook ();
					nb.ShowTabs = false;
					nb.AppendPage (c, null);
					return nb;
				}
				case "Gtk.ScrolledWindow": {
					Gtk.ScrolledWindow cc = new Gtk.ScrolledWindow ();
					cc.VscrollbarPolicy = Gtk.PolicyType.Never;
					cc.HscrollbarPolicy = Gtk.PolicyType.Never;
					cc.Add (c);
					return cc;
				}
				case "Gtk.Table": {
					Gtk.Table t = new Gtk.Table (1, 1, false);
					t.Attach (c, 0, 1, 0, 1);
					return t;
				}
				case "Gtk.ButtonBox":
					return new Gtk.HButtonBox ();
			}
			if (box != null) {
				box.Add (c);
				return box;
			} else {
				Stetic.CustomWidget custom = new Stetic.CustomWidget ();
				if (custom.Child != null)
					custom.Remove (custom.Child);
				custom.Add (c);
				return custom;
			}
		}
	}
	
	class CustomControlWrapper: Stetic.Wrapper.Container
	{
		protected override bool AllowPlaceholders {
			get {
				return false;
			}
		}
	}
	
	class ClassDescriptorWrapper: Stetic.ClassDescriptor
	{
		ClassDescriptor wrapped;
		
		public ClassDescriptorWrapper (ClassDescriptor wrapped)
		{
			this.wrapped = wrapped;
			label = wrapped.Label;
			
		}
		
		public override string WrappedTypeName {
			get { return wrapped.WrappedTypeName; }
		}
		
		public override Gdk.Pixbuf Icon {
			get { return wrapped.Icon; }
		}
		
		public override object CreateInstance (Stetic.IProject proj)
		{
			CustomWidget custom = new CustomWidget ();
			Stetic.Custom c = new Stetic.Custom ();
			// Give it some default size
			c.WidthRequest = 20;
			c.HeightRequest = 20;
			custom.Add (c);
			return c;
		}
		
		public override Stetic.ObjectWrapper CreateWrapper ()
		{
			return new Wrapper.Container ();
		}
	}
}

