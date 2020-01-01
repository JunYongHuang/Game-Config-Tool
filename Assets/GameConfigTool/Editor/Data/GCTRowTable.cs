﻿using NPOI.SS.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GCT
{
    internal class GCTRowTable : IDictionary<string, object>
    {
        public IDictionary<string, object> Data = new SortedDictionary<string, object>();
        public IDictionary<string, bool> Client = new SortedDictionary<string, bool>();
        public IDictionary<string, bool> Server = new SortedDictionary<string, bool>();

        public IRow Row { get; private set; }
        public IRow TitleRow { get; private set; }
        public GCTSchema Schema { get; private set; }
        public GCTExcel Excel { get; private set; }

        #region IDictionary
        public object this[string key] { get { return Data[key]; } set { Data[key] = value; } }

        public ICollection<string> Keys { get { return Data.Keys; } }

        public ICollection<object> Values { get { return Data.Values; } }

        public int Count { get { return Data.Count; } }

        public bool IsReadOnly { get { return Data.IsReadOnly; } }

        public void Add(string key, object value)
        {
            throw new Exception("Cant Add");
            //Data.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            throw new Exception("Cant Add");
            //Data.Add(item);
        }

        public void Clear()
        {
            Data.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return Data.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return Data.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            Data.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return Data.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return Data.Remove(item);
        }

        public bool TryGetValue(string key, out object value)
        {
            return Data.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Data.GetEnumerator();
        }
        #endregion

        public GCTRowTable(IRow row, IRow titleRow, GCTExcel excel)
        {
            Row = row;
            TitleRow = titleRow;
            Schema = excel.Schema;
            Excel = excel;

            for (var column = 0; column < titleRow.LastCellNum;)
            {
                var titleCell = titleRow.GetCell(column);
                var cell = row.GetCell(column);
                var title = titleCell.StringCellValue;
                var maintitle = GetMainTitle(title);
                if (Schema.Fields.ContainsKey(maintitle) == false)
                {
                    Debugger.LogError("field null" + title);
                    column++;
                    continue;
                }
                var field = Schema.Fields[maintitle];
                if (Data.ContainsKey(field.Name))
                {
                    Debugger.LogError("title重复" + field.Name);
                }
                List<ICell> cells = null;
                if (row.Cells.Count >= column + field.ColumnCount)
                    cells = row.Cells.GetRange(column, field.ColumnCount);
                else
                {
                    cells = new List<ICell>();
                    for (var i = 0; i < field.ColumnCount; ++i)
                        cells.Add(null);
                }
                var titleCells = titleRow.Cells.GetRange(column, field.ColumnCount);
                var titles = new List<string>();
                for (var i = column; i < column + field.ColumnCount; ++i)
                    titles.Add(titleRow.Cells[i].StringCellValue);
                var fieldValue = field.Value(cells, titles);
                if (fieldValue != null)
                {
                    Data.Add(field.Name, fieldValue);
                    Client.Add(field.Name, field.IsClient);
                    Server.Add(field.Name, field.IsServer);
                }

                column += field.ColumnCount;
            }
        }
        
        public void Check()
        {
            for (var column = 0; column < TitleRow.LastCellNum;)
            {
                var titleCell = TitleRow.GetCell(column);
                var cell = Row.GetCell(column);
                var title = titleCell.StringCellValue;
                var maintitle = GetMainTitle(title);
                if (Schema.Fields.ContainsKey(maintitle) == false)
                {
                    Debugger.LogError("field null" + title);
                    column++;
                    continue;
                }
                var field = Schema.Fields[maintitle];
                List<ICell> cells = null;
                if (Row.Cells.Count >= column + field.ColumnCount)
                    cells = Row.Cells.GetRange(column, field.ColumnCount);
                else
                {
                    cells = new List<ICell>();
                    for (var i = 0; i < field.ColumnCount; ++i)
                        cells.Add(null);
                }
                var titleCells = TitleRow.Cells.GetRange(column, field.ColumnCount);
                var titles = new List<string>();
                for (var i = column; i < column + field.ColumnCount; ++i)
                    titles.Add(TitleRow.Cells[i].StringCellValue);
                if (field.Type.Check(cells, titles) == false)
                    Debugger.LogError(string.Format("检查失败 Excel:{0} Title:{1} 行号:{2}", Excel.name, field.Title, Row.RowNum));

                column += field.ColumnCount;
            }
        }

        private static int GetArrayLength(IRow titleRow, string name, int startColumn, int memberCount)
        {
            for (var length = 0; length < int.MaxValue;)
            {
                var nameStart = string.Format("{0}[{1}]", name, length);
                for (var member = 0; member < memberCount; ++member)
                {
                    var cell = titleRow.GetCell(length + member + startColumn);
                    if (cell == null || cell.StringCellValue.StartsWith(nameStart) == false)
                    {
                        if (member == 0)
                            return length;
                        else
                            throw new Exception("GetArrayLength Error");
                    }
                }
                length += memberCount;
            }
            return 0;
        }

        private static int GetCommonTypeLength(IRow titleRow, string name, int startColumn)
        {
            for (var length = 0; length < int.MaxValue;)
            {
                var cell = titleRow.GetCell(length + startColumn);
                if (cell.StringCellValue.StartsWith(name) == false)
                    return length;
            }
            return 0;
        }

        private static string GetMainTitle(string title)
        {
            string maintitle = title;
            var arrayIndex = title.IndexOf("[0]");
            if (arrayIndex > 0)
                maintitle = title.Substring(0, arrayIndex);

            var mainIndex = maintitle.IndexOf('-');
            if (mainIndex > 0)
                maintitle = maintitle.Substring(0, mainIndex);

            return maintitle;
        }
    }
}
