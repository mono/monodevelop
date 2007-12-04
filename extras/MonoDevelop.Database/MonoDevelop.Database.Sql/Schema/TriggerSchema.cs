//
// Schema/TriggerSchema.cs
//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (c) 2007 Ben Motmans
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

namespace MonoDevelop.Database.Sql
{
	public class TriggerSchema : AbstractSchema
	{
		protected string tableName;
		protected TriggerType triggerType;
		protected TriggerFireType triggerFireType;
		protected TriggerEvent triggerEvent;
		protected int position;
		protected bool isActive;
		protected string source;
		
		public TriggerSchema (ISchemaProvider schemaProvider)
			: base (schemaProvider)
		{
		}
		
		public TriggerSchema (TriggerSchema trigger)
			: base (trigger)
		{
			tableName = trigger.tableName;
			triggerType = trigger.triggerType;
			triggerFireType = trigger.triggerFireType;
			triggerEvent = trigger.triggerEvent;
			position = trigger.position;
			isActive = trigger.isActive;
			source = trigger.source;
		}
		
		public string TableName {
			get { return tableName; }
			set {
				if (tableName != value) {
					tableName = value;
					OnChanged ();
				}
			}
		}
		
		public TriggerType TriggerType {
			get { return triggerType; }
			set {
				if (triggerType != value) {
					triggerType = value;
					OnChanged ();
				}
			}
		}
		
		public TriggerFireType TriggerFireType {
			get { return triggerFireType; }
			set {
				if (triggerFireType != value) {
					triggerFireType = value;
					OnChanged ();
				}
			}
		}
		
		public TriggerEvent TriggerEvent {
			get { return triggerEvent; }
			set {
				if (triggerEvent != value) {
					triggerEvent = value;
					OnChanged ();
				}
			}
		}
		
		public int Position {
			get { return position; }
			set {
				if (position != value) {
					position = value;
					OnChanged ();
				}
			}
		}
		
		public bool IsActive {
			get { return isActive; }
			set {
				if (isActive != value) {
					isActive = value;
					OnChanged ();
				}
			}
		}
		
		public string Source {
			get { return source; }
			set {
				if (source != value) {
					source = value;
					OnChanged ();
				}
			}
		}
		
		public override object Clone ()
		{
			return new TriggerSchema (this);
		}
	}
}
