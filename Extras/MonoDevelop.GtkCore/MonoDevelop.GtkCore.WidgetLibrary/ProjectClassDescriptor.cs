//
// ProjectClassDescriptor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Xml;

using MonoDevelop.Projects;	
using MonoDevelop.Projects.Parser;	
using MonoDevelop.Ide.Gui;	
using MonoDevelop.GtkCore.GuiBuilder;	

namespace MonoDevelop.GtkCore.WidgetLibrary
{
	internal class ProjectClassDescriptor: Stetic.ClassDescriptor
	{
		string wrappedTypeName;
		Stetic.ClassDescriptor typeClassDescriptor;
		ProjectClassInfo classInfo;
		Gdk.Pixbuf icon;
		
		public ProjectClassDescriptor (XmlElement element, ProjectClassInfo classInfo)
		{
			this.classInfo = classInfo;
			this.typeClassDescriptor = classInfo.BaseDescriptor;
			
			wrappedTypeName = element.GetAttribute ("type");
			icon = IdeApp.Services.Resources.GetIcon ("md-gtkcore-widget", Gtk.IconSize.LargeToolbar);
			
			Load (element);
		}
		
		public ProjectClassInfo ClassInfo {
			get { return classInfo; }
		}
		
		public override string WrappedTypeName {
			get { return wrappedTypeName; }
		}
		
		public override Gdk.Pixbuf Icon {
			get { return icon; }
		}
		
		public override object CreateInstance (Stetic.IProject proj)
		{
			if (classInfo.WidgetDesc != null) {
//				Gtk.Container w = Stetic.WidgetUtils.BuildWidget (classInfo.WidgetDesc) as Gtk.Container;
				Gtk.Container w = Stetic.WidgetUtils.ImportWidget (proj, classInfo.WidgetDesc) as Gtk.Container;
				MakeChildrenUnselectable (w);
				return w;
			}
			
			object res = typeClassDescriptor.CreateInstance (proj);
			
			// If it is a custom widget and there is no stetic project for it, just
			// show it as a regular custom widget.
			Stetic.CustomWidget custom = res as Stetic.CustomWidget;
			if (custom != null) {
				Stetic.Custom c = new Stetic.Custom ();
				// Give it some default size
				c.WidthRequest = 20;
				c.HeightRequest = 20;
				custom.Add (c);
			}
				
			return res;
		}
		
		void MakeChildrenUnselectable (Gtk.Widget w)
		{
			foreach (Gtk.Widget child in (Gtk.Container)w) {
				Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (child);
				if (wrapper != null)
					wrapper.Unselectable = true;
				if (child is Gtk.Container)
					MakeChildrenUnselectable (child);
			}
		}
		
		public override Stetic.ObjectWrapper CreateWrapper ()
		{
			return new CustomWidgetWrapper ();
		}
		
		protected override Stetic.ItemDescriptor CreateItemDescriptor (XmlElement elem, Stetic.ItemGroup group)
		{
			if (elem.Name == "property") {
				ProjectPropertyInfo propInfo = classInfo.GetPropertyInfo (elem.GetAttribute ("name"));
				if (propInfo != null)
					return new ProjectPropertyDescriptor (elem, group, this, propInfo);
			}
			else if (elem.Name == "signal") {
				ProjectSignalInfo signalInfo = classInfo.GetSignalInfo (elem.GetAttribute ("name"));
				if (signalInfo != null)
					return new ProjectSignalDescriptor (elem, group, this, signalInfo);
			}
			else
				return base.CreateItemDescriptor (elem, group);

			return null;
		}
	}
	
	class CustomWidgetWrapper: Stetic.Wrapper.Container
	{
		Hashtable propertyData;
		
		public object GetProperty (string name)
		{
			if (propertyData == null)
				return null;
			else
				return propertyData [name];
		}
		
		public void SetProperty (string name, object value)
		{
			if (propertyData == null)
				propertyData = new Hashtable ();
			propertyData [name] = value;
			EmitNotify (name);
		}
	}


	[Serializable]
	class ProjectClassInfo
	{
		public string Name;
		string baseDescriptorName;
		string widgetDescText;
		
		[NonSerialized] XmlElement widgetDesc;
		[NonSerialized] Stetic.ClassDescriptor baseDescriptor;
		
		Hashtable properties;
		Hashtable events;
		
		public static ProjectClassInfo Create (IParserContext ctx, IClass cls, Stetic.ClassDescriptor baseDescriptor)
		{
			ProjectClassInfo pinfo = new ProjectClassInfo ();
			
			pinfo.Name = cls.FullyQualifiedName;
			pinfo.baseDescriptorName = baseDescriptor.Name;
			pinfo.baseDescriptor = baseDescriptor;

			pinfo.properties = new Hashtable ();
			pinfo.events = new Hashtable ();
		
			foreach (IProperty prop in cls.Properties) {
				ProjectPropertyInfo propInfo = ProjectPropertyInfo.Create (prop);
				if (propInfo != null)
					pinfo.properties [prop.Name] = propInfo;
			}
			
			foreach (IEvent evnt in cls.Events) {
				ProjectSignalInfo signalInfo = ProjectSignalInfo.Create (ctx, evnt);
				if (signalInfo != null)
					pinfo.events [evnt.Name] = signalInfo;
			}
			
			return pinfo;
		}
		
		public ProjectPropertyInfo GetPropertyInfo (string name) 
		{
			return properties [name] as ProjectPropertyInfo;
		}
		
		public ProjectSignalInfo GetSignalInfo (string name) 
		{
			return events [name] as ProjectSignalInfo;
		}
		
		public Stetic.ClassDescriptor BaseDescriptor {
			get {
				if (baseDescriptor == null)
					baseDescriptor = Stetic.Registry.LookupClassByName (baseDescriptorName);
				return baseDescriptor;
			}
		}
		
		public XmlElement WidgetDesc {
			get {
				if (widgetDesc == null && widgetDescText != null) {
					XmlDocument doc = new XmlDocument ();
					doc.LoadXml (widgetDescText);
					widgetDesc = doc.DocumentElement;
				}
				return widgetDesc;
			} set {
				widgetDesc = value;
				widgetDescText = widgetDesc.OuterXml;
			}
		}
	}
	
	[Serializable]
	class ProjectPropertyInfo
	{
		public Type PropType;
		public string Name;
		public bool CanWrite;
		public object InitialValue;

		public static ProjectPropertyInfo Create (IProperty property)
		{
			ProjectPropertyInfo pinfo = new ProjectPropertyInfo ();
			
			pinfo.Name = property.Name;
			pinfo.PropType = Stetic.Registry.GetType (property.ReturnType.FullyQualifiedName, false);
			if (pinfo.PropType == null) {
				Console.WriteLine ("Could not find type " + property.ReturnType.FullyQualifiedName);
				return null;
			}
				
			pinfo.CanWrite = property.CanSet;

			if (pinfo.PropType.IsValueType)
				pinfo.InitialValue = Activator.CreateInstance (pinfo.PropType);

			return pinfo;
		}
	}
	
	[Serializable]
	class ProjectSignalInfo
	{
		public string Name;
		public string HandlerTypeName;
		public string HandlerReturnTypeName;
		public Stetic.ParameterDescriptor[] HandlerParameters;
		
		public static ProjectSignalInfo Create (IParserContext ctx, IEvent evnt)
		{
			ProjectSignalInfo pinfo = new ProjectSignalInfo ();
			
			pinfo.Name = evnt.Name;
			
			IClass eventClass = ctx.GetClass (evnt.ReturnType.FullyQualifiedName);
			if (eventClass == null)
				return null;
			
			pinfo.HandlerTypeName = evnt.ReturnType.FullyQualifiedName;
			
			foreach (IMethod met in eventClass.Methods) {
				if (met.Name == "Invoke") {
					pinfo.HandlerReturnTypeName = met.ReturnType.FullyQualifiedName;
					pinfo.HandlerParameters = new Stetic.ParameterDescriptor [met.Parameters.Count];
					for (int n = 0; n < met.Parameters.Count; n++) {
						IParameter p = met.Parameters [n];
						pinfo.HandlerParameters [n] = new Stetic.ParameterDescriptor (p.Name, p.ReturnType.FullyQualifiedName);
					}
				}
			}
			if (pinfo.HandlerParameters == null)
				return null;
			return pinfo;
		}
	}
}

