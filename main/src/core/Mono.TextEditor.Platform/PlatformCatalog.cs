//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Ide.Editor.Highlighting;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Platform
{
    public class PlatformCatalog
    {
        public static PlatformCatalog Instance = new PlatformCatalog();

        public CompositionContainer CompositionContainer { get; }

        public ITextBufferFactoryService2 TextBufferFactoryService { get; }

        private PlatformCatalog()
        {
            var container = PlatformCatalog.CreateContainer();
            container.SatisfyImportsOnce(this);

            this.CompositionContainer = container;
            this.TextBufferFactoryService = (ITextBufferFactoryService2)_textBufferFactoryService;

            this.MimeToContentTypeRegistryService.LinkTypes("text/plain", this.ContentTypeRegistryService.GetContentType("text"));		  //HACK
            this.MimeToContentTypeRegistryService.LinkTypes("text/x-csharp", this.ContentTypeRegistryService.GetContentType("csharp"));   //HACK

            if (null != this.ContentTypeRegistryService.GetContentType("css"))
            {
                this.MimeToContentTypeRegistryService.LinkTypes("text/x-css", this.ContentTypeRegistryService.GetContentType("css"));   //HACK
                this.MimeToContentTypeRegistryService.LinkTypes("text/x-html", this.ContentTypeRegistryService.GetContentType("htmlx"));   //HACK
                this.MimeToContentTypeRegistryService.LinkTypes("text/x-json", this.ContentTypeRegistryService.GetContentType("JSON"));   //HACK
            }
        }

        private static CompositionContainer CreateContainer()
        {
            // TODO: Read these from manifest.addin.xml?
            AggregateCatalog catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(PlatformCatalog).Assembly));

            // Add other assemblies from which we expect to get MEF objects
            // TODO: add some mechanism to allow these to be updated at runtime.
            string[] assemblyNames =
                {
                "Microsoft.VisualStudio.Text.Logic"
                };

            foreach (string assemblyName in assemblyNames)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    catalog.Catalogs.Add(new AssemblyCatalog(assembly));
                }
                catch (Exception e)
                {
                    LoggingService.LogError("Workspace can't load assembly " + assemblyName + " to host mef services.", e);
                }
            }

            foreach (var node in AddinManager.GetExtensionNodes("/MonoDevelop/Ide/TypeService/PlatformMefHostServices"))
            {
                var assemblyNode = node as AssemblyExtensionNode;
                if (assemblyNode != null)
                {
                    try
                    {
                        var assemblyFilePath = assemblyNode.Addin.GetFilePath(assemblyNode.FileName);
                        var assembly = Assembly.LoadFrom(assemblyFilePath);
                        catalog.Catalogs.Add(new AssemblyCatalog(assembly));
                    }
                    catch (Exception e)
                    {
                        LoggingService.LogError("Workspace can't load assembly " + assemblyNode.FileName + " to host mef services.", e);
                    }
                }
            }

            //Create the CompositionContainer with the parts in the catalog
            CompositionContainer container = new CompositionContainer(catalog);

            return container;
        }

        [Export]                                        //HACK
        [Name("csharp")]                                //HACK
        [BaseDefinition("code")]                        //HACK
        public ContentTypeDefinition codeContentType;   //HACK

        [Import]
        internal ITextBufferFactoryService _textBufferFactoryService { get; private set; }

        [Import]
        internal ITextEditorFactoryService TextEditorFactoryService { get; private set; }

        [Import]
        internal IMimeToContentTypeRegistryService MimeToContentTypeRegistryService { get; private set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; private set; }

        [Import]
        internal IBufferTagAggregatorFactoryService BufferTagAggregatorFactoryService { get; private set; }

        [Import]
        internal IClassifierAggregatorService ClassifierAggregatorService { get; private set; }
    }

    [Export(typeof(IThreadHelper))]
    public class PlatformThreadHelper : IThreadHelper
    {
        public Task RunOnUIThread(Action action)
        {
            return MonoDevelop.Core.Runtime.RunInMainThread(action);
        }

        public Task RunOnUIThread(UIThreadPriority priority, Action action)
        {
            return MonoDevelop.Core.Runtime.RunInMainThread(action);
        }

        public Task<T> RunOnUIThread<T>(Func<T> function)
        {
            return MonoDevelop.Core.Runtime.RunInMainThread(function);
        }

        public Task<T> RunOnUIThread<T>(UIThreadPriority priority, Func<T> function)
        {
            return MonoDevelop.Core.Runtime.RunInMainThread(function);
        }
    }

    public interface IMimeToContentTypeRegistryService
    {
        string GetMimeType(IContentType type);
        IContentType GetContentType(string type);

        void LinkTypes(string mimeType, IContentType contentType);
    }

    [Export(typeof(IMimeToContentTypeRegistryService))]
    public class MimeToContentTypeRegistryService : IMimeToContentTypeRegistryService
    {
        public string GetMimeType(IContentType type)
        {
            string mimeType;
            if (this.maps.Item2.TryGetValue(type, out mimeType))
            {
                return mimeType;
            }

            return null;
        }

        public IContentType GetContentType(string type)
        {
            IContentType contentType;
            if (this.maps.Item1.TryGetValue(type, out contentType))
            {
                return contentType;
            }

            return null;
        }

        public void LinkTypes(string mimeType, IContentType contentType)
        {
            var oldMap = Volatile.Read(ref this.maps);
            while (true)
            {
                if (oldMap.Item1.ContainsKey(mimeType) || oldMap.Item2.ContainsKey(contentType))
                    break;

                var newMap = Tuple.Create(oldMap.Item1.Add(mimeType, contentType), oldMap.Item2.Add(contentType, mimeType));
                var result = Interlocked.CompareExchange(ref this.maps, newMap, oldMap);
                if (result == oldMap)
                {
                    break;
                }

                oldMap = result;
            }

        }

        private Tuple<ImmutableDictionary<string, IContentType>, ImmutableDictionary<IContentType, string>> maps = Tuple.Create(ImmutableDictionary<string, IContentType>.Empty, ImmutableDictionary<IContentType, string>.Empty);
    }

#if false
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(IClassificationTag))]
    public class TestClassifierProvider : ITaggerProvider
    {
        [Import]
        internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; private set; }

        [Export]
        [Name("keyword")]
        public ClassificationTypeDefinition textClassificationType;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(typeof(TestClassifier), () => new TestClassifier(this)) as ITagger<T>;
        }
    }

    public class TestClassifier : ITagger<IClassificationTag>
    {
        private ClassificationTag _keyword { get; }

        public TestClassifier(TestClassifierProvider provider)
        {
            _keyword = new ClassificationTag(provider.ClassificationTypeRegistryService.GetClassificationType("keyword"));
        }

        public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                int start = -1;
                for (int i = span.Start; (i < span.End); ++i)
                {
                    var c = span.Snapshot[i];
                    if ((c == 'a') || (c == 'A'))
                    {
                        if (start == -1)
                        {
                            start = i;
                        }
                    }
                    else if (start != -1)
                    {
                        yield return new TagSpan<ClassificationTag>(
                                new SnapshotSpan(span.Snapshot, start, i - start),
                                _keyword);
                        start = -1;
                    }
                }

                if (start != -1)
                {
                    yield return new TagSpan<ClassificationTag>(
                            new SnapshotSpan(span.Snapshot, start, span.End - start),
                            _keyword);
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
#endif
}