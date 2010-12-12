using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace ProverbTeleprompter.Helpers
{
    public static class DocumentHelpers
    {
        /// <summary>
        /// Sets a property value for each and every block or inline in a flow document.  optionally only sets the value
        /// on objects that have a specific prior value
        /// </summary>
        /// <param name="document"></param>
        /// <param name="propertyToChange"></param>
        /// <param name="newValue"></param>
        /// <param name="priorValue">if not null,only sets the new value on objects with the prior value</param>
        public static void ChangePropertyValue(FlowDocument document,DependencyProperty propertyToChange, object newValue, object priorValue = null)
        {
            //If no prior value provided, just apply color change to all text
            //if (priorValue == null)
            //{
            //    TextRange range = new TextRange(document.ContentStart, document.ContentEnd);
            //    range.ApplyPropertyValue(propertyToChange, newValue);
            //}
            //else
            {
                ChangePropertyValueRecursive(document.Blocks, propertyToChange, newValue, priorValue);

            }
        }

        private static void ChangePropertyValueRecursive(BlockCollection blocks,
            DependencyProperty propertyToChange, object newValue, object priorValue = null)
        {
            foreach (var block in blocks)
            {
                var currentValue = block.GetValue(propertyToChange);

                if (priorValue == null || priorValue.ValuesAreEqual(currentValue))
                {
                    block.SetValue(propertyToChange, newValue);
                }
                if (block is Paragraph)
                {
                    var para = block as Paragraph;
                    ChangePropertyValueRecursive(para.Inlines, propertyToChange, newValue, priorValue);
                }
                else if (block is List)
                {
                    var list = block as List;

                    foreach (var listItem in list.ListItems)
                    {
                        ChangePropertyValueRecursive(listItem.Blocks, propertyToChange, newValue, priorValue);
                    }
                }

            }
        }

        private static void ChangePropertyValueRecursive(InlineCollection inlines, DependencyProperty propertyToChange, object newValue, object priorValue = null)
        {

            foreach (var inline in inlines)
            {

                 var currentValue = inline.GetValue(propertyToChange);
                
                
                if (priorValue == null || priorValue.ValuesAreEqual(currentValue))
                {
                    inline.SetValue(propertyToChange, newValue);
                }

                var pi = inline.GetType().GetProperty("Inlines");
                if (pi != null)
                {
                    var value = pi.GetValue(inline, null);
                    if (value != null && value is InlineCollection)
                    {
                        ChangePropertyValueRecursive(value as InlineCollection, propertyToChange, newValue, priorValue);
                    }
                }


            }
        }

        public static bool ValuesAreEqual(this object item1, object item2)
        {
            if (item1 == null && item2 == null)
            {
                return true;
            }
            if (item1 == null || item2 == null)
            {
                return false;
            }

            return  item1.ToString() == item2.ToString();
        }


        public static void SaveDocument(Stream documentStream, FlowDocument document, string dataFormat)
        {
            TextRange range = new TextRange(document.ContentStart, document.ContentEnd);

            range.Save(documentStream, dataFormat, true);
            
            


        }

        public static void LoadDocument(Stream fileStream, FlowDocument document, string dataFormat)
        {
            try
            {
                TextRange range = new TextRange(document.ContentStart, document.ContentEnd);

                range.Load(fileStream, dataFormat);

            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Unsupported file type.");
            }
        }
        /// <summary>
        /// NOTE: This method is slow and inneficient.  DO not yet know a better way...
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static int GetLineNumberFromSelection(TextPointer position)
        {
            if (position == null)
            {
                return 0;
            }

            int lineNumber = 0;
            int linesMoved;
            do
            {
                position = position.GetLineStartPosition(-1, out linesMoved);
                lineNumber++;
            }
            while (position != null && linesMoved != 0);

            return lineNumber;
        }



    }
}
