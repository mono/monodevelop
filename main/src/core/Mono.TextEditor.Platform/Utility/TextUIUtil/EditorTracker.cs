// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Editor;

    internal sealed class EditorTracker : IDisposable
    {
        ITextEditorFactoryService factory;

        static List<WeakReference> allocatedEditors = new List<WeakReference>();

        public EditorTracker(ITextEditorFactoryService factory)
        {
            this.factory = factory;
            factory.TextViewCreated += OnTextViewCreated;
        }

        public void Dispose()
        {
            this.factory.TextViewCreated -= OnTextViewCreated;
            allocatedEditors.Clear();
        }

        public static void TagEditor(IWpfTextView editor, string tag)
        {
            if (editor == null)
            {
                throw new ArgumentNullException("editor");
            }
            if (tag == null)
            {
                throw new ArgumentNullException("tag");
            }
            editor.Properties.AddProperty("tag", tag);
        }

        public static int ReportLiveEditors(System.IO.TextWriter writer)
        {
            int totalEditors = 0;
            for (int e = 0; e < allocatedEditors.Count; ++e)
                if (allocatedEditors[e].IsAlive)
                    ++totalEditors;

            if (writer != null)
            {
                writer.Write
                    (String.Format
                        (System.Globalization.CultureInfo.CurrentCulture,
                         "{0}\r\n", totalEditors));

                int liveEditors = 0;
                for (int e = 0; e < allocatedEditors.Count; ++e)
                {
                    IWpfTextView editor = allocatedEditors[e].Target as IWpfTextView;
                    if (editor != null)
                    {
                        ++liveEditors;

                        string tag = "";
                        bool found = editor.Properties.TryGetProperty<string>("tag", out tag);
                        writer.Write
                            (String.Format
                                (System.Globalization.CultureInfo.CurrentCulture,
                                 "{0,5} {1}\r\n", e, found ? tag : "Untagged"));

                        foreach (KeyValuePair<Object, Object> pair in editor.Properties.PropertyList)
                        {
                            if (pair.Key.ToString() != "tag")
                            {
                                string rhsType;
                                if (pair.Value == null)
                                {
                                    rhsType = "?null";
                                }
                                else
                                {
                                    rhsType = pair.Value.GetType().Name;
                                    if (pair.Value is WeakReference)
                                    {
                                        Object target = (pair.Value as WeakReference).Target;
                                        if (target != null)
                                        {
                                            rhsType = rhsType + "(" + target.GetType().Name + ")";
                                        }
                                    }
                                }
                                writer.Write(String.Format(System.Globalization.CultureInfo.CurrentCulture, "      {0}\r\n", rhsType));
                            }
                        }
                    }
                }
            }

            return totalEditors;
        }

        private void OnTextViewCreated(object sender, TextViewCreatedEventArgs e)
        {
            // look for a hole in allocatedEditors
            int ed = -1;
            while (true)
            {
                if (++ed >= allocatedEditors.Count)
                {
                    allocatedEditors.Add(new WeakReference(e.TextView));
                    break;
                }
                else if (!allocatedEditors[ed].IsAlive)
                {
                    allocatedEditors[ed] = new WeakReference(e.TextView);
                    break;
                }
            }
        }
    }
}
