using System;
using Gsheets.IO;
using LWSerializer;

namespace Gsheets.Parsing
{
    /// <summary>
    /// 이 어트리뷰트가 선언된 구조체는 어트리뷰트에 명시된 타입이 LwSerializer에서 파싱될때 사용됩니다.
    /// 반드시 IParserFormatter를 구현한 구조체나 클래스여야합니다 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ParserTriggerAttribute : Attribute
    {
        public Type TriggerType { get; }
        public ParserTriggerAttribute(Type targetType) => TriggerType = targetType;
    }
    
    public interface IParserFormatter
    {
        public object ToData(string content);
        public void Write(string content, GSheetBinaryWriter writer);
    }

    
    /// <summary> 사용자 지정 형식에 대한 파싱방법을 제공합니다.  </summary>
    public interface IGSheetParser : IParserFormatter, ILwSerializable
    {
        
    }
    
    //ILwSerializable
    
    
}
