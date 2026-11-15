using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LWSerializer;

namespace Gsheets.IO
{
    /// <summary>A file snapshot loaded with explicitly registered row types, independent of Unity settings.</summary>
    public sealed class GSheetData
    {
        private readonly Dictionary<string, object> _sheets = new Dictionary<string, object>(StringComparer.Ordinal);

        public static GSheetData FromFile(string path, params Type[] sheetTypes)
            => FromBytes(File.ReadAllBytes(path), sheetTypes);

        public static GSheetData FromBytes(byte[] data, params Type[] sheetTypes)
        {
            if (sheetTypes == null) throw new ArgumentNullException(nameof(sheetTypes));
            var types = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in sheetTypes)
            {
                if (type == null || !typeof(ILwSerializable).IsAssignableFrom(type) || type.IsAbstract ||
                    (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null))
                    throw new ArgumentException("Row types must implement ILwSerializable and have a public parameterless constructor.", nameof(sheetTypes));
                if (types.ContainsKey(type.Name))
                    throw new ArgumentException($"Duplicate sheet type name: {type.Name}", nameof(sheetTypes));
                types.Add(type.Name, type);
            }

            var result = new GSheetData();
            using (var reader = GSheetBinaryReader.FromBytes(data))
            {
                reader.Read(out int count);
                if (count < 0 || count > data.Length / 32)
                    throw new InvalidDataException("Invalid GSheet sheet count.");
                for (int i = 0; i < count; i++)
                {
                    string name = null;
                    Type rowType = null;
                    var sheet = GSheetHelper.ReadSheet(reader, sheetName =>
                    {
                        name = sheetName;
                        types.TryGetValue(sheetName, out rowType);
                        return rowType;
                    });
                    if (result._sheets.ContainsKey(name))
                        throw new InvalidDataException($"Duplicate sheet: {name}");
                    if (sheet is IDictionary dictionary)
                    {
                        var target = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), rowType));
                        foreach (DictionaryEntry entry in dictionary) target.Add(entry.Key, entry.Value);
                        result._sheets.Add(name, target);
                    }
                    else
                    {
                        var target = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(rowType));
                        foreach (var row in (IList)sheet) target.Add(row);
                        result._sheets.Add(name, target);
                    }
                }
            }
            return result;
        }

        public IReadOnlyList<T> GetList<T>(string sheetName) => (List<T>)_sheets[sheetName];
        public IReadOnlyDictionary<string, T> GetDictionary<T>(string sheetName)
            => (Dictionary<string, T>)_sheets[sheetName];
    }
}
