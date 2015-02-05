// 
// CompiledTemplate.cs
//  
// Author:
//       Nathan Baulch <nathan.baulch@gmail.com>
// 
// Copyright (c) 2009 Nathan Baulch
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
using System.Reflection;
using Microsoft.VisualStudio.TextTemplating;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Collections.Generic;

namespace Mono.TextTemplating
{
	public sealed class CompiledTemplate : MarshalByRefObject, IDisposable
	{
		ITextTemplatingEngineHost host;
		object textTransformation;
		readonly CultureInfo culture;
		readonly string[] assemblyFiles;
		
		public CompiledTemplate (ITextTemplatingEngineHost host, CompilerResults results, string fullName, CultureInfo culture,
			string[] assemblyFiles)
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveReferencedAssemblies;
			this.host = host;
			this.culture = culture;
			this.assemblyFiles = assemblyFiles;
			Load (results, fullName);
		}
		
		void Load (CompilerResults results, string fullName)
		{
			var assembly = results.CompiledAssembly;
			Type transformType = assembly.GetType (fullName);
			//MS Templating Engine does not look on the type itself, 
			//it checks only that required methods are exists in the compiled type 
			textTransformation = Activator.CreateInstance (transformType);
			
			//set the host property if it exists
			Type hostType = null;
			var gen = host as TemplateGenerator;
			if (gen != null) {
				hostType = gen.SpecificHostType;
			}
			var hostProp = transformType.GetProperty ("Host", hostType ?? typeof(ITextTemplatingEngineHost));
			if (hostProp != null && hostProp.CanWrite)
				hostProp.SetValue (textTransformation, host, null);
			
			var sessionHost = host as ITextTemplatingSessionHost;
			if (sessionHost != null) {
				//FIXME: should we create a session if it's null?
				var sessionProp = transformType.GetProperty ("Session", typeof (IDictionary<string, object>));
				sessionProp.SetValue (textTransformation, sessionHost.Session, null);
			}
		}

		public string Process ()
		{
			var ttType = textTransformation.GetType ();

			var errorProp = ttType.GetProperty ("Errors", BindingFlags.Instance | BindingFlags.NonPublic);
			if (errorProp == null)
				throw new ArgumentException ("Template must have 'Errors' property");
			var errorMethod = ttType.GetMethod ("Error",new Type[]{typeof(string)});
			if (errorMethod == null) {
				throw new ArgumentException ("Template must have 'Error(string message)' method");
			}

			var errors = (CompilerErrorCollection) errorProp.GetValue (textTransformation);
			errors.Clear ();
			
			//set the culture
			if (culture != null)
				ToStringHelper.FormatProvider = culture;
			else
				ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;

			string output = null;

			var initMethod = ttType.GetMethod ("Initialize");
			var transformMethod = ttType.GetMethod ("TransformText");

			if (initMethod == null) {
				errorMethod.Invoke (textTransformation, new object[]{ "Error running transform: no method Initialize()" });
			} else if (transformMethod == null) {
				errorMethod.Invoke (textTransformation, new object[]{ "Error running transform: no method TransformText()" });
			} else try {
				initMethod.Invoke (textTransformation, null);
				output = (string)transformMethod.Invoke (textTransformation, null);
			} catch (Exception ex) {
				errorMethod.Invoke (textTransformation, new object[]{ "Error running transform: " + ex });
			}

			host.LogErrors (errors);
			
			ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;
			return output;
		}
		
		Assembly ResolveReferencedAssemblies (object sender, ResolveEventArgs args)
		{
			Assembly asm = null;
			foreach (var asmFile in assemblyFiles) {
				var name = System.IO.Path.GetFileNameWithoutExtension (asmFile);
				if (args.Name.StartsWith (name, StringComparison.Ordinal))
					asm = Assembly.LoadFrom (asmFile);
			}
			return asm;
		}
		
		public void Dispose ()
		{
			if (host != null) {
				host = null;
				AppDomain.CurrentDomain.AssemblyResolve -= ResolveReferencedAssemblies;
			}
		}
	}
}
