//
// Ambience.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.Ide.TypeSystem
{
	public abstract class Ambience
	{
		public string Name {
			get;
			private set;
		}

		
		public Ambience (string name)
		{
			this.Name = name;
		}
		
		#region To implement
		public abstract string GetIntrinsicTypeName (string reflectionName);
		
		public abstract string SingleLineComment (string text);
		public abstract string GetString (string nameSpace, OutputSettings settings);
		
		protected abstract string GetTypeReferenceString (IType reference, OutputSettings settings);
		protected abstract string GetTypeString (IType type, OutputSettings settings);
		protected abstract string GetMethodString (IMethod method, OutputSettings settings);
		protected abstract string GetConstructorString (IMethod constructor, OutputSettings settings);
		protected abstract string GetDestructorString (IMethod destructor, OutputSettings settings);
		protected abstract string GetOperatorString (IMethod op, OutputSettings settings);
		
		protected abstract string GetFieldString (IField field, OutputSettings settings);
		protected abstract string GetEventString (IEvent evt, OutputSettings settings);
		protected abstract string GetPropertyString (IProperty property, OutputSettings settings);
		protected abstract string GetIndexerString (IProperty property, OutputSettings settings);

		protected abstract string GetParameterString (IParameterizedMember member, IParameter parameter, OutputSettings settings);

		#endregion
		
		public virtual TooltipInformation GetTooltip (IEntity entity)
		{
			return null;
		}

		public static string Format (string str)
		{
			if (String.IsNullOrEmpty (str))
				return string.Empty;
			
			StringBuilder sb = new StringBuilder (str.Length);
			MarkupUtilities.AppendEscapedString (sb, str);
			return sb.ToString (); 
		}
		
		protected static OutputFlags GetFlags (object settings)
		{
			if (settings is OutputFlags)
				return (OutputFlags)settings;
			return ((OutputSettings)settings).OutputFlags;
		}
		
		protected static OutputSettings GetSettings (object settings)
		{
			if (settings is OutputFlags)
				return new OutputSettings ((OutputFlags)settings);
			return (OutputSettings)settings;
		}
		
		public string GetString (string nameSpace, OutputFlags flags)
		{
			return GetString (nameSpace, new OutputSettings (flags));
		}
		
		public string GetString (IEntity entity, OutputSettings settings)
		{
			if (entity == null) {
				string[] trace = Environment.StackTrace.Split (new [] { Environment.NewLine }, StringSplitOptions.None);
				return "null entity: " + (trace != null && trace.Length > 2 ? trace [2] : "unknown location");
			}
			string result = null;
			switch (entity.EntityType) {
			case EntityType.Constructor:
				result = GetConstructorString ((IMethod)entity, settings);
				break;
			case EntityType.Destructor:
				result = GetDestructorString ((IMethod)entity, settings);
				break;
			case EntityType.Event:
				result = GetEventString ((IEvent)entity, settings);
				break;
			case EntityType.Field:
				result = GetFieldString ((IField)entity, settings);
				break;
			case EntityType.Indexer:
				result = GetPropertyString ((IProperty)entity, settings);
				break;
			case EntityType.Method:
				result = GetMethodString ((IMethod)entity, settings);
				break;
			case EntityType.Operator:
				result = GetMethodString ((IMethod)entity, settings);
				break;
			case EntityType.Property:
				result = GetPropertyString ((IProperty)entity, settings);
				break;
			case EntityType.TypeDefinition:
				result = GetTypeString ((ITypeDefinition)entity, settings);
				break;
			default:
				throw new ArgumentOutOfRangeException ("EntityType", "Unknown entity type:" + entity.EntityType);
			}
			result = settings.PostProcess (entity, result);
			return result;
		}
		
		public string GetString (IType type, OutputSettings settings)
		{
			var result = GetTypeString (type, settings);
			return settings.PostProcess (type, result);
		}
	
/*		public string GetString (ITypeReference reference, OutputSettings settings)
		{
			var result = GetTypeReferenceString (reference, settings);
			return settings.PostProcess (reference, result);
		}*/
		
		public string GetString (IEntity entity, OutputFlags flags)
		{
			return GetString (entity, new OutputSettings (flags));
		}
		
		public string GetString (IType type, OutputFlags flags)
		{
			return GetString (type, new OutputSettings (flags));
		}
		
		public string GetString (ITypeDefinition type, OutputFlags flags)
		{
			return GetString ((IEntity)type, new OutputSettings (flags));
		}
		
		
		public string GetString (IParameterizedMember member, IParameter parameter, OutputFlags flags)
		{
			return GetParameterString (member, parameter, new OutputSettings (flags));
		}
		/*
		public string GetString (ITypeReference reference, OutputFlags flags)
		{
			return GetString (reference, new OutputSettings (flags));
		}*/
	}
}
