//
// IToolboxLoader.cs: Interface for classes that can load toolbox 
//    nodes from a file.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public interface IToolboxLoader
	{
		//comma-separated extensions
		string [] FileTypes {
			get;
		}
		
		System.Collections.Generic.IList<ItemToolboxNode> Load (LoaderContext context, string filename);
	}
	
	public class LoaderContext
	{
		Hashtable values = new Hashtable ();
		Dictionary<TargetRuntime, ExternalLoader> externalLoaders;
		int counter;
		
		public T CreateExternalLoader<T> (TargetRuntime runtime) where T:MarshalByRefObject
		{
			if (externalLoaders == null)
				externalLoaders = new Dictionary<TargetRuntime, ExternalLoader> ();
			
			ExternalLoader eloader;
			if (!externalLoaders.TryGetValue (runtime, out eloader)) {
				eloader = (ExternalLoader) Runtime.ProcessService.CreateExternalProcessObject (typeof(ExternalLoader), runtime);
				externalLoaders [runtime] = eloader;
				values [counter++] = eloader;
			} else {
				try {
					eloader.Ping ();
				} catch {
					// The remote process is dead. Try creating a new one.
					eloader.Dispose (); // Calling Dispose is always safe
					externalLoaders.Remove (runtime);
					return CreateExternalLoader<T> (runtime);
				}
			}
			
			return (T) eloader.CreateInstance (typeof(T));
		}
		
		public IList<ItemToolboxNode> LoadItemsIsolated (TargetRuntime runtime, Type loaderType, string filename)
		{
			if (!typeof(IExternalToolboxLoader).IsAssignableFrom (loaderType))
				throw new InvalidOperationException ("Type '" + loaderType + "' does not implement 'IExternalToolboxLoader'");
			try {
				ExternalItemLoader loader = CreateExternalLoader<ExternalItemLoader> (runtime);
				string s = loader.LoadItems (loaderType.Assembly.FullName, loaderType.FullName, filename);
				XmlDataSerializer ser = new XmlDataSerializer (MonoDevelop.Projects.Services.ProjectService.DataContext);
				ToolboxList list = (ToolboxList) ser.Deserialize (new StringReader (s), typeof(ToolboxList));
				return list;
			} catch {
				return new List<ItemToolboxNode> ();
			}
		}
		
		public object this [object key] {
			get {
				return values [key];
			}
			set {
				values [key] = value;
			}
		}
		
		internal void Dispose ()
		{
			foreach (object ob in values.Values) {
				if (ob is IDisposable) {
					try {
						((IDisposable)ob).Dispose ();
					} catch {
						// Ignore
					}
				}
			}
		}
	}
	
	public interface IExternalToolboxLoader
	{
		IList<ItemToolboxNode> Load (string filename);
	}
	
	[MonoDevelop.Core.Execution.AddinDependency ("MonoDevelop.DesignerSupport")] 
	class ExternalLoader: RemoteProcessObject
	{
		public void Ping ()
		{
		}
		
		public object CreateInstance (Type type)
		{
			return Activator.CreateInstance (type);
		}
	}
	
	class ExternalItemLoader: MarshalByRefObject
	{
		public string LoadItems (string asmName, string typeName, string fileName)
		{
			XmlDataSerializer ser = new XmlDataSerializer (MonoDevelop.Projects.Services.ProjectService.DataContext);
			ToolboxList tl = new ToolboxList ();
			object ob = Activator.CreateInstance (asmName, typeName).Unwrap ();
			IExternalToolboxLoader loader = (IExternalToolboxLoader) ob;
			IList<ItemToolboxNode> list = loader.Load (fileName);
			tl.AddRange (list);
			StringWriter sw = new StringWriter ();
			ser.Serialize (sw, tl);
			return sw.ToString ();
		}
	}
}
