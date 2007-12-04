/* Copyright (c) 2004-2005 Novell, Inc. */

#include <glib-object.h>

GType stetic_param_spec_get_value_type (GParamSpec *pspec);
gboolean stetic_param_spec_is_unichar (GParamSpec *pspec);
gboolean stetic_param_spec_get_minimum (GParamSpec *pspec, GValue *value);
gboolean stetic_param_spec_get_maximum (GParamSpec *pspec, GValue *value);
gboolean stetic_param_spec_get_default (GParamSpec *pspec, GValue *value);

GType
stetic_param_spec_get_value_type (GParamSpec *pspec)
{
	return pspec->value_type;
}

gboolean
stetic_param_spec_is_unichar (GParamSpec *pspec)
{
	return G_IS_PARAM_SPEC_UNICHAR (pspec);
}

gboolean
stetic_param_spec_get_minimum (GParamSpec *pspec, GValue *value)
{
	g_value_init (value, pspec->value_type);

	if (G_IS_PARAM_SPEC_CHAR (pspec))
		g_value_set_char (value, G_PARAM_SPEC_CHAR (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_UCHAR (pspec))
		g_value_set_uchar (value, G_PARAM_SPEC_UCHAR (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_INT (pspec))
		g_value_set_int (value, G_PARAM_SPEC_INT (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_UINT (pspec))
		g_value_set_uint (value, G_PARAM_SPEC_UINT (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_LONG (pspec))
		g_value_set_long (value, G_PARAM_SPEC_LONG (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_ULONG (pspec))
		g_value_set_ulong (value, G_PARAM_SPEC_ULONG (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_INT64 (pspec))
		g_value_set_int64 (value, G_PARAM_SPEC_INT64 (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_UINT64 (pspec))
		g_value_set_uint64 (value, G_PARAM_SPEC_UINT64 (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_FLOAT (pspec))
		g_value_set_float (value, G_PARAM_SPEC_FLOAT (pspec)->minimum);
	else if (G_IS_PARAM_SPEC_DOUBLE (pspec))
		g_value_set_double (value, G_PARAM_SPEC_DOUBLE (pspec)->minimum);
	else
		return FALSE;
	return TRUE;
}

gboolean
stetic_param_spec_get_maximum (GParamSpec *pspec, GValue *value)
{
	g_value_init (value, pspec->value_type);

	if (G_IS_PARAM_SPEC_CHAR (pspec))
		g_value_set_char (value, G_PARAM_SPEC_CHAR (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_UCHAR (pspec))
		g_value_set_uchar (value, G_PARAM_SPEC_UCHAR (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_INT (pspec))
		g_value_set_int (value, G_PARAM_SPEC_INT (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_UINT (pspec))
		g_value_set_uint (value, G_PARAM_SPEC_UINT (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_LONG (pspec))
		g_value_set_long (value, G_PARAM_SPEC_LONG (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_ULONG (pspec))
		g_value_set_ulong (value, G_PARAM_SPEC_ULONG (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_INT64 (pspec))
		g_value_set_int64 (value, G_PARAM_SPEC_INT64 (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_UINT64 (pspec))
		g_value_set_uint64 (value, G_PARAM_SPEC_UINT64 (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_FLOAT (pspec))
		g_value_set_float (value, G_PARAM_SPEC_FLOAT (pspec)->maximum);
	else if (G_IS_PARAM_SPEC_DOUBLE (pspec))
		g_value_set_double (value, G_PARAM_SPEC_DOUBLE (pspec)->maximum);
	else
		return FALSE;
	return TRUE;
}

gboolean
stetic_param_spec_get_default (GParamSpec *pspec, GValue *value)
{
	g_value_init (value, pspec->value_type);

	if (G_IS_PARAM_SPEC_CHAR (pspec))
		g_value_set_char (value, G_PARAM_SPEC_CHAR (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_UCHAR (pspec))
		g_value_set_uchar (value, G_PARAM_SPEC_UCHAR (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_INT (pspec))
		g_value_set_int (value, G_PARAM_SPEC_INT (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_UINT (pspec))
		g_value_set_uint (value, G_PARAM_SPEC_UINT (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_LONG (pspec))
		g_value_set_long (value, G_PARAM_SPEC_LONG (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_ULONG (pspec))
		g_value_set_ulong (value, G_PARAM_SPEC_ULONG (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_INT64 (pspec))
		g_value_set_int64 (value, G_PARAM_SPEC_INT64 (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_UINT64 (pspec))
		g_value_set_uint64 (value, G_PARAM_SPEC_UINT64 (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_FLOAT (pspec))
		g_value_set_float (value, G_PARAM_SPEC_FLOAT (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_DOUBLE (pspec))
		g_value_set_double (value, G_PARAM_SPEC_DOUBLE (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_BOOLEAN (pspec))
		g_value_set_boolean (value, G_PARAM_SPEC_BOOLEAN (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_UNICHAR (pspec))
		g_value_set_uint (value, G_PARAM_SPEC_UNICHAR (pspec)->default_value);
	else if (G_IS_PARAM_SPEC_STRING (pspec))
		g_value_set_static_string (value, G_PARAM_SPEC_STRING (pspec)->default_value);
	else
		return FALSE;
	return TRUE;
}
