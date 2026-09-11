using System;
using Gsheets.IO;
using Unity.Collections;

namespace Gsheets.Parsing
{
    [ParserTrigger(typeof(NativeArray<>))]
    public class Format_NativeArray<T> : IParserFormatter where T : unmanaged
    {
        public object ToData(string content)
        {
            var items = StringArray.Convert(content);
            NativeArray<T> array = new NativeArray<T>(items.Count, Allocator.Temp);
            for (int i = 0; i < items.Count; i++)
                array[i] = (T)ParserFormatter.Get(typeof(T)).ToData(items[i]);
            return array;
        }

        public void Write(string content, GSheetBinaryWriter writer)
        {
            writer.Write((NativeArray<T>)ToData(content));
        }
    }

    [ParserTrigger(typeof(NativeReference<>))]
    public class Format_NativeReference<T> : IParserFormatter where T : unmanaged
    {
        public object ToData(string content) => ParserFormatter.Get(typeof(T)).ToData(content);

        public void Write(string content, GSheetBinaryWriter writer) => 
            ParserFormatter.Get(typeof(T)).Write(content, writer);
        
    }
    
    [ParserTrigger(typeof(FixedString32Bytes))]
    public class Format_FixedString32 : IParserFormatter
    {
        public object ToData(string content) => new FixedString32Bytes(content);
        public void Write(string content, GSheetBinaryWriter writer) => 
            writer.Write( new FixedString32Bytes(content));
    }
    
    [ParserTrigger(typeof(FixedString64Bytes))]
    public class Format_FixedString64 : IParserFormatter
    {
        public object ToData(string content) => new FixedString64Bytes(content);
        public void Write(string content, GSheetBinaryWriter writer) => 
            writer.Write( new FixedString64Bytes(content));
    }
    
    [ParserTrigger(typeof(FixedString128Bytes))]
    public class Format_FixedString128 : IParserFormatter
    {
        public object ToData(string content) => new FixedString128Bytes(content);
        public void Write(string content, GSheetBinaryWriter writer) => 
            writer.Write( new FixedString128Bytes(content));
    }
    
    [ParserTrigger(typeof(FixedString512Bytes))]
    public class Format_FixedString512 : IParserFormatter
    {
        public object ToData(string content) => new FixedString512Bytes(content);
        public void Write(string content, GSheetBinaryWriter writer) => 
            writer.Write( new FixedString512Bytes(content));
    }
    
    [ParserTrigger(typeof(FixedString4096Bytes))]
    public class Format_FixedString4096 : IParserFormatter
    {
        public object ToData(string content) => new FixedString4096Bytes(content);
        public void Write(string content, GSheetBinaryWriter writer) => 
            writer.Write( new FixedString4096Bytes(content));
    }

}