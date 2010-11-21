using System;
using System.Collections;
using System.Xml;
using System.Reflection;
using Mono.Cecil;

namespace Stetic
{
	class CecilSignalDescriptor: Stetic.SignalDescriptor
	{
		public CecilSignalDescriptor (CecilWidgetLibrary lib, XmlElement elem, Stetic.ItemGroup group, Stetic.ClassDescriptor klass, EventDefinition sinfo) : base (elem, group, klass)
		{
			if (sinfo != null) {
				string handler = sinfo.EventType.FullName;
				handlerTypeName = handler.Replace ('/','+');
				Type t = Registry.GetType (handler, false);
				if (t != null) {
					MethodInfo mi = t.GetMethod ("Invoke");
					handlerReturnTypeName = mi.ReturnType.FullName;
					ParameterInfo[] pars = mi.GetParameters ();
					handlerParameters = new ParameterDescriptor [pars.Length];
					for (int n=0; n<pars.Length; n++)
						handlerParameters [n] = new ParameterDescriptor (pars[n].Name, pars[n].ParameterType.FullName);
				} else {
					// If the type is generic, the type arguments must be ignored when looking for the type 
					string tn = handler;
					int i = handler.IndexOf ('<');
					if (i != -1) {
						tn = handler.Substring (0, i);
						// Convert the type name to a type reference
						handler = handler.Replace ('<', '[');
						handler = handler.Replace ('>', ']');
					}
					TypeDefinition td = lib.FindTypeDefinition (tn);
					if (td != null) {
						MethodDefinition mi = null;
						foreach (MethodDefinition md in td.Methods) {
							if (md.Name == "Invoke") {
								mi = md;
								break;
							}
						}
						if (mi != null) {
							handlerReturnTypeName = CecilWidgetLibrary.GetInstanceType (td, sinfo.EventType, mi.ReturnType);
							handlerParameters = new ParameterDescriptor [mi.Parameters.Count];
							for (int n=0; n<handlerParameters.Length; n++) {
								ParameterDefinition par = mi.Parameters [n];
								handlerParameters [n] = new ParameterDescriptor (par.Name, CecilWidgetLibrary.GetInstanceType (td, sinfo.EventType, par.ParameterType));
							}
						}
					} else {
						handlerParameters = new ParameterDescriptor [0];
					}
				}
				SaveCecilXml (elem);
			}
			else {
				handlerTypeName = elem.GetAttribute ("handlerTypeName");
				handlerReturnTypeName = elem.GetAttribute ("handlerReturnTypeName");
				
				ArrayList list = new ArrayList ();
				foreach (XmlNode npar in elem.ChildNodes) {
					XmlElement epar = npar as XmlElement;
					if (epar == null) continue;
					list.Add (new ParameterDescriptor (epar.GetAttribute ("name"), epar.GetAttribute ("type")));
				}
				
				handlerParameters = (ParameterDescriptor[]) list.ToArray (typeof(ParameterDescriptor));
			}
			
			Load (elem);
		}

		internal void SaveCecilXml (XmlElement elem)
		{
			elem.SetAttribute ("handlerTypeName", handlerTypeName);
			elem.SetAttribute ("handlerReturnTypeName", handlerReturnTypeName);
			if (handlerParameters != null) {
				foreach (ParameterDescriptor par in handlerParameters) {
					XmlElement epar = elem.OwnerDocument.CreateElement ("param");
					epar.SetAttribute ("name", par.Name);
					epar.SetAttribute ("type", par.TypeName);
					elem.AppendChild (epar);
				}
			}
		}
	}
}
