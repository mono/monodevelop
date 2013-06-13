// 
// MonoDocDocumentationProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Xml;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.Documentation;
using System.Text;

namespace MonoDevelop.Ide.TypeSystem
{
	[Serializable]
	public class MonoDocDocumentationProvider : IDocumentationProvider
	{
		[NonSerialized]
		bool hadError;
		
		public MonoDocDocumentationProvider ()
		{
		}

		#region IDocumentationProvider implementation
		[NonSerialized]
		readonly Dictionary<string, DocumentationComment> commentCache = new Dictionary<string, DocumentationComment> ();

		public DocumentationComment GetDocumentation (IEntity entity)
		{
			if (entity == null)
				throw new System.ArgumentNullException ("entity");
			
			// If we had an exception while getting the help xml the monodoc help provider
			// shouldn't try it again. A corrupt .zip file could cause long tooltip delays otherwise.
			if (hadError)
				return null;
			var idString = entity.GetIdString ();
			DocumentationComment result;
			if (commentCache.TryGetValue (idString, out result))
				return result;
			XmlDocument doc = null;
			try {
				var helpTree = MonoDevelop.Projects.HelpService.HelpTree;
				if (helpTree == null)
					return null;
				if (entity.EntityType == EntityType.TypeDefinition) {
					doc = helpTree.GetHelpXml (idString);
				} else {
					var parentId = entity.DeclaringTypeDefinition.GetIdString ();

					doc = helpTree.GetHelpXml (parentId);
					if (doc == null)
						return null;
					XmlNode node = SelectNode (doc, entity);
					
					if (node != null)
						return commentCache [idString] = new DocumentationComment (node.OuterXml, new SimpleTypeResolveContext (entity));
					return null;
//					var node = doc.SelectSingleNode ("/Type/Members/Member")
//					return new DocumentationComment (doc.OuterXml, new SimpleTypeResolveContext (entity));
				}
			} catch (Exception e) {
				hadError = true;
				LoggingService.LogError ("Error while reading monodoc file.", e);
			}
			if (doc == null) {
				commentCache [idString] = null;
				return null;
			}
			return commentCache [idString] = new DocumentationComment (doc.OuterXml, new SimpleTypeResolveContext (entity));
		}

		public XmlNode SelectNode (XmlDocument doc, IEntity entity)
		{
			switch (entity.EntityType) {
			case EntityType.None:
			case EntityType.TypeDefinition:
			case EntityType.Field:
			case EntityType.Property:
			case EntityType.Indexer:
			case EntityType.Event:
				return doc.SelectSingleNode ("/Type/Members/Member[@MemberName='" + entity.Name + "']");
			
			case EntityType.Method:
			case EntityType.Operator:
			case EntityType.Destructor:
				return SelectOverload (doc.SelectNodes ("/Type/Members/Member[@MemberName='" + entity.Name + "']"), (IParameterizedMember)entity);
			case EntityType.Constructor:
				return SelectOverload (doc.SelectNodes ("/Type/Members/Member[@MemberName='.ctor']"), (IParameterizedMember)entity);
				
			default:
				throw new ArgumentOutOfRangeException ();
			}

		}
		public XmlNode SelectOverload (XmlNodeList nodes, IParameterizedMember entity)
		{
			XmlNode node = null;
			if (nodes.Count == 1) {
				node = nodes [0];
			} else {
				var p = entity.Parameters;
				foreach (XmlNode curNode in nodes) {
					var paramList = curNode.SelectNodes ("Parameters/*");
					if (p.Count == 0 && paramList.Count == 0) 
						return curNode;
					if (p.Count != paramList.Count) 
						continue;
					bool matched = true;
					for (int i = 0; i < p.Count; i++) {
						var idString = GetTypeString (p [i].Type);
						if (idString != paramList [i].Attributes ["Type"].Value) {
							matched = false;
							break;
						}
					}
					if (matched) {
						return curNode;
					}
				}
			}
			if (node != null) {
				System.Xml.XmlNode result = node.SelectSingleNode ("Docs");
				return result;
			}
			return null;
		}
		

		static string GetTypeString (IType t)
		{
			if (t.Kind == TypeKind.Unknown)
				return t.Name;
			
			if (t.Kind == TypeKind.TypeParameter)
				return t.FullName;
			
			var typeWithElementType = t as TypeWithElementType;
			if (typeWithElementType != null) {
				var sb = new StringBuilder ();
			
				if (typeWithElementType is PointerType) {
					sb.Append ("*");
				} 
				sb.Append (GetTypeString (typeWithElementType.ElementType));

				if (typeWithElementType is ArrayType) {
					sb.Append ("[");
					sb.Append (new string (',', ((ArrayType)t).Dimensions - 1));
					sb.Append ("]");
				}
				return sb.ToString ();
			}
			
			ITypeDefinition typeDef = t.GetDefinition ();
			if (typeDef == null)
				return "";
			
			var result = new StringBuilder ();

			result.Append (typeDef.Namespace + ".");
			
			if (typeDef.DeclaringTypeDefinition != null) {
				string typeString = GetTypeString (typeDef.DeclaringTypeDefinition);
				result.Append (typeString);
				result.Append (".");
			}

			result.Append (typeDef.Name);

			if (typeDef.TypeParameterCount > 0) {
				result.Append ("<");
				for (int i = 0; i < typeDef.TypeParameterCount; i++) {
					if (i > 0)
						result.Append (",");
					if (t.TypeArguments.Count > 0) {
						result.Append (GetTypeString (t.TypeArguments [i]));
					} else {
						result.Append (typeDef.TypeParameters [i].FullName);
					}
				}
				result.Append (">");
			}
			
			return result.ToString ();
		}
		
		#endregion
		
		
	}
}

