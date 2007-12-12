using System;
using System.Reflection;
using System.Xml;

namespace Stetic
{
	[Serializable]
	public class TypedSignalDescriptor: SignalDescriptor
	{
		string gladeName;
		
		const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
		
		public TypedSignalDescriptor (XmlElement elem, ItemGroup group, TypedClassDescriptor klass) : base (elem, group, klass)
		{
			Load (elem);

			EventInfo eventInfo = FindEvent (klass.WrapperType, klass.WrappedType, name);
			MethodInfo handler = eventInfo.EventHandlerType.GetMethod ("Invoke");
			
			if (elem.HasAttribute ("glade-name"))
				gladeName = elem.GetAttribute ("glade-name");
			else {
				object[] att = eventInfo.GetCustomAttributes (typeof(GLib.SignalAttribute), true);
				if (att.Length > 0)
					gladeName = ((GLib.SignalAttribute)att[0]).CName;
			}
			
			handlerTypeName = eventInfo.EventHandlerType.FullName;
			handlerReturnTypeName = handler.ReturnType.FullName;
			
			ParameterInfo[] pars = handler.GetParameters ();
			handlerParameters = new ParameterDescriptor [pars.Length];
			for (int n=0; n<pars.Length; n++)
				handlerParameters [n] = new ParameterDescriptor (pars[n].Name, pars [n].ParameterType.FullName);
		}
		
		public string GladeName {
			get { return gladeName; }
		}

		static EventInfo FindEvent (Type wrapperType, Type objectType, string name)
		{
			EventInfo info;

			if (wrapperType != null) {
				info = wrapperType.GetEvent (name, flags);
				if (info != null)
					return info;
			}

			info = objectType.GetEvent (name, flags);
			if (info != null)
				return info;

			throw new ArgumentException ("Invalid event name " + objectType.Name + "." + name);
		}
	}
}
