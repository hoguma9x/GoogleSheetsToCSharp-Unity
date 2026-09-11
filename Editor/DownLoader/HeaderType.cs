using System;
using System.Linq;
using LWSerializer;
using Gsheets.Editor.Generator;
using Unity.Collections;

namespace Gsheets.Editor.DownLoader
{
    public class HeaderType
    {
        private readonly Type LwSerializableType = typeof(ILwSerializable);
        
        public string originalText { get; private set; }
        public string typeString { get; private set; }
        
        public Type type { get; private set; }
        public Type genericType { get; private set; }
        
        public string memberName { get; private set; }
        public bool IsUnmanaged { get; private set; }
        public bool IsMissingType { get; private set; }
        public bool IsLwSerializable { get; private set; }
        public bool IsArray => typeString.Contains("[]");
        public bool IsDisposable { get; private set; }

        public HeaderType(string headerText)
        {
            originalText = headerText;
            typeString = originalText.Split(' ').First();
            memberName = originalText.Split(' ').Last();
            if (typeString.Contains('<'))
            {
                var generic = TypeFinder.GetGenericType(typeString);
                type = TypeFinder.Find($"{generic.containerType}`1");
                genericType = TypeFinder.Find(generic.genericType);
                type = type.MakeGenericType(genericType);
            }
            else
            {
                type = TypeFinder.Find(typeString);
            }
            
            IsMissingType = type == null;
            if (IsMissingType)
            {
                IsUnmanaged = false;
                IsLwSerializable = false;
            }
            else
            {
                IsUnmanaged = TypeFinder.IsUnmanaged(type);
                IsLwSerializable = LwSerializableType.IsAssignableFrom(type);
                IsDisposable = typeof(IDisposable).IsAssignableFrom(type);
            }
            
        }
    }
}
