using System;
using Gsheets.IO;
using LWSerializer;
using Gsheets.Editor.DownLoader;

namespace Gsheets.Parsing
{
    [ParserTrigger(typeof(string))]
    public class Format_String : IParserFormatter
    {
        public object ToData(string content)
        {
            return content;
        }

        public void Write(string content, GSheetBinaryWriter writer)
        {
            writer.Write(content);
        }
        
    }
    
    
    public class Format_Primitive<T> : IParserFormatter
    {
        public object ToData(string content)
        {
            if (content == "")
                return default(T);
            return Convert.ChangeType(content, typeof(T));
        }
        
        public void Write(string content, GSheetBinaryWriter writer)
        {
            writer.Write((T)ToData(content));
        }
    }
}
