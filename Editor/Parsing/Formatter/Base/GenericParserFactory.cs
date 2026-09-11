using System;
using System.Collections.Generic;

namespace Gsheets.Parsing
{
    public class GenericParserFactory
    {
        private Type _genericType;
        private Dictionary<Type, IParserFormatter> _formatters;
        
        public Type GenericType => _genericType;
        
        public GenericParserFactory(Type genericType)
        {
            _genericType = genericType;
            _formatters = new Dictionary<Type, IParserFormatter>();
        }

        public IParserFormatter GetFormatter<T>() where T : IParserFormatter
        {
            return GetFormatter(typeof(T));
        }
        
        
        public IParserFormatter GetFormatter(Type argumentType)
        {
            if (!_formatters.TryGetValue(argumentType, out IParserFormatter formatter))
            {
                formatter = (IParserFormatter)Activator.CreateInstance
                    (_genericType.MakeGenericType(argumentType));
                _formatters.Add(argumentType, formatter);
            }
            return formatter;
        }
    }
}