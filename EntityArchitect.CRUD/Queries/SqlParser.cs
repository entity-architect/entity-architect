using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EntityArchitect.CRUD.Queries;

public abstract class SqlParser
{
    public static List<Field> ParseSql(string sql, Assembly assembly)
    {
        var columns = ExtractColumns(sql);
        return ParseFields(columns, assembly);
    }

    private static List<string> ExtractColumns(string nestedFields)
    {
        var columns = new List<string>();
        var buffer = new StringBuilder();
        var depth = 0;

        foreach (var ch in nestedFields)
            if (ch == ',' && depth == 0)
            {
                columns.Add(buffer.ToString().Trim());
                buffer.Clear();
            }
            else
            {
                if (ch == '(') depth++;
                if (ch == ')') depth--;
                buffer.Append(ch);
            }

        if (buffer.Length > 0)
            columns.Add(buffer.ToString().Trim());

        return columns;
    }

    private static List<Field> ParseFields(List<string> columnStrings, Assembly assembly)
    {
        var fields = new List<Field>();

        foreach (var column in columnStrings)
            if (IsComplexType(column))
                fields.Add(ParseComplexField(column, assembly));
            else
                fields.Add(ParseSimpleField(column, assembly));

        return fields;
    }

    private static bool IsComplexType(string column)
    {
        return Regex.IsMatch(column, @":\(.+\)") || Regex.IsMatch(column, ":^(?i):enumeration:$");
    }

    private static Field ParseComplexField(string column, Assembly assembly)
    {
        var mainFieldMatch = Regex.Match(column, @"([\w\.]+):\((.+)\)(\[\])?:([\w]+)");
        if (!mainFieldMatch.Success)
            throw new ArgumentException($"Column format is invalid: {column}");

        var mainName = mainFieldMatch.Groups[1].Value;
        var nestedFields = mainFieldMatch.Groups[2].Value;
        var mainType = mainFieldMatch.Groups[4].Value;
        var isArray = mainFieldMatch.Groups[3].Success;

        Type? enumerationType = null;
        if (mainType.ToLower() == "enumeration")
        {
            enumerationType = assembly.GetTypes().FirstOrDefault(x => x.Name == mainName);
        }

        var extracted = ExtractColumns(nestedFields);
        var fields = new List<Field>();

        foreach (var nestedField in extracted)
            if (IsComplexType(nestedField))
                fields.Add(ParseComplexField(nestedField.Trim(), assembly));
            else
                fields.Add(ParseSimpleField(nestedField.Trim(), assembly));
        
        return new Field
        {
            Name = mainName,
            Type = mainType,
            Fields = fields,
            IsArray = isArray,
            EnumerationType = enumerationType
        };
    }


    private static Field ParseSimpleField(string column, Assembly assembly)
    {
        var match = Regex.Match(column, @"([\w\.]+):([\w]+)(:([\w]+))?");
        if (!match.Success)
            throw new ArgumentException($"Invalid column format: {column}");
        Type? enumerationType = null;
        if (match.Groups[2].Value.ToLower() == "enumeration")
        {
            enumerationType = assembly.GetTypes().FirstOrDefault(x => x.Name == match.Groups[3].Value.Replace(":", ""));
        }
        
        return new Field
        {
            Name = match.Groups[1].Value,
            Type = match.Groups[2].Value,
            IsKey = match.Groups[4].Value.ToLower() == "key",
            EnumerationType = enumerationType,
            Fields = [],
            IsArray = false
        };
    }

    internal static string RemoveTypes(string text)
    {
        const string pattern = @"(@\w+):\w+(:\w+)?";
        return Regex.Replace(text, pattern, "$1");
    }

    internal static string CleanupSql(string inputSql)
    {
        var step1 = Regex.Replace(inputSql, @":\w+", "", RegexOptions.IgnoreCase);
        var step2 = Regex.Replace(step1, @"\b\w+:\(([^()]*?)\)", "$1", RegexOptions.IgnoreCase);

        while (Regex.IsMatch(step2, @"\b\w+:\(([^()]*?)\)", RegexOptions.IgnoreCase))
            step2 = Regex.Replace(step2, @"\b\w+:\(([^()]*?)\)", "$1", RegexOptions.IgnoreCase);

        step2 = Regex.Replace(step2, @",\s*\)", ")",
            RegexOptions.IgnoreCase);
        step2 = Regex.Replace(step2, @",\s*,", ",", RegexOptions.IgnoreCase);
        step2 = Regex.Replace(step2, @"\bas\s+(\w+)", "as $1", RegexOptions.IgnoreCase);
        var step3 = Regex.Replace(step2, @"\[\]", "", RegexOptions.IgnoreCase);
        step3 = Regex.Replace(step3, @"\s{2,}", " ").Trim();
        step3 = Regex.Replace(step3, @"^,|,$", "", RegexOptions.IgnoreCase);
        
        return step3;
    }
    

    public class Field
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<Field> Fields { get; set; }
        public bool IsArray { get; set; }
        public bool IsKey { get; set; }
        public Type? EnumerationType { get; set; }
    }
}