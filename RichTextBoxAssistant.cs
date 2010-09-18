using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ProverbTeleprompter
{
    public class RichTextboxAssistant : DependencyObject
    {

        public static FlowDocument GetDocument(DependencyObject obj) { return (FlowDocument)obj.GetValue(DocumentProperty); }
        public static void SetDocument(DependencyObject obj, FlowDocument value) { obj.SetValue(DocumentProperty, value); }

        public static readonly DependencyProperty DocumentProperty =
            DependencyProperty.RegisterAttached("Document", typeof (FlowDocument),
                                                typeof (RichTextboxAssistant),
                                                new FrameworkPropertyMetadata
                                                    {
                                                        BindsTwoWayByDefault = true,
                                                        PropertyChangedCallback = DocumentChanged
                                                    });


        private static void DocumentChanged(DependencyObject obj,
                    DependencyPropertyChangedEventArgs e)
        {
            var rtb = obj as RichTextBox;
            rtb.Document = e.NewValue as FlowDocument;
        }

        public FlowDocument Document
        {
            get { return GetValue(DocumentProperty) as FlowDocument; }
            set { SetValue(DocumentProperty, value); }
        }

    }
}
