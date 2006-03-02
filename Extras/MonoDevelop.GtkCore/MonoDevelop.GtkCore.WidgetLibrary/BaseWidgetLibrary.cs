//
// BaseWidgetLibrary.cs
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
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Projects;	
using MonoDevelop.Projects.Parser;	
using MonoDevelop.Ide.Gui;	

namespace MonoDevelop.GtkCore.WidgetLibrary
{
	public abstract class BaseWidgetLibrary: Stetic.WidgetLibrary
	{
		protected abstract XmlDocument GetObjectsDocument ();
		
		protected abstract IParserContext GetParserContext ();
		
		public override void Load ()
		{
			XmlDocument doc = GetObjectsDocument ();
			if (doc != null)
				Load (doc);
		}
		
		protected override Stetic.ClassDescriptor LoadClassDescriptor (XmlElement element)
		{
			string name = element.GetAttribute ("type");
			ProjectClassInfo cinfo;
			
			// Read the class info from the parser service
			IParserContext ctx = GetParserContext ();
			IClass cls = ctx.GetClass (name, true, true);
			if (cls == null)
				return null;
			
			// Find the nearest type that can be loaded
			Stetic.ClassDescriptor typeClassDescriptor = FindType (ctx, cls);

			if (typeClassDescriptor == null) {
				Console.WriteLine ("Descriptor not found: " + cls.Name);
				return null;
			}
			
			cinfo = ProjectClassInfo.Create (ctx, cls, typeClassDescriptor);
			if (cinfo == null)
				return null;
				
			return new ProjectClassDescriptor (element, cinfo);
		}
		
		Stetic.ClassDescriptor FindType (IParserContext ctx, IClass cls)
		{
			foreach (string baseType in cls.BaseTypes) {
				IClass bc = ctx.GetClass (baseType);
				if (bc == null)
					continue;
				if (bc.ClassType == ClassType.Interface)
					continue;

				Stetic.ClassDescriptor klass = Stetic.Registry.LookupClassByName (baseType);
				if (klass != null) return klass;
				
				klass = FindType (ctx, bc);
				if (klass != null) 
					return klass;
			}
			return null;
		}
		
		public abstract string AssemblyPath {
			get;
		}
	}
	
}
