using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Gsheets.Parsing
{
    
    [InitializeOnLoad]
    public static class ParserFormatter
    {
        private static Dictionary<Type, IParserFormatter> _singleParser;
        private static Dictionary<Type, GenericParserFactory> _parserFactories;
        private static Format_String _stringFormat;
        private static GenericParserFactory _arrayFormat;
        private static GenericParserFactory _enumFormat;
        private static GenericParserFactory _primitiveFormat;
        
        static ParserFormatter()
        {
            RefreshParser();
            _stringFormat = new Format_String();
            _arrayFormat = new GenericParserFactory(typeof(Format_ArrayFormatter<>));
            _enumFormat = new GenericParserFactory(typeof(Format_Enum<>));
            _primitiveFormat = new GenericParserFactory(typeof(Format_Primitive<>));
        }
        
        /// <summary>
        /// 미리 정의된 타입에 해당하는 포매터를 가져와 반환합니다.
        /// </summary>
        public static IParserFormatter Get(Type type)
        {
            IParserFormatter formatter = null;
            if (type == typeof(string))
                return _stringFormat;
            if (type.IsGenericType)
            {
                if(_parserFactories.TryGetValue(type.GetGenericTypeDefinition(), out var factory))
                    return factory.GetFormatter(type.GetGenericArguments()[0]);
            }
            if (type.IsArray)
                return _arrayFormat.GetFormatter(type.GetElementType());
            if (type.IsEnum)
                return _enumFormat.GetFormatter(type);
            if (type.IsPrimitive)
                return _primitiveFormat.GetFormatter(type);
            
            _singleParser.TryGetValue(type, out formatter);
            
            if(formatter == null)
                throw new Exception($"{type.Name} formatter not found");
            return formatter;
        }
      
        private static void AddManual(IParserFormatter formatter)
        {
            var trigger = GetTriggerType(formatter.GetType());
            _singleParser.Add(trigger, formatter);
        }

        private static Type GetTriggerType(Type type, bool ignoreError = false)
        {
            var attribute = type.GetCustomAttribute<ParserTriggerAttribute>();
            if (attribute == null) //Trigger타입 명시 없을경우 본인타입 반환,
                    return type;
            if(!ignoreError && attribute == null)
                throw new Exception($"{type.Name} Parser trigger not found, Use Attribute 'ParserTriggerAttribute'");
            return attribute?.TriggerType;
        }
        
        private static void RefreshParser()
        {
            _singleParser = new();
            _parserFactories = new Dictionary<Type, GenericParserFactory>();
            var types = TypeFinder.GetAssignableFroms<IParserFormatter>();
            foreach (var formatterType in types)
            {
                if(formatterType.IsAbstract || formatterType.IsInterface)
                    continue;
                if (formatterType.IsGenericType)
                {
                    if(formatterType.GetGenericArguments().Length != 1)
                        throw new Exception($"{formatterType.Name} formatter type doesn't have 1 generic argument");
                    
                    if (!_parserFactories.ContainsKey(formatterType))
                    {
                        var trigger = GetTriggerType(formatterType, true);
                        if(trigger != null)
                            _parserFactories.Add(trigger, new GenericParserFactory(formatterType));
                    }
                }
                else
                {
                    var formatter = (IParserFormatter)Activator.CreateInstance(formatterType);
                    AddManual(formatter);
                }
            }
        }
    }
}