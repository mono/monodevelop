//
// ConditionType.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using Mono.Addins.Description;
using System.Collections;

namespace Mono.Addins
{
	public abstract class ConditionType
	{
		internal event EventHandler Changed;
		string id;
		
		public abstract bool Evaluate (NodeElement conditionNode);
		
		public void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		internal string Id {
			get { return id; }
			set { id = value; }
		}
	}
	
	internal class BaseCondition
	{
		BaseCondition parent;
		
		internal BaseCondition (BaseCondition parent)
		{
			this.parent = parent;
		}
		
		public virtual bool Evaluate (ExtensionContext ctx)
		{
			return parent == null || parent.Evaluate (ctx);
		}
		
		internal virtual void GetConditionTypes (ArrayList listToFill)
		{
		}
	}
	
	internal class NullCondition: BaseCondition
	{
		public NullCondition (): base (null)
		{
		}
		
		public override bool Evaluate (ExtensionContext ctx)
		{
			return false;
		}
	}
	
	class OrCondition: BaseCondition
	{
		BaseCondition[] conditions;
		
		public OrCondition (BaseCondition[] conditions, BaseCondition parent): base (parent)
		{
			this.conditions = conditions;
		}
		
		public override bool Evaluate (ExtensionContext ctx)
		{
			if (!base.Evaluate (ctx))
				return false;
			foreach (BaseCondition cond in conditions)
				if (cond.Evaluate (ctx))
					return true;
			return false;
		}
		
		internal override void GetConditionTypes (ArrayList listToFill)
		{
			foreach (BaseCondition cond in conditions)
				cond.GetConditionTypes (listToFill);
		}
	}
	
	class AndCondition: BaseCondition
	{
		BaseCondition[] conditions;
		
		public AndCondition (BaseCondition[] conditions, BaseCondition parent): base (parent)
		{
			this.conditions = conditions;
		}
		
		public override bool Evaluate (ExtensionContext ctx)
		{
			if (!base.Evaluate (ctx))
				return false;
			foreach (BaseCondition cond in conditions)
				if (!cond.Evaluate (ctx))
					return false;
			return true;
		}
		
		internal override void GetConditionTypes (ArrayList listToFill)
		{
			foreach (BaseCondition cond in conditions)
				cond.GetConditionTypes (listToFill);
		}
	}

	
	internal sealed class Condition: BaseCondition
	{
		ExtensionNodeDescription node;
		string typeId;
		
		internal Condition (ExtensionNodeDescription element, BaseCondition parent): base (parent)
		{
			typeId = element.GetAttribute ("id");
			node = element;
		}
		
		public override bool Evaluate (ExtensionContext ctx)
		{
			if (!base.Evaluate (ctx))
				return false;
			
			ConditionType type = ctx.GetCondition (typeId);
			if (type == null) {
				AddinManager.ReportError ("Condition '" + typeId + "' not found in current extension context.", null, null, false);
				return false;
			}
			
			try {
				return type.Evaluate (node);
			}
			catch (Exception ex) {
				AddinManager.ReportError ("Error while evaluating condition '" + typeId + "'", null, ex, false);
				return false;
			}
		}
		
		internal override void GetConditionTypes (ArrayList listToFill)
		{
			listToFill.Add (typeId);
		}
	}
}
