using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gsheets.Editor.DownLoader;
using Gsheets.Editor.Generator;
using Gsheets.IO;
using LWSerializer;

public static class GenerateSmoke
{
    public static void Main(string[] args)
    {
        var samples = Assembly.LoadFrom(args[0]);
        var output = args[1];
        Directory.CreateDirectory(output);
        var sheets = new List<SheetRawData>();
        foreach (var field in samples.GetType("Generator.Gsheet").GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            var collection = field.FieldType;
            var dictionary = collection.GetGenericTypeDefinition() == typeof(Dictionary<,>);
            var row = collection.GetGenericArguments().Last();
            var external = row.Namespace != "Generator";
            var headers = row.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(f => TypeName(f.FieldType) + " " + f.Name.TrimStart('_'));
            var sheet = new SheetRawData(new GSheetInfo(row.Name, "0"),
                (external ? "external" : row.IsValueType ? "struct" : "class") + "," + string.Join(",", headers) +
                "\r\n" + (dictionary ? "key" : "") + "," + string.Join(",", headers.Select(_ => "0")));
            sheets.Add(sheet);
            if (!external) File.WriteAllText(Path.Combine(output, row.Name + ".cs"), new TypeModel(sheet, "Generator").Generator());
        }
        File.WriteAllText(Path.Combine(output, "Gsheet.cs"), new GSheetModel(sheets.ToArray(), "Generator").Generator());

        // A Unity-compiled writer produces a known fixture with the same per-column format as WriteDirect.
        using (var writer = new LwBinaryWriter())
        {
            writer.Write(1);
            writer.Write(new GSheetInfo("WireRow", "0").UpdateInfo(1, true));
            writer.Write("test");
            writer.Write("한글");
            writer.Write(42);
            writer.Write(new UnityEngine.Vector2(1.25f, -2.5f));
            writer.Write(new int[] { 3, 7 }); // NativeArray<T> uses the same length + unmanaged block.
            writer.Write(new Unity.Collections.FixedString32Bytes("공유"));
            writer.Write(99); // NativeReference<int> writes its value.
            writer.WritePadding(32);
            File.WriteAllBytes(Path.Combine(output, "wire.bytes"), writer.ToArray());
        }
        Console.WriteLine("Generated shared models and Unity wire fixture.");
    }

    private static string TypeName(Type type)
    {
        if (type.IsArray) return TypeName(type.GetElementType()) + "[]";
        if (type.IsGenericType) return type.Name.Split('`')[0] + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(string)) return "string";
        return type.Name;
    }
}
