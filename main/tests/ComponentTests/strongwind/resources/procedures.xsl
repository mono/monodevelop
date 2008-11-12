<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:template match="/">
  <html lang="en">
    <head>
      <title><xsl:value-of select="test/name"/></title>
      <link rel="stylesheet" type="text/css" href="procedures.css" media="all"/>
    </head>
    <body>
      <p class="header">
        <a href="http://www.medsphere.org/projects/strongwind"><img src="strongwind.png" width="182" height="172" alt="Strongwind"/></a><br/><br/>
        <span id="testName"><xsl:value-of select="test/name"/></span><br/>
        <span id="documentName">Strongwind Test Script</span><br/>
        <span id="testDescription"><xsl:value-of select="test/description"/></span><br/>
      </p>

      <hr/>

      <h3>Test Procedures</h3>
      <table>
        <tr>
          <th id="stepNumber">Step</th>
          <th id="action">Action</th>
          <th id="expectedResult">Expected Results</th>
          <th id="screenshot">Screenshot</th>
          <th id="actualResult">Actual Results</th>
        </tr>
        <xsl:for-each select="test/procedures/step">
          <tr>
            <td><xsl:number/></td>
            <td><xsl:value-of select="action"/></td>
            <td><xsl:value-of select="expectedResult"/></td>
            <td>
              <a>
                <xsl:attribute name="href">
                  <xsl:value-of select="screenshot"/>
                </xsl:attribute>
                <img width="80" height="60">
                  <xsl:attribute name="src">
                    <xsl:value-of select="screenshot"/>
                  </xsl:attribute>
                </img>
              </a>
            </td>
            <td></td>
          </tr>
        </xsl:for-each>
      </table>

      <p id="signature">
        Tested By: ________________________________<br/>
        Date of Execution: ________________________________
      </p>
    </body>
  </html>
</xsl:template>
</xsl:stylesheet>
