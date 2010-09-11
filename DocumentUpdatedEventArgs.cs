using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProverbTeleprompter
{
    public class DocumentUpdatedEventArgs : EventArgs
    {
        public Stream DocumentData { get; private set; }

        public string DataFormat { get; private set; }

        public DocumentUpdatedEventArgs(Stream documentData, string dataFormat)
        {
            DocumentData = documentData;
            DataFormat = dataFormat;
        }
    }
}
