// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Xml;

using MonoDevelop.Services;
using MonoDevelop.SharpAssembly.Metadata.Rows;
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.PE;
using SharpAssembly_=MonoDevelop.SharpAssembly.Assembly.SharpAssembly;
using AssemblyReader=MonoDevelop.SharpAssembly.Assembly.AssemblyReader;
using SharpCustomAttribute=MonoDevelop.SharpAssembly.Assembly.SharpCustomAttribute;

namespace MonoDevelop.Internal.Parser {
	
	[Serializable]
	public class SharpAssemblyEvent : AbstractEvent
	{
		public SharpAssemblyEvent(SharpAssembly_ asm, Event[] eventTable, SharpAssemblyClass declaringtype, uint index)
		{
			if (asm == null) {
				throw new System.ArgumentNullException("asm");
			}
			if (eventTable == null) {
				throw new System.ArgumentNullException("eventTable");
			}
			if (declaringtype == null) {
				throw new System.ArgumentNullException("declaringtype");
			}
			if (index > eventTable.GetUpperBound(0) || index < 1) {
				throw new System.ArgumentOutOfRangeException("index", index, String.Format("must be between 1 and {0}!", eventTable.GetUpperBound(0)));
			}
			
			AssemblyReader assembly = asm.Reader;
			
			declaringType = declaringtype;
			
			Event evt = eventTable[index];
			string name = assembly.GetStringFromHeap(evt.Name);
			FullyQualifiedName = String.Concat(declaringType.FullyQualifiedName, ".", name);
			
			MethodSemantics[] sem = asm.Tables.MethodSemantics;
			Method[] method       = asm.Tables.Method;
			if (sem == null) goto nosem;
			
			for (int i = 1; i <= sem.GetUpperBound(0); ++i) {
				uint table = sem[i].Association & 1;
				uint ident = sem[i].Association >> 1;
				
				if (table == 0 && ident == index) {  // table: Event
					modifiers = ModifierEnum.None;
					Method methodDef = method[sem[i].Method];
			
					if (methodDef.IsFlagSet(Method.FLAG_STATIC)) {
						modifiers |= ModifierEnum.Static;
					}
					
					if (methodDef.IsMaskedFlagSet(Method.FLAG_PRIVATE, Method.FLAG_MEMBERACCESSMASK)) { // I assume that private is used most and public last (at least should be)
						modifiers |= ModifierEnum.Private;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMILY, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Protected;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_PUBLIC, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Public;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_ASSEM, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Internal;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMORASSEM, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.ProtectedOrInternal;
					} else if (methodDef.IsMaskedFlagSet(Method.FLAG_FAMANDASSEM, Method.FLAG_MEMBERACCESSMASK)) {
						modifiers |= ModifierEnum.Protected;
						modifiers |= ModifierEnum.Internal;
					}
					
					if ((sem[i].Semantics & MethodSemantics.SEM_ADDON) == MethodSemantics.SEM_ADDON) {
						addMethod = new SharpAssemblyMethod(asm, method, declaringtype, sem[i].Method);
					}
					
					if ((sem[i].Semantics & MethodSemantics.SEM_REMOVEON) == MethodSemantics.SEM_REMOVEON) {
						removeMethod = new SharpAssemblyMethod(asm, method, declaringtype, sem[i].Method);
					}

					if ((sem[i].Semantics & MethodSemantics.SEM_FIRE) == MethodSemantics.SEM_FIRE) {
						raiseMethod = new SharpAssemblyMethod(asm, method, declaringtype, sem[i].Method);
					}
				}
				
			}
			
		nosem:
			
			// Attributes
			ArrayList attrib = asm.Attributes.Event[index] as ArrayList;
			if (attrib == null) goto noatt;
			
			AbstractAttributeSection sect = new AbstractAttributeSection();
			
			foreach(SharpCustomAttribute customattribute in attrib) {
				sect.Attributes.Add(new SharpAssemblyAttribute(asm, customattribute));
			}
			
			attributes.Add(sect);
		
		noatt:
			
			if ((evt.EventFlags & Event.FLAG_SPECIALNAME) == Event.FLAG_SPECIALNAME) modifiers |= ModifierEnum.SpecialName;
			
			uint typtab = evt.EventType & 0x03;
			uint typid  = evt.EventType >> 2;
			
			if (typtab == 0) {        // TypeDef
				TypeDef[] typedef = (TypeDef[])assembly.MetadataTable.Tables[TypeDef.TABLE_ID];
				returnType = new SharpAssemblyReturnType(asm, typedef, typid);

			} else if (typtab == 1) { // TypeRef
				TypeRef[] typeref = (TypeRef[])assembly.MetadataTable.Tables[TypeRef.TABLE_ID];
				returnType = new SharpAssemblyReturnType(asm, typeref, typid);

			} else {                  // TypeSpec
				returnType = new SharpAssemblyReturnType("NOT_SUPPORTED");
				Runtime.LoggingService.Info("SharpAssemblyEvent: TypeSpec -- not supported");
			}
			
		}
		
		public override string ToString()
		{
			return FullyQualifiedName;
		}
	}
}
