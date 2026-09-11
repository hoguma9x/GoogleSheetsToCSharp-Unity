using System;
using Gsheets.IO;

namespace Gsheets.Parsing
{
    public class Format_Enum<T> : IParserFormatter where T : Enum
    {
        public object ToData(string content)
        {
            var result = Enum.TryParse(typeof(T), content, out var enumValue);
            return enumValue == null ? default(T) : enumValue;
        }

        public void Write(string content, GSheetBinaryWriter writer)
        {
            writer.Write((T)ToData(content));
        }
    }
}