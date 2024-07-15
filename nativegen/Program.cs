using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace NativeGen;

public static class Application
{
    private const string JSON_FILE_URI = "https://github.com/alloc8or/gta5-nativedb-data/blob/master/natives.json?raw=true";
    private const string OUTPUT_FILE = "Native.cs";

    public static void Main(string[] args)
    {
        using var client = new HttpClient();
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;

        Console.WriteLine("Downloading natives.json");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, sdch");

        var nativeFileRaw = Decompress(client.GetByteArrayAsync(JSON_FILE_URI).Result);
        var nativeTemplate = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NativeTemplate.txt"));

        var file = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, NativeFunction>>>(nativeFileRaw);
        var sb = new StringBuilder();

        foreach (var namespaceKey in file.Keys)
        {
            Console.WriteLine($"Processing {namespaceKey}");
            var nativeNamespace = file[namespaceKey];

            sb.Append("\t/*\n\t\t").Append(namespaceKey).Append("\n\t*/\n");
            foreach (var function in nativeNamespace)
            {
                var hasParams = function.Value.Params is not null && function.Value.Params.Count is not 0;
                var returnType = function.Value.ReturnType.ReplaceReturnType();

                // comment
                if (!string.IsNullOrEmpty(function.Value.Comment))
                {
                    sb.Append("\t/// <summary>\n");
                    sb.Append("\t/// \t").Append(function.Value.Comment.ReplaceXMLEscapeLetter().Replace("\n", "<br/>\n\t/// \t")).Append('\n');
                    sb.Append("\t/// </summary>\n");
                }
                // func
                sb.Append('\t').Append("public static ").Append(returnType.Replace("Any*", "int")).Append(' ').Append(function.Value.Name);
                sb.Append('(');
                if (hasParams)
                {
                    var length = function.Value.Params.Count;
                    for (int i = 0; i < length; i++)
                    {
                        var type = function.Value.Params[i].Type.ReplaceReturnType();
                        if (type is "Any" or "Any*") continue;
                        if (type.Contains('*')) sb.Append("out ");
                        sb.Append(type.Replace("*", "")).Append(' ').Append(function.Value.Params[i].Name.ReplaceVariableName()).Append(", ");
                    }
                }
                sb.Append(")\n\t{\n\t\t");
                if (returnType is not "void") sb.Append("return ");
                sb.Append("NativeFunction.Natives.").Append(function.Value.Name.ReplaceVariableName()).Append('(');
                if (hasParams)
                {
                    var length = function.Value.Params.Count;
                    for (int i = 0; i < length; i++)
                    {
                        var type = function.Value.Params[i].Type.ReplaceReturnType();
                        if (type is "Any" or "Any*")
                        {
                            sb.Append('0');
                        }
                        else
                        {
                            if (type.Contains('*')) sb.Append("out ");
                            sb.Append(function.Value.Params[i].Name.ReplaceVariableName());
                        }
                        sb.Append(", ");
                    }
                }
                sb.Append(");\n\t}\n");
            }
            sb.Replace(", )", ")");
        }

        File.WriteAllText(OUTPUT_FILE, string.Format(nativeTemplate, DateTime.Now, sb));
    }

    /// <summary>
    ///     This method is used to decompress the received GZIP-String
    /// </summary>
    /// <param name="gzip">compressed input</param>
    /// <returns>UTF-8 encoded and decompressed string</returns>
    private static string Decompress(byte[] gzip)
    {
        using var stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress);
        byte[] buffer = new byte[gzip.Length];

        using var memory = new MemoryStream();
        int count;

        do
        {
            count = stream.Read(buffer, 0, gzip.Length);

            if (count > 0)
            {
                memory.Write(buffer, 0, count);
            }
        }
        while (count > 0);

        return Encoding.UTF8.GetString(memory.ToArray());
    }

    private static string ReplaceReturnType(this string type)
    {
        return type switch
        {
            "BOOL" or "BOOL*" => "bool",
            "const char*" => "string",
            "Hash" => "ulong",
            "Cam" => "Camera",
            "Pickup" or "Interior" or "ScrHandle" or "ScrHandle*" => "uint",
            "FireId" => "Fire",
            _ => type,
        };
    }

    private static string ReplaceVariableName(this string name)
    {
        return name switch
        {
            "base" => "_base",
            "override" => "_override",
            "object" => "_object",
            "event" => "_event",
            "string" => "_string",
            "out" => "_out",
            _ => name,
        };
    }

    private static string ReplaceXMLEscapeLetter(this string comment)
    {
        return comment.Replace("\"", "&quot;").Replace("'", "&apos;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("&", "&amp;");
    }
}