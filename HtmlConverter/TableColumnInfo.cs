using System.Collections.Generic;
using System.Diagnostics;

namespace ProverbTeleprompter.HtmlConverter
{
    internal class TableColumnsInfo
    {
        private List<TableColumnInfo> m_listTableColumnInfo = new List<TableColumnInfo>();

        public double Width { get; set; }

        public TableColumnsInfo()
        {
            this.Width = double.NaN;
        }

        public void AddTableColumnInfo(TableColumnInfo tableColumnInfo)
        {
            this.m_listTableColumnInfo.Add(tableColumnInfo);

            if (!double.IsNaN(tableColumnInfo.Width))
            {
                if (double.IsNaN(this.Width))
                {
                    this.Width = 0;
                }
                this.Width += tableColumnInfo.Width;
            }
        }

        public TableColumnInfo GetTableColumnInfo(int iIndex)
        {
            Debug.Assert(this.m_listTableColumnInfo.Count > iIndex, "Invalid index!");
            if(this.m_listTableColumnInfo.Count <= iIndex)
            {
                return null;
            }
            return this.m_listTableColumnInfo[iIndex];
        }

        public void Clear()
        {
            this.m_listTableColumnInfo.Clear();
        }
    }

    internal class TableColumnInfo
    {
        public double Width { get; set; }

        public TableColumnInfo(double dWidth)
        {
            this.Width = dWidth;
        }

    }
}
