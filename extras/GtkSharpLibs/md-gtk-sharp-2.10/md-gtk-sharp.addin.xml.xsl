<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" indent="yes" />
<xsl:template match="/">
<Addin
       namespace   = "MonoDevelop"
       author      = "Lluis Sanchez"
       copyright   = "GPL"
       url         = "http://www.monodevelop.com"
       category    = "GTK">
       
    <xsl:attribute name="id">GtkCore.GtkSharp.<xsl:value-of select="/files/targetversion"/></xsl:attribute>
    <xsl:attribute name="name">GTK# <xsl:value-of select="/files/targetversion"/> Compilation Support</xsl:attribute>
    <xsl:attribute name="version"><xsl:value-of select="/files/addinversion"/></xsl:attribute>
    <xsl:attribute name="description">Allows building applications which target GTK# <xsl:value-of select="/files/targetversion"/></xsl:attribute>

	<Runtime>
		<xsl:for-each select="/files/config|/files/dll">
			<xsl:element name="Import"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
		</xsl:for-each>
	</Runtime>
	
	<Dependencies>
		<Addin id="Core">
		    <xsl:attribute name="version"><xsl:value-of select="/files/coreaddinversion"/></xsl:attribute>
		</Addin>
		<Addin id="GtkCore">
		    <xsl:attribute name="version"><xsl:value-of select="/files/coreaddinversion"/></xsl:attribute>
		</Addin>
	</Dependencies>
	
	<Extension path = "/MonoDevelop/Core/SupportPackages">
		<Package name="art-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/art-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="gtk-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/atk-sharp') or contains(.,'/pango-sharp') or contains(.,'/gdk-sharp') or contains(.,'/gtk-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="vte-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/vte-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="glib-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/glib-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="rsvg-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/rsvg-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="gtk-dotnet-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/gtk-dotnet')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="gconf-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/gconf-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="glade-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/glade-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="gnome-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/gnome-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="gnome-vfs-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/gnome-vfs-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
		<Package name="gtkhtml-sharp-2.0" gacRoot="true">
		    <xsl:attribute name="version"><xsl:value-of select="/files/gtkversion"/></xsl:attribute>
			<xsl:for-each select="/files/dll[contains(.,'/gtkhtml-sharp')]">
				<xsl:element name="Assembly"><xsl:attribute name="file"><xsl:value-of select="." /></xsl:attribute></xsl:element>
			</xsl:for-each>
		</Package>
	</Extension>
</Addin>
</xsl:template>
</xsl:stylesheet>
