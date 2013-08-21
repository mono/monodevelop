// 
// HelpService.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using Monodoc;
using System.Threading;
using MonoDevelop.Core;
using Mono.Addins;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Projects.Extensions;
using ICSharpCode.NRefactory.TypeSystem;
using System.Text;
using System.Xml;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Documentation;

namespace MonoDevelop.Projects
{
	public static class HelpService
	{
		static RootTree helpTree;
		static bool helpTreeInitialized;
		static object helpTreeLock = new object ();
		static HashSet<string> sources = new HashSet<string> ();
		
		/// <summary>
		/// Starts loading the MonoDoc tree in the background.
		/// </summary>
		public static void AsyncInitialize ()
		{
			lock (helpTreeLock) {
				if (helpTreeInitialized)
					return;
			}
			ThreadPool.QueueUserWorkItem (delegate {
				// Load the help tree asynchronously. Reduces startup time.
				InitializeHelpTree ();
			});
		}
		
		//FIXME: allow adding sources without restart when extension installed (will need to be async)
		// will also be tricky we cause we'll also have update any running MonoDoc viewer
		static void InitializeHelpTree ()
		{
			lock (helpTreeLock) {
				if (helpTreeInitialized)
					return;
				
				Counters.HelpServiceInitialization.BeginTiming ();
				
				try {
					helpTree = RootTree.LoadTree ();
					
					foreach (var node in AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/MonoDocSources"))
						sources.Add (((MonoDocSourceNode)node).Directory);
					
					//remove nonexistent sources
					foreach (var s in sources.ToList ().Where (d => !Directory.Exists (d)))
						sources.Remove (s);
					
					foreach (var s in sources)
						helpTree.AddSource (s);
							
				} catch (Exception ex) {
					if (!(ex is ThreadAbortException) && !(ex.InnerException is ThreadAbortException))
						LoggingService.LogError ("Monodoc documentation tree could not be loaded.", ex);
				} finally {
					helpTreeInitialized = true;
					Counters.HelpServiceInitialization.EndTiming ();
				}
			}
		}
		
		/// <summary>
		/// A MonoDoc docs tree.
		/// </summary>
		/// <remarks>
		/// The tree is background-loaded the help service, and accessing the property will block until it is finished 
		/// loading. If you don't wish to block, check the <see cref="TreeInitialized"/> property first.
		///  </remarks>
		public static RootTree HelpTree {
			get {
				lock (helpTreeLock) {
					if (!helpTreeInitialized)
						InitializeHelpTree ();
					return helpTree;
				}
			}
		}
		
		/// <summary>
		/// Whether the MonoDoc docs tree has finished loading.
		/// </summary>
		public static bool TreeInitialized {
			get {
				return helpTreeInitialized;
			}
		}
		
		public static IEnumerable<string> Sources {
			get { return sources; }
		}
		
		//note: this method is very careful to check that the generated URLs exist in MonoDoc
		//because if we send nonexistent URLS to MonoDoc, it shows empty pages
		public static string GetMonoDocHelpUrl (ResolveResult result)
		{
			if (result == null)
				return null;
			
//			if (result is AggregatedResolveResult) 
//				result = ((AggregatedResolveResult)result).PrimaryResult;
			
			
			if (result is NamespaceResolveResult) {
				string namespc = ((NamespaceResolveResult)result).NamespaceName;
				//verify that the namespace exists in the help tree
				//FIXME: GetHelpXml doesn't seem to work for namespaces, so forced to do full render
				Monodoc.Node dummy;
				if (!String.IsNullOrEmpty (namespc) && HelpTree != null && HelpTree.RenderUrl ("N:" + namespc, out dummy) != null)
					return "N:" + namespc;
				else
					return null;
			}
			
			IMember member = null;
//			if (result is MethodGroupResolveResult)
//				member = ((MethodGroupResolveResult)result).Methods.FirstOrDefault ();
//			else 
			if (result is MemberResolveResult)
				member = ((MemberResolveResult)result).Member;
			
			if (member != null && member.GetMonodocDocumentation () != null)
				return member.GetIdString ();
			
			var type = result.Type;
			if (type != null && !String.IsNullOrEmpty (type.FullName)) {
				string t = "T:" + type.FullName;
				try {
					var tree = HelpTree;
					if (tree != null && tree.GetHelpXml (t) != null)
						return t;
				} catch (Exception) {
					return null;
				}
			}
			
			return null;
		}
	}
	
	public static class HelpExtension
	{
		static void AppendTypeReference (StringBuilder result, ITypeReference type)
		{
			if (type is ArrayTypeReference) {
				var array = (ArrayTypeReference)type;
				AppendTypeReference (result, array.ElementType);
				result.Append ("[");
				result.Append (new string (',', array.Dimensions));
				result.Append ("]");
				return;
			}
			
			if (type is PointerTypeReference) {
				var ptr = (PointerTypeReference)type;
				AppendTypeReference (result, ptr.ElementType);
				result.Append ("*");
				return;
			}
			
			if (type is IType)
				result.Append (((IType)type).FullName);
		}
		
		
		static void AppendHelpParameterList (StringBuilder result, IList<IParameter> parameters)
		{
			result.Append ('(');
			if (parameters != null) {
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0)
						result.Append (',');
					var p = parameters [i];
					if (p == null)
						continue;
					if (p.IsRef || p.IsOut)
						result.Append ("&");
					AppendTypeReference (result, p.Type.ToTypeReference ());
				}
			}
			result.Append (')');
		}
		
		static void AppendHelpParameterList (StringBuilder result, IList<IUnresolvedParameter> parameters)
		{
			result.Append ('(');
			if (parameters != null) {
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0)
						result.Append (',');
					var p = parameters [i];
					if (p == null)
						continue;
					if (p.IsRef || p.IsOut)
						result.Append ("&");
					AppendTypeReference (result, p.Type);
				}
			}
			result.Append (')');
		}
		
		static XmlNode FindMatch (IMethod method, XmlNodeList nodes)
		{
			foreach (XmlNode node in nodes) {
				XmlNodeList paramList = node.SelectNodes ("Parameters/*");
				if (method.Parameters.Count == 0 && paramList.Count == 0) 
					return node;
				if (method.Parameters.Count != paramList.Count) 
					continue;
				
/*				bool matched = true;
				for (int i = 0; i < p.Count; i++) {
					if (p [i].ReturnType.FullName != paramList [i].Attributes ["Type"].Value) {
						matched = false;
						break;
					}
				}
				if (matched)*/
					return node;
			}
			return null;
		}
		
		public static XmlNode GetMonodocDocumentation (this IEntity member)
		{
			if (member.SymbolKind == SymbolKind.TypeDefinition) {
				var helpXml = HelpService.HelpTree != null ? HelpService.HelpTree.GetHelpXml (member.GetIdString ()) : null;
				if (helpXml == null)
					return null;
				return helpXml.SelectSingleNode ("/Type/Docs");
			}
			
			var declaringXml = HelpService.HelpTree != null && member.DeclaringTypeDefinition != null ? HelpService.HelpTree.GetHelpXml (member.DeclaringTypeDefinition.GetIdString ()) : null;
			if (declaringXml == null)
				return null;
			
			switch (member.SymbolKind) {
			case SymbolKind.Method: {
					var nodes = declaringXml.SelectNodes ("/Type/Members/Member[@MemberName='" + member.Name + "']");
					XmlNode node = nodes.Count == 1 ? nodes [0] : FindMatch ((IMethod)member, nodes);
					if (node != null) {
						System.Xml.XmlNode result = node.SelectSingleNode ("Docs");
						return result;
					}
					return null;
				}
			case SymbolKind.Constructor: {
					var nodes = declaringXml.SelectNodes ("/Type/Members/Member[@MemberName='.ctor']");
					XmlNode node = nodes.Count == 1 ? nodes [0] : FindMatch ((IMethod)member, nodes);
					if (node != null) {
						System.Xml.XmlNode result = node.SelectSingleNode ("Docs");
						return result;
					}
					return null;
				}
			default:
				return declaringXml.SelectSingleNode ("/Type/Members/Member[@MemberName='" + member.Name + "']/Docs");
			}
		}
		
	}
}

