using System;
using System.Collections.Generic;
using Gsheets.IO;

namespace Gsheets.Parsing
{
    /// <summary>
    /// 모든 배열을 포매팅해줌
    /// Type1 - a,b,c,d
    /// Type2 - {a,b},{c,b},{e,f}
    /// </summary>
    public class Format_ArrayFormatter<T> : IParserFormatter
    {
        public object ToData(string content)
        {
            var contents = StringArray.Convert(content);
            var elementParser = ParserFormatter.Get(typeof(T));
            T[] newArray = new T[contents.Count];
            for (int i = 0; i < contents.Count; i++)
            {
                var str = StringArray.RemoveBucket(contents[i]);
                newArray[i] = (T)elementParser.ToData(str);
            }
            return newArray;
        }

        public void Write(string content, GSheetBinaryWriter writer)
        {
            writer.Write((T[])ToData(content));
        }
    }
}