 /* 
 * EventBindingService.cs - handles binding of Control events to CodeBehind methods
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.ComponentModel.Design;
using System.ComponentModel;
using System.Collections;
using System.Reflection;
using System.Globalization;

namespace AspNetEdit.Editor.ComponentModel
{
	public class EventBindingService : IEventBindingService
	{
		//TODO: Remove once we get code view
		Gtk.Window parentWindow;
		
		public EventBindingService (Gtk.Window parentWindow)
		{
			this.parentWindow = parentWindow;
		}
		
		//TODO: Remove after getting code view
		private void ShowMessage (string mess)
		{
			Gtk.MessageDialog md = new Gtk.MessageDialog (parentWindow, Gtk.DialogFlags.DestroyWithParent, Gtk.MessageType.Info, Gtk.ButtonsType.Close, mess);
			md.Run ();
			md.Destroy ();
		}				
		#region IEventBindingService Members
		
		private Hashtable eventHandlers = new Hashtable ();
		
		public string CreateUniqueMethodName (IComponent component, EventDescriptor e)
		{
			if (component.Site == null || component.Site.Name == null)
				throw new ArgumentException ("IComponent must be sited and named");
			
			//TODO: check component.Site.Name is valid as start of method name
			string trialPrefix = component.Site.Name + "_" + e.Name;
			string trialValue = trialPrefix;
			
			for (int suffix = 1; suffix <= int.MaxValue; suffix++)
			{
				if (!eventHandlers.ContainsKey (trialValue))
					return trialValue;
				
				trialValue = trialPrefix + suffix.ToString ();
			}
			
			throw new Exception ("Tried method names up to " + trialValue + " and all already existed");
		}

		public System.Collections.ICollection GetCompatibleMethods (System.ComponentModel.EventDescriptor e)
		{
			ParameterInfo[] pi = GetEventParameters (e);
			
			ArrayList arr = new ArrayList ();
			
			foreach (DictionaryEntry de in eventHandlers)
				if ( (ParameterInfo[]) de.Value == pi)
					arr.Add (de.Key); 
			
			return arr.ToArray (typeof(string));
		}

		public System.ComponentModel.EventDescriptor GetEvent (System.ComponentModel.PropertyDescriptor property)
		{
			EventPropertyDescriptor epd = property as EventPropertyDescriptor;
			if (epd == null)
				return null;
			
			return epd.InternalEventDescriptor;
		}

		public System.ComponentModel.PropertyDescriptorCollection GetEventProperties (System.ComponentModel.EventDescriptorCollection events)
		{
			ArrayList props = new ArrayList ();
			
			foreach (EventDescriptor e in events)
				props.Add (GetEventProperty (e));
				
			return new PropertyDescriptorCollection ((PropertyDescriptor[]) props.ToArray (typeof (PropertyDescriptor)));
		}

		public System.ComponentModel.PropertyDescriptor GetEventProperty (System.ComponentModel.EventDescriptor e)
		{
			if (e == null) throw new ArgumentNullException ("e");
			return new EventPropertyDescriptor (e);
		}

		//TODO: actually show code and return true if exists
		public bool ShowCode (System.ComponentModel.IComponent component, System.ComponentModel.EventDescriptor e)
		{
			PropertyDescriptor pd = GetEventProperty (e);
			string name = (string) pd.GetValue (component);
			
			if (name == null) {
				name = CreateUniqueMethodName (component, e);
				pd.SetValue (component, name);
			}
			
			if (eventHandlers.ContainsKey (name))
				ShowMessage("In an IDE this would show the existing CodeBehind method \"" + name + "\".");
			else {
				eventHandlers.Add (name, GetEventParameters (e) );
				ShowMessage("In an IDE this would create and show the CodeBehind method \"" + name + "\".");
			}
			
			return true;
		}

		//TODO: actually show code and return true if exists
		public bool ShowCode (int lineNumber)
		{
			ShowMessage ("In an IDE this would show the code at line " + lineNumber.ToString ());
			return true;
		}

		public bool ShowCode ()
		{
			return ShowCode (0);
		}

		#endregion
		
		private ParameterInfo[] GetEventParameters (EventDescriptor e)
		{
			MethodInfo mi = e.EventType.GetMethod ("Invoke");
			return mi.GetParameters ();
		}
	}

	internal class EventPropertyDescriptor : PropertyDescriptor
	{
		private EventDescriptor eDesc;
		private TypeConverter tc;
	
		public EventPropertyDescriptor (EventDescriptor eDesc)
			: base (eDesc)
		{
			this.eDesc = eDesc;
		}
		
		public override bool CanResetValue (object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get { return eDesc.ComponentType; }
		}

		public override object GetValue(object component)
		{
			IDictionaryService dict = GetDictionaryService (component);
			return dict.GetValue (base.Name) as string;
		}

		public override bool IsReadOnly
		{
			get { return false; }
		}

		public override Type PropertyType
		{
			get { return eDesc.EventType; }
		}

		public override void ResetValue (object component)
		{
			SetValue (component, null);
		}

		public override void SetValue (object component, object value)
		{
			IDictionaryService dict = GetDictionaryService (component);
			dict.SetValue (base.Name, value);
		}

		public override bool ShouldSerializeValue (object component)
		{
			if (GetValue (component) == null) return false;
			return true;
		}
		
		internal static IDictionaryService GetDictionaryService (object component)
		{
			if (component == null)
				throw new ArgumentNullException ("component");
			IComponent comp = component as IComponent;
			if (comp == null || comp.Site == null)
				throw new ArgumentException ("component must be a sited IComponent", "component");
				
			IDictionaryService dict = comp.Site.GetService (typeof (IDictionaryService)) as IDictionaryService;
			if (dict == null)
				throw new InvalidOperationException ("could not obtain IDictionaryService implementation");
				
			return dict;
		}
		
		public override TypeConverter Converter {
			get {
				if (tc == null)
					tc = TypeDescriptor.GetConverter (string.Empty);
				return tc;
			}
		}
		
		internal EventDescriptor InternalEventDescriptor {
			get { return eDesc; }
		}
	}
}
