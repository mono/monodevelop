//
// MSBuildEngine.cs
//
// Author:
//       lluis <>
//
// Copyright (c) 2015 lluis
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
using MonoDevelop.Core;

namespace MonoDevelop.Projects.MSBuild
{
	abstract class MSBuildEngine: IDisposable
	{
		bool disposed;

		protected MSBuildEngine (MSBuildEngineManager manager)
		{
			EngineManager = manager;
		}

		public MSBuildEngineManager EngineManager { get; private set; }

		public abstract object LoadProject (MSBuildProject project, string xml, FilePath fileName);

		public abstract void UnloadProject (object project);

		public void Dispose ()
		{
			if (!disposed) {
				disposed = true;
				OnDispose ();
			}
		}

		internal bool Disposed {
			get { return disposed; }
		}

		public virtual void OnDispose ()
		{
		}

		public abstract object CreateProjectInstance (object project);

		public abstract void DisposeProjectInstance (object projectInstance);

		public virtual void Evaluate (object projectInstance)
		{
		}

		public abstract bool GetItemHasMetadata (object item, string name);

		public abstract string GetItemMetadata (object item, string name);

		public abstract string GetEvaluatedItemMetadata (object item, string name);

		public abstract IEnumerable<string> GetItemMetadataNames (object item);

		public abstract IEnumerable<object> GetImports (object projectInstance);

		public abstract string GetImportEvaluatedProjectPath (object projectInstance, object import);

		public abstract IEnumerable<object> GetEvaluatedItemsIgnoringCondition (object projectInstance);

		public abstract IEnumerable<object> GetEvaluatedProperties (object projectInstance);

		public abstract IEnumerable<object> GetEvaluatedItems (object projectInstance);

		public abstract void GetItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported);

		public abstract void GetEvaluatedItemInfo (object item, out string name, out string include, out string finalItemSpec, out bool imported);

		public abstract void GetPropertyInfo (object property, out string name, out string value, out string finalValue, out bool definedMultipleTimes);

		public abstract IEnumerable<MSBuildTarget> GetTargets (object projectInstance);

		public abstract IEnumerable<MSBuildTarget> GetTargetsIgnoringCondition (object projectInstance);

		public abstract void SetGlobalProperty (object projectInstance, string property, string value);

		public abstract void RemoveGlobalProperty (object projectInstance, string property);

		public abstract ConditionedPropertyCollection GetConditionedProperties (object projectInstance);

		public abstract IEnumerable<MSBuildItem> FindGlobItemsIncludingFile  (object projectInstance, string include);

		internal abstract IEnumerable<MSBuildItem> FindUpdateGlobItemsIncludingFile (object projectInstance, string include, MSBuildItem globItem);
	}
}

