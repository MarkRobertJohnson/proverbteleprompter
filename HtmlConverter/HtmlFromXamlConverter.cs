//---------------------------------------------------------------------------
// 
// File: HtmlFromXamlConverter.cs
//
// Copyright (C) Microsoft Corporation.  All rights reserved.
//
// Description: Prototype for Xaml - Html conversion 
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Xml;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ProverbTeleprompter.HtmlConverter
{
    /// <summary>
    /// HtmlToXamlConverter is a static class that takes an HTML string
    /// and converts it into XAML
    /// </summary>
    public static class HtmlFromXamlConverter
    {
        // ---------------------------------------------------------------------
        //
        // Internal Methods
        //
        // ---------------------------------------------------------------------

        #region Internal Methods

        public static string GetFlowDocumentText(FlowDocument flowDoc)
        {
            TextPointer contentstart = flowDoc.ContentStart;
            TextPointer contentend = flowDoc.ContentEnd;
            TextRange textRange = new TextRange(contentstart, contentend);
            return textRange.Text;
        }

        public static string SerializeFlowDocument(FlowDocument flowDoc)
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream();

                using (StreamWriter xmlTextWriter = new StreamWriter(memoryStream, Encoding.GetEncoding("iso8859-2")))
                {
                    XamlWriter.Save(flowDoc, xmlTextWriter);
                    StreamReader xmltextreader = new StreamReader(memoryStream, Encoding.GetEncoding("iso8859-2"));
                    xmltextreader.BaseStream.Position = 0;
                    xmltextreader.BaseStream.Position = 0;
                    return xmltextreader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                return String.Empty;
            }
        }

        public static string ConvertXamlToHtml(FlowDocument flowDoc)
        {
            string strXml = SerializeFlowDocument(flowDoc);
            Debug.WriteLine("XML: " + strXml);
            return ConvertXamlToHtml(strXml);
        }

        /// <summary>
        /// Main entry point for Xaml-to-Html converter.
        /// Converts a xaml string into html string.
        /// </summary>
        /// <param name="xamlString">
        /// Xaml strinng to convert.
        /// </param>
        /// <returns>
        /// Html string produced from a source xaml.
        /// </returns>
        public static string ConvertXamlToHtml(string xamlString)
        {
            XmlTextReader xamlReader;
            StringBuilder htmlStringBuilder;
            XmlTextWriter htmlWriter;

            xamlReader = new XmlTextReader(new StringReader(xamlString));

            htmlStringBuilder = new StringBuilder(100);
            htmlWriter = new XmlTextWriter(new StringWriter(htmlStringBuilder));

            if (!WriteFlowDocument(xamlReader, htmlWriter))
            {
                return "";
            }

            string htmlString = htmlStringBuilder.ToString();

            return htmlString;
        }

        #endregion Internal Methods

        // ---------------------------------------------------------------------
        //
        // Private Methods
        //
        // ---------------------------------------------------------------------

        #region Private Methods
        /// <summary>
        /// Processes a root level element of XAML (normally it's FlowDocument element).
        /// </summary>
        /// <param name="xamlReader">
        /// XmlTextReader for a source xaml.
        /// </param>
        /// <param name="htmlWriter">
        /// XmlTextWriter producing resulting html
        /// </param>
        private static bool WriteFlowDocument(XmlTextReader xamlReader, XmlTextWriter htmlWriter)
        {
            if (!ReadNextToken(xamlReader))
            {
                // Xaml content is empty - nothing to convert
                return false;
            }

            if (xamlReader.NodeType != XmlNodeType.Element || xamlReader.Name != "FlowDocument")
            {
                // Root FlowDocument elemet is missing
                return false;
            }

            // Create a buffer StringBuilder for collecting css properties for inline STYLE attributes
            // on every element level (it will be re-initialized on every level).
            StringBuilder inlineStyle = new StringBuilder();

            htmlWriter.WriteStartElement("HTML");
            htmlWriter.WriteRaw(HtmlParser.BaseHtmlInfoString);
            htmlWriter.WriteStartElement("BODY");

            WriteFormattingProperties(xamlReader, htmlWriter, inlineStyle);

            WriteElementContent(xamlReader, htmlWriter, inlineStyle,string.Empty);

            htmlWriter.WriteEndElement();
            htmlWriter.WriteEndElement();

            return true;
        }

        /// <summary>
        /// Reads attributes of the current xaml element and converts
        /// them into appropriate html attributes or css styles.
        /// </summary>
        /// <param name="xamlReader">
        /// XmlTextReader which is expected to be at XmlNodeType.Element
        /// (opening element tag) position.
        /// The reader will remain at the same level after function complete.
        /// </param>
        /// <param name="htmlWriter">
        /// XmlTextWriter for output html, which is expected to be in
        /// after WriteStartElement state.
        /// </param>
        /// <param name="inlineStyle">
        /// String builder for collecting css properties for inline STYLE attribute.
        /// </param>
        private static void WriteFormattingProperties(XmlTextReader xamlReader, XmlTextWriter htmlWriter, StringBuilder inlineStyle)
        {
            Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

            // Clear string builder for the inline style
            inlineStyle.Remove(0, inlineStyle.Length);

            if (!xamlReader.HasAttributes)
            {
                return;
            }

            bool borderSet = false;
            bool fontSet = false;
            bool fontSizeSet = false;

            while (xamlReader.MoveToNextAttribute())
            {
                string css = null;

                switch (xamlReader.Name)
                {
                    // Character fomatting properties
                    // ------------------------------
                    case "Background":
                        css = "background-color:" + ParseXamlColor(xamlReader.Value) + ";";
                        break;
                    case "FontFamily":
                        css = "font-family:" + xamlReader.Value + ";";
                        fontSet = true;
                        break;
                    case "FontStyle":
                        css = "font-style:" + xamlReader.Value.ToLower() + ";";
                        break;
                    case "FontWeight":
                        css = "font-weight:" + xamlReader.Value.ToLower() + ";";
                        break;
                    case "FontStretch":
                        break;
                    case "FontSize":
                        css = "font-size:" + xamlReader.Value + ";";
                        fontSizeSet = true;
                        break;
                    case "Foreground":
                        css = "color:" + ParseXamlColor(xamlReader.Value) + ";";
                        break;
                    case "TextDecorations":
                        css = "text-decoration:underline;";
                        break;
                    case "TextEffects":
                        break;
                    case "Emphasis":
                        break;
                    case "StandardLigatures":
                        break;
                    case "Variants":
                        break;
                    case "Capitals":
                        break;
                    case "Fraction":
                        break;

                    // Paragraph formatting properties
                    // -------------------------------
                    case "Padding":
                        css = "padding:" + ParseXamlThickness(xamlReader.Value) + ";";
                        break;
                    case "Margin":
                        css = "margin:" + ParseXamlThickness(xamlReader.Value) + ";";
                        break;
                    case "BorderThickness":
                        css = "border-width:" + ParseXamlThickness(xamlReader.Value) + ";";
                        borderSet = true;
                        break;
                    case "BorderBrush":
                        css = "border-color:" + ParseXamlColor(xamlReader.Value) + ";";
                        borderSet = true;
                        break;
                    case "LineHeight":
                        break;
                    case "TextIndent":
                        css = "text-indent:" + xamlReader.Value + ";";
                        break;
                    case "TextAlignment":
                        css = "text-align:" + xamlReader.Value + ";";
                        break;
                    case "IsKeptTogether":
                        break;
                    case "IsKeptWithNext":
                        break;
                    case "ColumnBreakBefore":
                        break;
                    case "PageBreakBefore":
                        break;
                    case "FlowDirection":
                        break;

                    // Table attributes
                    // ----------------
                    case "Width":
                        css = "width:" + String.Format("{0:0}", xamlReader.Value) + ";";
                        break;

                    case "Height":
                        css = "height:" + String.Format("{0:0}", xamlReader.Value) + ";";
                        break;

                    case "ColumnSpan":
                        htmlWriter.WriteAttributeString("COLSPAN", xamlReader.Value);
                        break;
                    case "RowSpan":
                        htmlWriter.WriteAttributeString("ROWSPAN", xamlReader.Value);
                        break;

                    case "NavigateUri":
                    //case "ToolTip":
                        htmlWriter.WriteAttributeString("HREF", xamlReader.Value);
                        break;

                    case "Source":
                        htmlWriter.WriteAttributeString("src", xamlReader.Value);
                        break;

                    default:
                        break;
                }

                if (css != null)
                {
                    inlineStyle.Append(css);
                }
            }

            if (borderSet)
            {
                inlineStyle.Append("border-style:solid;mso-element:para-border-div;");
            }

            if (!fontSet)
            {
                inlineStyle.Append("font-family:tahoma;");
            }

            if (!fontSizeSet)
            {
                inlineStyle.Append("font-size:11;");
            }

            // Return the xamlReader back to element level
            xamlReader.MoveToElement();
            Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);
        }

        private static string ParseXamlColor(string color)
        {
            if (color.StartsWith("#"))
            {
                // Remove transparancy value
                color = "#" + color.Substring(3);
            }
            return color;
        }

        private static string ParseXamlThickness(string thickness)
        {
            string[] values = thickness.Split(',');

            for (int i = 0; i < values.Length; i++)
            {
                double value;
                if (double.TryParse(values[i], out value))
                {
                    values[i] = Math.Ceiling(value).ToString();
                }
                else
                {
                    values[i] = "1";
                }
            }

            string cssThickness;
            switch (values.Length)
            {
                case 1:
                    cssThickness = thickness;
                    break;
                case 2:
                    cssThickness = values[1] + " " + values[0];
                    break;
                case 4:
                    cssThickness = values[1] + " " + values[2] + " " + values[3] + " " + values[0];
                    break;
                default:
                    cssThickness = values[0];
                    break;
            }

            return cssThickness;
        }

        /// <summary>
        /// Reads a content of current xaml element, converts it
        /// </summary>
        /// <param name="xamlReader">
        /// XmlTextReader which is expected to be at XmlNodeType.Element
        /// (opening element tag) position.
        /// </param>
        /// <param name="htmlWriter">
        /// May be null, in which case we are skipping the xaml element;
        /// witout producing any output to html.
        /// </param>
        /// <param name="inlineStyle">
        /// StringBuilder used for collecting css properties for inline STYLE attribute.
        /// </param>
        private static void WriteElementContent(XmlTextReader xamlReader, XmlTextWriter htmlWriter, StringBuilder inlineStyle, string htmlActualElementName)
        {
            Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

            bool elementContentStarted = false;

            if (xamlReader.IsEmptyElement)
            {
                if (htmlWriter != null && !elementContentStarted && inlineStyle.Length > 0)
                {
                    // Output STYLE attribute and clear inlineStyle buffer.
                    htmlWriter.WriteAttributeString("STYLE", inlineStyle.ToString());
                    inlineStyle.Remove(0, inlineStyle.Length);
                }
                elementContentStarted = true;
            }
            else
            {
                while (ReadNextToken(xamlReader) && xamlReader.NodeType != XmlNodeType.EndElement)
                {
                    switch (xamlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xamlReader.Name.Contains("."))
                            {
                                AddComplexProperty(xamlReader, inlineStyle,htmlWriter);
                            }
                            else
                            {
                                if (htmlWriter != null && !elementContentStarted && inlineStyle.Length > 0)
                                {
                                    // Output STYLE attribute and clear inlineStyle buffer.
                                    htmlWriter.WriteAttributeString("STYLE", inlineStyle.ToString());
                                    inlineStyle.Remove(0, inlineStyle.Length);
                                }
                                elementContentStarted = true;
                                WriteElement(xamlReader, htmlWriter, inlineStyle);
                            }
                            Debug.Assert(xamlReader.NodeType == XmlNodeType.EndElement || xamlReader.NodeType == XmlNodeType.Element && xamlReader.IsEmptyElement);
                            break;
                        case XmlNodeType.Comment:
                            if (htmlWriter != null)
                            {
                                if (!elementContentStarted && inlineStyle.Length > 0)
                                {
                                    htmlWriter.WriteAttributeString("STYLE", inlineStyle.ToString());
                                }
                                htmlWriter.WriteComment(xamlReader.Value);
                            }
                            elementContentStarted = true;
                            break;
                        case XmlNodeType.CDATA:
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                            if (htmlWriter != null)
                            {
                                if (!elementContentStarted && inlineStyle.Length > 0)
                                {
                                    htmlWriter.WriteAttributeString("STYLE", inlineStyle.ToString());
                                }
                                htmlWriter.WriteString(xamlReader.Value);
                            }
                            elementContentStarted = true;
                            break;

                        default:
                            break;
                    }
                }

                Debug.Assert(xamlReader.NodeType == XmlNodeType.EndElement);
            }
        }

        /// <summary>
        /// Conberts an element notation of complex property into
        /// </summary>
        /// <param name="xamlReader">
        /// On entry this XmlTextReader must be on Element start tag;
        /// on exit - on EndElement tag.
        /// </param>
        /// <param name="inlineStyle">
        /// StringBuilder containing a value for STYLE attribute.
        /// </param>
        private static void AddComplexProperty(XmlTextReader xamlReader, StringBuilder inlineStyle,XmlTextWriter htmlWriter)
        {
            Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

            if (inlineStyle != null)
            {
                if (xamlReader.Name.EndsWith(".TextDecorations"))
                {
                    inlineStyle.Append("text-decoration:underline;");
                }
                if (xamlReader.Name.EndsWith("Table.Columns"))
                {
                    ParseTableColumnsInfo(xamlReader);
                    if (null != mst_tableColumnsInfoActual && !double.IsNaN(mst_tableColumnsInfoActual.Width))
                    {
                        htmlWriter.WriteAttributeString("width", mst_tableColumnsInfoActual.Width.ToString());
                        htmlWriter.WriteAttributeString("align", "left");
                        htmlWriter.WriteAttributeString("vAlign", "top");
                    }
                    return;
                }
            }

            // Skip the element representing the complex property
            WriteElementContent(xamlReader, /*htmlWriter:*/null, /*inlineStyle:*/null,string.Empty);
        }

        private static TableColumnsInfo mst_tableColumnsInfoActual = null;

        private static void ParseTableColumnsInfo(XmlTextReader xamlReader)
        {
            mst_tableColumnsInfoActual = new TableColumnsInfo();

            while (ReadNextToken(xamlReader) && xamlReader.NodeType != XmlNodeType.EndElement)
            {
                if(xamlReader.Name.ToLower() == "tablecolumn")
                {
                    string strWidth = xamlReader.GetAttribute("Width");
                    if (!String.IsNullOrEmpty(strWidth))
                    {
                        mst_tableColumnsInfoActual.AddTableColumnInfo(new TableColumnInfo(strWidth.ToLower() == "auto" ? double.NaN : Convert.ToDouble(strWidth)));
                    }
                }
            }
            //if (xamlReader.NodeType == XmlNodeType.EndElement)
            //{
            //    ReadNextToken(xamlReader);
            //}
        }

        private static int mst_iCurTableRow = 0;
        /// <summary>
        /// Converts a xaml element into an appropriate html element.
        /// </summary>
        /// <param name="xamlReader">
        /// On entry this XmlTextReader must be on Element start tag;
        /// on exit - on EndElement tag.
        /// </param>
        /// <param name="htmlWriter">
        /// May be null, in which case we are skipping xaml content
        /// without producing any html output
        /// </param>
        /// <param name="inlineStyle">
        /// StringBuilder used for collecting css properties for inline STYLE attributes on every level.
        /// </param>
        private static void WriteElement(XmlTextReader xamlReader, XmlTextWriter htmlWriter, StringBuilder inlineStyle)
        {
            Debug.Assert(xamlReader.NodeType == XmlNodeType.Element);

            if (htmlWriter == null)
            {
                // Skipping mode; recurse into the xaml element without any output
                WriteElementContent(xamlReader, /*htmlWriter:*/null, null,string.Empty);
            }
            else
            {
                string htmlElementName = null;

                switch (xamlReader.Name)
                {
                    case "Run" :
                    case "Span":
                        //htmlElementName = "SPAN";
                        htmlElementName = (xamlReader.IsEmptyElement ? "BR" : "SPAN");
                        break;
                    case "InlineUIContainer":
                        htmlElementName = "SPAN";
                        break;
                    case "Bold":
                        htmlElementName = "B";
                        break;
                    case "Italic" :
                        htmlElementName = "I";
                        break;
                    case "Paragraph" :
                        htmlElementName = (xamlReader.IsEmptyElement ? "BR" : "DIV");
                        //htmlElementName = "P";
                        break;
                    case "BlockUIContainer":
                        htmlElementName = "DIV";
                        break;
                    case "Section":
                        htmlElementName = "DIV";
                        break;
                    case "Table":
                        htmlElementName = "TABLE";
                        break;
                    case "TableColumn":
                        htmlElementName = "COL";
                        break;
                    case "TableRowGroup" :
                        htmlElementName = "TBODY";
                        break;
                    case "TableRow" :
                        htmlElementName = "TR";
                        mst_iCurTableRow = 0;
                        break;

                    case "TableCell" :
                        htmlElementName = "TD";
                        break;

                    case "List" :
                        string marker = xamlReader.GetAttribute("MarkerStyle");
                        if (marker == null || marker == "None" || marker == "Disc" || marker == "Circle" || marker == "Square" || marker == "Box")
                        {
                            htmlElementName = "UL";
                        }
                        else
                        {
                            htmlElementName = "OL";
                        }
                        break;
                    case "ListItem" :
                        htmlElementName = "LI";
                        break;

                    case "Hyperlink":
                        htmlElementName = "A";
                        break;

                    case "Image" :
                        htmlElementName = "IMG";
                        break;

                    case "Path" :
                        htmlElementName = "HR";
                        break;
                    
                    default :
                        htmlElementName = null; // Ignore the element
                        break;
                }

                if (htmlWriter != null && htmlElementName != null)
                {
                    htmlWriter.WriteStartElement(htmlElementName);

                    if (null != mst_tableColumnsInfoActual && htmlElementName == "TD" && !double.IsNaN(mst_tableColumnsInfoActual.Width))
                    {
                        TableColumnInfo tci = mst_tableColumnsInfoActual.GetTableColumnInfo(mst_iCurTableRow);
                        htmlWriter.WriteAttributeString("width", mst_tableColumnsInfoActual.GetTableColumnInfo(mst_iCurTableRow).Width.ToString());
                        htmlWriter.WriteAttributeString("align", "left");
                        htmlWriter.WriteAttributeString("vAlign", "top");

                        mst_iCurTableRow++;
                    }

                    //### Formating properties
                    WriteFormattingProperties(xamlReader, htmlWriter, inlineStyle);

                    WriteElementContent(xamlReader, htmlWriter, inlineStyle,htmlElementName);

                    if (null != mst_tableColumnsInfoActual && htmlElementName == "TABLE")
                    {
                        mst_tableColumnsInfoActual.Clear();
                        mst_tableColumnsInfoActual = null;
                        mst_iCurTableRow = 0;
                    }

                    htmlWriter.WriteEndElement();
                }
                else
                {
                    // Skip this unrecognized xaml element
                    WriteElementContent(xamlReader, /*htmlWriter:*/null, null,string.Empty);
                }
            }
        }

        // Reader advance helpers
		// ----------------------
				 
        /// <summary>
        /// Reads several items from xamlReader skipping all non-significant stuff.
        /// </summary>
        /// <param name="xamlReader">
        /// XmlTextReader from tokens are being read.
        /// </param>
        /// <returns>
        /// True if new token is available; false if end of stream reached.
        /// </returns>
		private static bool ReadNextToken(XmlReader xamlReader)
		{
			while (xamlReader.Read())
			{
				Debug.Assert(xamlReader.ReadState == ReadState.Interactive, "Reader is expected to be in Interactive state (" + xamlReader.ReadState + ")");
				switch (xamlReader.NodeType)
				{
				    case XmlNodeType.Element: 
				    case XmlNodeType.EndElement:
				    case XmlNodeType.None:
				    case XmlNodeType.CDATA:
				    case XmlNodeType.Text:
				    case XmlNodeType.SignificantWhitespace:
					    return true;

				    case XmlNodeType.Whitespace:
					    if (xamlReader.XmlSpace == XmlSpace.Preserve)
					    {
						    return true;
					    }
					    // ignore insignificant whitespace
					    break;

				    case XmlNodeType.EndEntity:
				    case XmlNodeType.EntityReference:
                        //  Implement entity reading
					    //xamlReader.ResolveEntity();
					    //xamlReader.Read();
					    //ReadChildNodes( parent, parentBaseUri, xamlReader, positionInfo);
                        break; // for now we ignore entities as insignificant stuff

                    case XmlNodeType.Comment:
                        return true;
                    case XmlNodeType.ProcessingInstruction:
				    case XmlNodeType.DocumentType:
				    case XmlNodeType.XmlDeclaration:
				    default:
					    // Ignorable stuff
					    break;
				}
            }
            return false;
        }

        #endregion Private Methods

        // ---------------------------------------------------------------------
        //
        // Private Fields
        //
        // ---------------------------------------------------------------------

        #region Private Fields

        #endregion Private Fields
    }
}
