using LWSerializer;
using Gsheets.Compatibility;

// Server-side equivalents of the example project's custom contracts. The application owns these types.
namespace Examples
{
    public enum CustomEnum { Array, String, Boolean, Number, Json }

    public class TestExternalData : ILwSerializable
    {
        public string _localizeName;
        public float _time;
        public CustomEnum _property;
        public int[] _arr;
        public CustomEnum[] _properties;
        public void OnNativeRead(LwBinaryReader r) => r.Read(out _localizeName, out _time, out _property, out _arr, out _properties);
        public void OnNativeWrite(LwBinaryWriter w) => w.Write(_localizeName, _time, _property, _arr, _properties);
    }

    public struct CustomStruct : ILwSerializable
    {
        public FixedString32Bytes monsterName;
        public CustomEnum property;
        public float speed;
        public void OnNativeRead(LwBinaryReader r) => r.Read(out monsterName, out property, out speed);
        public void OnNativeWrite(LwBinaryWriter w) => w.Write(monsterName, property, speed);
    }

    public class CustomClass : ILwSerializable
    {
        public string Name;
        public int Number;
        public void OnNativeRead(LwBinaryReader r) => r.Read(out Name, out Number);
        public void OnNativeWrite(LwBinaryWriter w) => w.Write(Name, Number);
    }

    public class SpecialClass : ILwSerializable
    {
        public string Name;
        public float[] Values;
        public void OnNativeRead(LwBinaryReader r) => r.Read(out Name, out Values);
        public void OnNativeWrite(LwBinaryWriter w) => w.Write(Name, Values);
    }
}
