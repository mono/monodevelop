/* Copyright (c) 2005 Novell, Inc. */

#include <gdk/gdk.h>
#include <gdk/gdkx.h>

gboolean stetic_keycode_is_modifier (guint keycode);

gboolean 
stetic_keycode_is_modifier (guint keycode)
{
	static XModifierKeymap *mod_keymap;
	static int map_size;
	int i;

	if (!mod_keymap) {
		mod_keymap = XGetModifierMapping (gdk_display);
		map_size = 8 * mod_keymap->max_keypermod;
	}

	for (i = 0; i < map_size; i++) {
		if (keycode == mod_keymap->modifiermap[i])
			return TRUE;
	}

	return FALSE;
}
