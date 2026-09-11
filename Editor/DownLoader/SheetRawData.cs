using System;
using System.Collections.Generic;
using System.Linq;
using Gsheets.Editor.Generator;
using Gsheets.IO;
using Gsheets.Parsing;
using LWSerializer;
using Unity.Collections;
using UnityEngine;

namespace Gsheets.Editor.DownLoader
{
    public class SheetRawData
    {
        delegate void SplitRowStringForEachHandler(string str, int index);
        private int _columnCount;
        private GSheetInfo _gSheetInfo;
        private List<string[]> _rows;
        private List<HeaderType> _headers;
        
        public string SheetName => _gSheetInfo.SheetName;
        public Type SheetNameToType => TypeFinder.Find(SheetName);
        public List<string[]> Rows => _rows;
        public List<HeaderType> Headers => _headers;
        public string TypeKeyword => _rows.First().First().ToLower();
        
        public SheetRawData(GSheetInfo info, string csvData)
        {
            _gSheetInfo = info;
            if (string.IsNullOrEmpty(csvData))
                return;
            _rows = new();
            List<int> ignoreIdxList = new List<int>();
            var rows = csvData.Split("\r\n");
            var length = SearchIgnoreColumns(rows[0], ignoreIdxList);
            foreach (var row in rows)
            {
                string[] strs = new string[length];
                if(IsAnnotation(row))
                    continue;
                int idx = 0;
                SplitRowString(row, (str, index) =>
                {
                    if (!ignoreIdxList.Contains(index))
                        strs[idx++] = str;
                });
                _rows.Add(strs);
            }
            RefreshHeaderRowType();
            _gSheetInfo = _gSheetInfo.UpdateInfo(_rows.Count - 1, IsDictionary());
        }

        public bool IsExternalSheet()
        {
            return TypeKeyword == "external";
        }
        
        public bool IsDictionary()
        {
            for (int i = 1; i < _rows.Count; i++)
            {
                if (_rows[i].FirstOrDefault() != "")
                    return true;
            }

            return false;
        }
        
        public bool IsValidation()
        {
            if (_rows.Count == 0)
                return false;
            if (_rows.Count < 2)
            {
                Debug.LogError($"{SheetName}' row more than once");
                return false;
            }
            if (_rows[0].Length < 3)
            {
                Debug.LogError($"{SheetName}' column more than two");
                return false;
            }
            return true;
        }

        public TypeModel ClassGenerator(string nameSpace)
        {
            return new TypeModel(this, nameSpace);
        }

        public void WriteDirect(GSheetBinaryWriter writer)
        {
            writer.Write(_gSheetInfo);
            for (int i = 1; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (IsDictionary())
                    writer.Write(row[0]);
                for (int j = 1; j < row.Length; j++)
                {
                    var contentType = _headers[j].type;
                    ParserFormatter.Get(contentType).Write(row[j], writer);
                }
            }
            writer.WritePadding(32);
        }

        #region private
        void RefreshHeaderRowType()
        {
            var header = _rows[0];
            _headers = new();
            foreach (var row in header)
            {
                _headers.Add(new HeaderType(row));
            }
            _headers[0] = new HeaderType("string");
        }
        
        bool IsAnnotation(string strs) => strs.Length >= 2 && strs.Substring(0, 2) == GSheetSettingScriptable.AnnotationText;

        int SearchIgnoreColumns(string firstRow, List<int> ignoreColumIdxs)
        {
            var length = 0;
            SplitRowString(firstRow, (str, index) =>
            {
                if (IsAnnotation(str))
                    ignoreColumIdxs.Add(index);
                else
                    length++;
            });
            return length;
        }
        
        void SplitRowString(string row, SplitRowStringForEachHandler caller)
        {
            int splitIdx = 0;
            int startIdx = 0;
            int len = 0;
            bool strOpenState = false;
            for (int i = 0; i < row.Length; i++)
            {
                if (!strOpenState && row[i] == ',')
                {
                    caller.Invoke(len == 0 ? "" : row.Substring(startIdx, len), splitIdx++);
                    startIdx = i + 1;
                    len = 0;
                }
                else
                {
                    if (row[i] == '"')
                    {
                        if (!strOpenState)
                            startIdx++;
                        strOpenState = !strOpenState;
                    }
                    else
                        len++;
                }
            }
            caller.Invoke(len == 0 ? "" : row.Substring(startIdx, len), splitIdx++);
        }
        #endregion

        
    }
}
