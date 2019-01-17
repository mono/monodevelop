// Gio.cs
//
// Author:  Mike Kestner  <mkestner@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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


using System;
using System.Collections;
using System.Runtime.InteropServices;
using MonoDevelop.Ide.Desktop;

namespace MonoDevelop.Platform {

	internal static class Gio {

		const string gio = "libgio-2.0-0.dll";
		const string glib = "libglib-2.0-0.dll";
		const string gobject = "libgobject-2.0-0.dll";

		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_app_info_get_executable (IntPtr raw);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_app_info_get_id (IntPtr raw);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_app_info_get_name (IntPtr raw);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_app_info_get_default_for_type (IntPtr content_type, bool must_support_uris);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_app_info_get_all_for_type (IntPtr content_type);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_content_type_from_mime_type (IntPtr mime_type);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_content_type_get_description (IntPtr mime_type);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_content_type_get_mime_type (IntPtr content_type);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_file_info_get_content_type (IntPtr handle);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_file_info_get_icon (IntPtr fileinfo);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_file_new_for_path (IntPtr path);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_file_new_for_uri (IntPtr uri);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_file_query_info (IntPtr handle, IntPtr attrs, int flags, IntPtr cancellable, out IntPtr error);
		[DllImport (gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_icon_to_string (IntPtr icon);
		[DllImport(gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_settings_get_string(IntPtr gsettings, IntPtr key);
		[DllImport(gio, CallingConvention = CallingConvention.Cdecl)]
		static extern IntPtr g_settings_new(IntPtr schema);
		[DllImport (glib, CallingConvention = CallingConvention.Cdecl)]
		static extern void g_error_free (IntPtr raw);
		[DllImport (glib, CallingConvention = CallingConvention.Cdecl)]
		static extern void g_list_free (IntPtr raw);
		[DllImport (gobject, CallingConvention = CallingConvention.Cdecl)]
		static extern void g_object_unref (IntPtr handle);

		struct GList {
			#pragma warning disable 649 // never assigned
			public IntPtr data;
			public IntPtr next;
			public IntPtr prev;
			#pragma warning restore 649
		}

		static GnomeDesktopApplication AppFromAppInfoPtr (IntPtr handle, DesktopApplication defaultApp)
		{
			string id = GLib.Marshaller.Utf8PtrToString (g_app_info_get_id (handle));
			string name = GLib.Marshaller.Utf8PtrToString (g_app_info_get_name (handle));
			string executable = GLib.Marshaller.Utf8PtrToString (g_app_info_get_executable (handle));
			
			if (!string.IsNullOrEmpty (name) && !string.IsNullOrEmpty (executable) && !executable.Contains ("monodevelop "))
				return new GnomeDesktopApplication (executable, name, defaultApp != null && defaultApp.Id == id);
			return null;
		}

		static IntPtr ContentTypeFromMimeType (string mime_type)
		{
			IntPtr native = GLib.Marshaller.StringToPtrGStrdup (mime_type);
			IntPtr content_type;
			try {
				content_type = g_content_type_from_mime_type (native);
			} catch (EntryPointNotFoundException) {
				return native;
			}
			GLib.Marshaller.Free (native);
			return content_type;
		}

		public static DesktopApplication GetDefaultForType (string mime_type) 
		{
			IntPtr content_type = ContentTypeFromMimeType (mime_type);
			IntPtr ret = g_app_info_get_default_for_type (content_type, false);
			GLib.Marshaller.Free (content_type);
			return ret == IntPtr.Zero ? null : AppFromAppInfoPtr (ret, null);
		}

		public static System.Collections.Generic.IList<DesktopApplication> GetAllForType (string mime_type)
		{
			var def = GetDefaultForType (mime_type);
			
			IntPtr content_type = ContentTypeFromMimeType (mime_type);
			IntPtr ret = g_app_info_get_all_for_type (content_type);
			GLib.Marshaller.Free (content_type);
			IntPtr l = ret;
			var apps = new System.Collections.Generic.List<DesktopApplication> ();
			while (l != IntPtr.Zero) {
				GList node = (GList) Marshal.PtrToStructure (l, typeof (GList));
				if (node.data != IntPtr.Zero) {
					var app = AppFromAppInfoPtr (node.data, def);
					if (app != null)
						apps.Add (app);
				}
				l = node.next;
			}
			g_list_free (ret);
			return apps;
		}

		public static string GetGSettingsString(string schema, string key)
		{
			IntPtr schema_native = GLib.Marshaller.StringToPtrGStrdup (schema);
			IntPtr gsettings = g_settings_new (schema_native);
			GLib.Marshaller.Free (schema_native);

			IntPtr key_native = GLib.Marshaller.StringToPtrGStrdup (key);
			IntPtr ret = g_settings_get_string (gsettings, key_native);
			GLib.Marshaller.Free (key_native);

			g_object_unref (gsettings);
			return GLib.Marshaller.PtrToStringGFree (ret);
		}

		public static string GetIconIdForFile (string filename)
		{
			if (String.IsNullOrEmpty (filename))
				return null;
			IntPtr native = GLib.Marshaller.StringToPtrGStrdup (filename);
			IntPtr gfile = g_file_new_for_path (native);
			GLib.Marshaller.Free (native);
			IntPtr native_attrs = GLib.Marshaller.StringToPtrGStrdup ("standard::icon");
			IntPtr error;
			IntPtr info = g_file_query_info (gfile, native_attrs, 0, IntPtr.Zero, out error);
			GLib.Marshaller.Free (native_attrs);
			if (error != IntPtr.Zero) {
				g_error_free (error);
				g_object_unref (gfile);
				return null;
			}
			IntPtr iconnative = g_icon_to_string (g_file_info_get_icon (info));
			string[] iconid = GLib.Marshaller.Utf8PtrToString (iconnative).Split (' ');
			GLib.Marshaller.Free (iconnative);
			g_object_unref (info);
			g_object_unref (gfile);
			// g_icon_to_string should give us 4 fields, 2nd is GThemedIcon
			// if this isn't the case, we're into crazyland, and fall back
			if (iconid.Length == 4 && iconid [1].Trim () == "GThemedIcon")
				return iconid [2].Trim ();
			return null;
		}

		public static string GetMimeTypeDescription (string mime_type)
		{
			IntPtr content_type = ContentTypeFromMimeType (mime_type);
			IntPtr desc = g_content_type_get_description (content_type);
			GLib.Marshaller.Free (content_type);
			return GLib.Marshaller.PtrToStringGFree (desc);
		}

		public static string GetMimeTypeForUri (string uri)
		{
			if (String.IsNullOrEmpty (uri))
				return null;
			IntPtr native = GLib.Marshaller.StringToPtrGStrdup (uri);
			IntPtr gfile = g_file_new_for_uri (native);
			GLib.Marshaller.Free (native);
			IntPtr native_attrs = GLib.Marshaller.StringToPtrGStrdup ("standard::content-type");
			IntPtr error;
			IntPtr info = g_file_query_info (gfile, native_attrs, 0, IntPtr.Zero, out error);
			GLib.Marshaller.Free (native_attrs);
			if (error != IntPtr.Zero) {
				g_error_free (error);
				g_object_unref (gfile);
				return null;
			}
			IntPtr content_type = g_file_info_get_content_type (info);
			string mime_type = GLib.Marshaller.Utf8PtrToString (g_content_type_get_mime_type (content_type));
			GLib.Marshaller.Free (content_type);
			g_object_unref (info);
			g_object_unref (gfile);
			return mime_type;
		}
	}
}
