//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Linq;
using Microsoft.WindowsAPICodePack.Shell;
using System.Collections.Generic;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Diagnostics;

namespace Microsoft.WindowsAPICodePack.Shell.Samples
{
    public class PropertyEdit
    {
        static void Main(string[] args)
        {
            PropertyEdit app = new PropertyEdit();
            try
            {
                app.DoAction(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: " + e.InnerException.Message);
                }
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.Write("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        void DoAction(string [] args)
        {
            if (args.Length == 0 || args[0].Contains("?"))
            {
                Usage();
                return;
            }

         
            if (args[0].Equals("-get", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length != 3)
                {
                    Usage();
                    return;
                }
                string propertyName = args[1];
                string fileName = Path.GetFullPath(args[2]);

                IShellProperty prop = ShellObject.FromParsingName(fileName).Properties.GetProperty(propertyName);

                DisplayPropertyValue(prop);

            } 
            else if (args[0].Equals("-set", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length != 4)
                {
                    Usage();
                    return;
                }
                string propertyName = args[1];
                string value = args[2];
                string fileName = Path.GetFullPath(args[3]);

                IShellProperty prop = ShellObject.FromParsingName(fileName).Properties.GetProperty(propertyName);
                SetPropertyValue(value, prop);

            } 
            else if (args[0].Equals("-info", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length != 2)
                {
                    Usage();
                    return;
                }
                string propertyName = args[1];
                ShellPropertyDescription propDesc = SystemProperties.GetPropertyDescription(propertyName);
                ShowPropertyInfo(propertyName, propDesc);

            }
            else if (args[0].Equals("-enum", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length < 2)
                {
                    Usage();
                    return;
                }
                string fileName = null;
                string filter = null;
                if (args.Length > 2)
                {
                    filter = args[1];
                    fileName = Path.GetFullPath(args[2]);
                }
                else
                {
                    fileName = Path.GetFullPath(args[1]);
                }

                EnumProperties(fileName, filter);

            }
            else
            {
                Usage();
                return;
            }
        }

        private static void DisplayPropertyValue(IShellProperty prop)
        {
            string value = string.Empty;
            value = prop.ValueAsObject == null ? "" : prop.FormatForDisplay(
                    PropertyDescriptionFormatOptions.None);

            Console.WriteLine("{0} = {1}", prop.CanonicalName, value);
        }

        private static void EnumProperties(string fileName, string filter)
        {
            ShellPropertyCollection collection = new ShellPropertyCollection(fileName);


            var properties = collection
                 .Where(
                     prop => prop.CanonicalName != null &&
                     (filter == null ? true : prop.CanonicalName.StartsWith(filter, StringComparison.CurrentCultureIgnoreCase)))
                 .ToArray();

            
            Array.ForEach(
                properties, 
                p=> 
                {
                    DisplayPropertyValue(p);
                });
        }

        private static void ShowPropertyInfo(string propertyName, ShellPropertyDescription propDesc)
        {
            Console.WriteLine("\nProperty {0}", propDesc.CanonicalName);
            Console.WriteLine("\tPropertyKey: {0}, {1}", propDesc.PropertyKey.FormatId.ToString("B"), propDesc.PropertyKey.PropertyId);
            Console.WriteLine("\tLabel:  {0}", propDesc.DisplayName);
            Console.WriteLine("\tEdit Invitation:  {0}", propDesc.EditInvitation);
            Console.WriteLine("\tDisplay Type:  {0}", propDesc.DisplayType);
            Console.WriteLine("\tVar Enum Type:  {0}", propDesc.VarEnumType);
            Console.WriteLine("\tValue Type:  {0}", propDesc.ValueType);
            Console.WriteLine("\tDefault Column Width:  {0}", propDesc.DefaultColumWidth);
            Console.WriteLine("\tAggregation Type:  {0}", propDesc.AggregationTypes);
            Console.WriteLine("\tHas Multiple Values:  {0}", (propDesc.TypeFlags & PropertyTypeOptions.MultipleValues) == PropertyTypeOptions.MultipleValues);
            Console.WriteLine("\tIs Group:  {0}", (propDesc.TypeFlags & PropertyTypeOptions.IsGroup) == PropertyTypeOptions.IsGroup);
            Console.WriteLine("\tIs Innate:  {0}", (propDesc.TypeFlags & PropertyTypeOptions.IsInnate) == PropertyTypeOptions.IsInnate);
            Console.WriteLine("\tIs Queryable:  {0}", (propDesc.TypeFlags & PropertyTypeOptions.IsQueryable) == PropertyTypeOptions.IsQueryable);
            Console.WriteLine("\tIs Viewable:  {0}", (propDesc.TypeFlags & PropertyTypeOptions.IsViewable) == PropertyTypeOptions.IsViewable);
            Console.WriteLine("\tIs SystemProperty:  {0}", (propDesc.TypeFlags & PropertyTypeOptions.IsSystemProperty) == PropertyTypeOptions.IsSystemProperty);
        }

        private static void SetPropertyValue(string value, IShellProperty prop)
        {
            if (prop.ValueType == typeof(string[]))
            {
                string [] values = value.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                (prop as ShellProperty<string[]>).Value = values;
            }
            if (prop.ValueType == typeof(string))
            {
                (prop as ShellProperty<string>).Value = value;
            }
            else if (prop.ValueType == typeof(ushort?))
            {
                (prop as ShellProperty<ushort?>).Value = ushort.Parse(value);
            }
            else if (prop.ValueType == typeof(short?))
            {
                (prop as ShellProperty<short?>).Value = short.Parse(value);
            }
            else if (prop.ValueType == typeof(uint?))
            {
                (prop as ShellProperty<uint?>).Value = uint.Parse(value);
            }
            else if (prop.ValueType == typeof(int?))
            {
                (prop as ShellProperty<int?>).Value = int.Parse(value);
            }
            else if (prop.ValueType == typeof(ulong?))
            {
                (prop as ShellProperty<ulong?>).Value = ulong.Parse(value);
            }
            else if (prop.ValueType == typeof(long?))
            {
                (prop as ShellProperty<long?>).Value = long.Parse(value);
            }
            else if (prop.ValueType == typeof(DateTime?))
            {
                (prop as ShellProperty<DateTime?>).Value = DateTime.Parse(value);
            }
            else if (prop.ValueType == typeof(double?))
            {
                (prop as ShellProperty<double?>).Value = double.Parse(value);
            }
        }

        private void Usage()
        {
            Console.WriteLine("Usage: PropertyEdit.exe <OPTIONS> Filename ");
            Console.WriteLine("");
            Console.WriteLine("OPTIONS:");
            Console.WriteLine(" -get <PropertyName>   Get the value for the property defined");
            Console.WriteLine("                       by its Canonical Name in <propertyName>");
            Console.WriteLine(" -set <PropertyName>   Set the value for the property defined");
            Console.WriteLine("      <PropertyValue>	 by <PropertyName> with value <PropertyValue>");
            Console.WriteLine(" -enum  [Filter]  Enumerate all the properties for this file.");
            Console.WriteLine("                  filtering (starting with) Filter value.");
            Console.WriteLine(" -info <PropertyName>  Get schema information on property for this file.");
            Console.WriteLine("");
            Console.WriteLine("Examples:");
            Console.WriteLine("PropertyEdit -get System.Author foo.jpg");
            Console.WriteLine("PropertyEdit -set System.Author \"Jane Smith;John Smith\" foo.docx");
            Console.WriteLine("PropertyEdit -set System.Photo.MeteringMode 2 foo.jpg");
            Console.WriteLine("PropertyEdit -set System.Photo.DateTaken \"3/11/2009 12:03:02\" foo.jpg");
            Console.WriteLine("PropertyEdit -enum foo.jpg");
            Console.WriteLine("PropertyEdit -enum System.Photo foo.jpg");
            Console.WriteLine("PropertyEdit -info System.Author foo.docx");
        }

        
    }
}
