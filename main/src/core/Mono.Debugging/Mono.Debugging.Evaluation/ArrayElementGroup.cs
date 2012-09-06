// ArrayElementGroup.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Text;
using Mono.Debugging.Client;
using Mono.Debugging.Backend;

namespace Mono.Debugging.Evaluation
{
	public class ArrayElementGroup: RemoteFrameObject, IObjectValueSource
	{
		EvaluationContext ctx;
		int[] baseIndices;
		int firstIndex;
		int lastIndex;
		int[] bounds;
		ICollectionAdaptor array;
		
		const int MaxChildCount = 150;

		public ArrayElementGroup (EvaluationContext ctx, ICollectionAdaptor array)
			: this (ctx, array, new int [0])
		{
		}

		public ArrayElementGroup (EvaluationContext ctx, ICollectionAdaptor array, int[] baseIndices)
			: this (ctx, array, baseIndices, 0, -1)
		{
		}

		public ArrayElementGroup (EvaluationContext ctx, ICollectionAdaptor array, int[] baseIndices, int firstIndex, int lastIndex)
		{
			this.array = array;
			this.ctx = ctx;
			this.bounds = array.GetDimensions ();
			this.baseIndices = baseIndices;
			this.firstIndex = firstIndex;
			this.lastIndex = lastIndex;
		}
		
		public bool IsRange {
			get { return lastIndex != -1; }
		}
		
		public ObjectValue CreateObjectValue ()
		{
			Connect ();
			StringBuilder sb = new StringBuilder ("[");
			for (int n=0; n<baseIndices.Length; n++) {
				if (n > 0)
					sb.Append (", ");
				sb.Append (baseIndices [n].ToString ());
			}
			if (IsRange) {
				if (baseIndices.Length > 0)
					sb.Append (", ");
				sb.Append (firstIndex.ToString ()).Append ("..").Append (lastIndex.ToString ());
			}
			if (bounds.Length > 1 && baseIndices.Length < bounds.Length)
				sb.Append (", ...");
			
			sb.Append ("]");
			
			ObjectValue res = ObjectValue.CreateObject (this, new ObjectPath (sb.ToString ()), "", "", ObjectValueFlags.ArrayElement|ObjectValueFlags.ReadOnly|ObjectValueFlags.NoRefresh, null);
			res.ChildSelector = "";
			return res;
		}

		public ObjectValue[] GetChildren (EvaluationOptions options)
		{
			return GetChildren (new ObjectPath ("this"), -1, -1, options);
		}
		
		public ObjectValue[] GetChildren (ObjectPath path, int firstItemIndex, int count, EvaluationOptions options)
		{
			EvaluationContext cctx = ctx.WithOptions (options);
			if (path.Length > 1) {
				// Looking for children of an array element
				int[] idx = StringToIndices (path [1]);
				object obj = array.GetElement (idx);
				return cctx.Adapter.GetObjectValueChildren (cctx, new ArrayObjectSource (array, path[1]), obj, firstItemIndex, count);
			}
			
			int lowerBound;
			int upperBound;
			bool isLastDimension;
			
			if (bounds.Length > 1) {
				int rank = baseIndices.Length;
				lowerBound = 0;
				upperBound = bounds [rank] - 1;
				isLastDimension = rank == bounds.Length - 1;
			} else {
				lowerBound = 0;
				upperBound = bounds [0] - 1;
				isLastDimension = true;
			}
			
			int len;
			int initalIndex;
			
			if (!IsRange) {
				initalIndex = lowerBound;
				len = upperBound + 1;
			}
			else {
				initalIndex = firstIndex;
				len = lastIndex - firstIndex + 1;
			}
			
			if (firstItemIndex == -1) {
				firstItemIndex = 0;
				count = len;
			}
			
			// Make sure the group doesn't have too many elements. If so, divide
			int div = 1;
			while (len / div > MaxChildCount)
				div *= 10;
			
			if (div == 1 && isLastDimension) {
				// Return array elements
				
				ObjectValue[] values = new ObjectValue [count];
				ObjectPath newPath = new ObjectPath ("this");
				
				int[] curIndex = new int [baseIndices.Length + 1];
				Array.Copy (baseIndices, curIndex, baseIndices.Length);
				string curIndexStr = IndicesToString (baseIndices);
				if (baseIndices.Length > 0) curIndexStr += ",";
				
				for (int n=0; n < values.Length; n++) {
					int index = n + initalIndex + firstItemIndex;
					string sidx = curIndexStr + index.ToString ();
					ObjectValue val;
					string ename = "[" + sidx.Replace (",",", ") + "]";
					if (index > upperBound)
						val = ObjectValue.CreateUnknown (sidx);
					else {
						curIndex [curIndex.Length - 1] = index;
						object elem = array.GetElement (curIndex);
						val = cctx.Adapter.CreateObjectValue (cctx, this, newPath.Append (sidx), elem, ObjectValueFlags.ArrayElement);
						if (elem != null && !cctx.Adapter.IsNull (cctx, elem)) {
							TypeDisplayData tdata = cctx.Adapter.GetTypeDisplayData (cctx, cctx.Adapter.GetValueType (cctx, elem));
							if (!string.IsNullOrEmpty (tdata.NameDisplayString))
								ename = cctx.Adapter.EvaluateDisplayString (cctx, elem, tdata.NameDisplayString);
						}
					}
					val.Name = ename;
					values [n] = val;
				}
				return values;
			}
			else if (!isLastDimension && div == 1) {
				// Return an array element group for each index
				
				List<ObjectValue> list = new List<ObjectValue> ();
				for (int i=0; i<count; i++) {
					int index = i + initalIndex + firstItemIndex;
					ObjectValue val;
					
					// This array must be created at every call to avoid sharing
					// changes with all array groups
					int[] curIndex = new int [baseIndices.Length + 1];
					Array.Copy (baseIndices, curIndex, baseIndices.Length);
					curIndex [curIndex.Length - 1] = index;
					
					if (index > upperBound)
						val = ObjectValue.CreateUnknown ("");
					else {
						ArrayElementGroup grp = new ArrayElementGroup (cctx, array, curIndex);
						val = grp.CreateObjectValue ();
					}
					list.Add (val);
				}
				return list.ToArray ();
			}
			else {
				// Too many elements. Split the array.
				
				// Don't make divisions of 10 elements, min is 100
				if (div == 10)
					div = 100;
				
				// Create the child groups
				int i = initalIndex + firstItemIndex;
				len += i;
				List<ObjectValue> list = new List<ObjectValue> ();
				while (i < len) {
					int end = i + div - 1;
					if (end > len)
						end = len - 1;
					ArrayElementGroup grp = new ArrayElementGroup (cctx, array, baseIndices, i, end);
					list.Add (grp.CreateObjectValue ());
					i += div;
				}
				return list.ToArray ();
			}
		}
		
		internal static string IndicesToString (int[] indices)
		{
			StringBuilder sb = new StringBuilder ();
			for (int n=0; n<indices.Length; n++) {
				if (n > 0)
					sb.Append (',');
				sb.Append (indices [n].ToString ());
			}
			return sb.ToString ();
		}
		
		internal static int[] StringToIndices (string str)
		{
			string[] sidx = str.Split (',');
			int[] idx = new int [sidx.Length];
			for (int n=0; n<sidx.Length; n++)
				idx [n] = int.Parse (sidx [n]);
			return idx;
		}
		
		public static string GetArrayDescription (int[] bounds)
		{
			if (bounds.Length == 0)
				return "[...]";

			StringBuilder sb = new StringBuilder ("[");
			for (int n=0; n<bounds.Length; n++) {
				if (n > 0)
					sb.Append (", ");
				sb.Append (bounds [n].ToString ());
			}
			sb.Append ("]");
			return sb.ToString ();
		}
		
		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			if (path.Length != 2)
				throw new NotSupportedException ();
			
			int[] idx = StringToIndices (path [1]);
			
			object val;
			try {
				EvaluationContext cctx = ctx.Clone ();
				EvaluationOptions ops = options ?? cctx.Options;
				ops.AllowMethodEvaluation = true;
				ops.AllowTargetInvoke = true;
				cctx.Options = ops;
				ValueReference var = ctx.Evaluator.Evaluate (ctx, value, array.ElementType);
				val = var.Value;
				val = ctx.Adapter.Convert (ctx, val, array.ElementType);
				array.SetElement (idx, val);
			} catch {
				val = array.GetElement (idx);
			}
			try {
				return ctx.Evaluator.TargetObjectToExpression (ctx, val);
			} catch (Exception ex) {
				ctx.WriteDebuggerError (ex);
				return new EvaluationResult ("? (" + ex.Message + ")");
			}
		}
		
		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			if (path.Length != 2)
				throw new NotSupportedException ();
			
			int[] idx = StringToIndices (path [1]);
			object elem = array.GetElement (idx);
			EvaluationContext cctx = ctx.WithOptions (options);
			ObjectValue val = cctx.Adapter.CreateObjectValue (cctx, this, path, elem, ObjectValueFlags.ArrayElement);
			if (elem != null && !cctx.Adapter.IsNull (cctx, elem)) {
				TypeDisplayData tdata = cctx.Adapter.GetTypeDisplayData (cctx, cctx.Adapter.GetValueType (cctx, elem));
				if (!string.IsNullOrEmpty (tdata.NameDisplayString))
					val.Name = cctx.Adapter.EvaluateDisplayString (cctx, elem, tdata.NameDisplayString);
			}
			return val;
		}
		
		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			if (path.Length != 2)
				throw new NotSupportedException ();
			
			int[] idx = StringToIndices (path [1]);
			object elem = array.GetElement (idx);
			EvaluationContext cctx = ctx.WithOptions (options);
			return cctx.Adapter.ToRawValue (cctx, new ArrayObjectSource (array, idx), elem);
		}
		
		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			if (path.Length != 2)
				throw new NotSupportedException ();
			
			int[] idx = StringToIndices (path [1]);
			
			EvaluationContext cctx = ctx.WithOptions (options);
			object val = cctx.Adapter.FromRawValue (cctx, value);
			array.SetElement (idx, val);
		}
	}
	
	class ArrayObjectSource: IObjectSource
	{
		ICollectionAdaptor source;
		string path;
		
		public ArrayObjectSource (ICollectionAdaptor source, string path)
		{
			this.source = source;
			this.path = path;
		}
		
		public ArrayObjectSource (ICollectionAdaptor source, int[] index)
		{
			this.source = source;
			this.path = ArrayElementGroup.IndicesToString (index);
		}
		
		public object Value {
			get {
				return source.GetElement (ArrayElementGroup.StringToIndices (path));
			}
			set {
				source.SetElement (ArrayElementGroup.StringToIndices (path), value);
			}
		}
	}
}
