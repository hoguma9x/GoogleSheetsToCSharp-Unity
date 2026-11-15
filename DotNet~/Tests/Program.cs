using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gsheets.IO;
using Gsheets.Compatibility;
using Generator;
using LWSerializer;
using Examples;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

static void Check(bool ok, string message)
{
    if (!ok) throw new Exception(message);
}
static void Reject<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Expected " + typeof(T).Name);
}

var bytes = File.ReadAllBytes(args[0]);
var types = new[] { typeof(ExampleClass), typeof(TestExternalData), typeof(ExampleStruct), typeof(CustomTypes), typeof(Localize) };
var data = GSheetData.FromBytes(bytes, types);
var builder = WebApplication.CreateBuilder();
builder.Services.AddSingleton(data);
using var app = builder.Build();
app.MapGet("/units", (GSheetData sheets) => sheets.GetDictionary<ExampleStruct>("ExampleStruct"));
Check(ReferenceEquals(app.Services.GetRequiredService<GSheetData>(), data), "ASP.NET Core singleton differs");
var generated = Gsheet.LoadFromBytes(bytes);
Check(data.GetList<ExampleClass>("ExampleClass").Count > 0, "List rows missing");
Check(data.GetList<TestExternalData>("TestExternalData").Count > 0, "External rows missing");
var structs = data.GetDictionary<ExampleStruct>("ExampleStruct");
Check(structs.ContainsKey("unit_100"), "Dictionary key missing");
Check(structs["unit_100"].nativeArr.Length > 0, "NativeArray conversion failed");
Check(structs["unit_100"].testfxStr.ToString().Length > 0, "FixedString conversion failed");
Check(data.GetList<CustomTypes>("CustomTypes")[0].c_class != null, "Custom class missing");
Check(data.GetDictionary<Localize>("Localize")["ui_ok"].KR.Length > 0, "Localization missing");
Check(generated._ExampleStruct["unit_100"].a == structs["unit_100"].a, "Generated loader differs");
var fromFile = Gsheet.LoadFromFile(args[0]);
Check(fromFile._ExampleClass.Count == generated._ExampleClass.Count, "File loader differs");
Parallel.For(0, 20, _ => Check(Gsheet.LoadFromBytes(bytes)._Localize.Count == generated._Localize.Count, "Concurrent load differs"));

var wire = GSheetData.FromFile(args[1], typeof(WireRow)).GetDictionary<WireRow>("WireRow")["test"];
Check(wire.Name == "한글" && wire.Hp == 42, "Unity scalar/string wire differs");
Check(wire.Direction.x == 1.25f && wire.Direction.y == -2.5f, "Unity vector layout differs");
Check(wire.Values.SequenceEqual(new[] { 3, 7 }), "NativeArray wire differs");
Check(wire.Label.ToString() == "공유" && wire.Reference == 99, "FixedString/NativeReference wire differs");
Reject<InvalidDataException>(() => GSheetData.FromBytes(new byte[0], types));
Reject<InvalidDataException>(() => GSheetData.FromBytes(bytes, typeof(ExampleClass)));
Reject<ArgumentException>(() => GSheetData.FromBytes(bytes, typeof(ExampleClass), typeof(ExampleClass)));
Reject<ArgumentException>(() => GSheetData.FromBytes(bytes, typeof(string)));
var wrongCount = (byte[])bytes.Clone();
BitConverter.GetBytes(0).CopyTo(wrongCount, 0);
Reject<InvalidDataException>(() => Gsheet.LoadFromBytes(wrongCount));
Console.WriteLine("PASS: ASP.NET Core DI, Unity bytes, generated loaders, list/dictionary/external/custom/localization, portable layouts, concurrent loads, invalid registrations.");

public class WireRow : ILwSerializable
{
    public string Name;
    public int Hp;
    public Vector2 Direction;
    public int[] Values;
    public FixedString32Bytes Label;
    public int Reference;
    public void OnNativeRead(LwBinaryReader r) => r.Read(out Name, out Hp, out Direction, out Values, out Label, out Reference);
    public void OnNativeWrite(LwBinaryWriter w)
    {
        w.Write(Name); w.Write(Hp); w.Write(Direction); w.Write(Values); w.Write(Label); w.Write(Reference);
    }
}
