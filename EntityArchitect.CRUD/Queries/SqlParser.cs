using System.Text.RegularExpressions;

namespace EntityArchitect.CRUD.Queries;

public abstract class SqlParser
{
    public class Field
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<Field> Fields { get; set; }
        public bool IsArray { get; set; }
    }

    public static List<Field> ParseSql(string sql)
    {
        var columns = ExtractColumns(sql);
        return ParseFields(columns);
    }

    private static List<string> ExtractColumns(string sql)
    {
        const string pattern = @"[\w\.]+:[\w:]+|\b[\w\.]+:\([\w\.,\s:]+\)\[\]:\w+";
        var matches = Regex.Matches(sql, pattern);

        var columns = new List<string>();
        foreach (Match match in matches)
        {
            columns.Add(match.Value.Trim());
        }

        return columns;
    }
  static List<Field> ParseFields(List<string> columnStrings)
    {
        var fields = new List<Field>();

        foreach (var column in columnStrings)
        {
            if (IsComplexClass(column))
            {
                fields.Add(ParseComplexField(column));
            }
            else
            {
                fields.Add(ParseSimpleField(column));
            }
        }

        return fields;
    }

    static bool IsComplexClass(string column)
    {
        return Regex.IsMatch(column, @":\([\w\.,\s:]+\)");
    }

    static Field ParseComplexField(string column)
    {
        var mainFieldMatch = Regex.Match(column, @"([\w\.]+):\([\w\.,\s:]+\)\[\]:([\w]+)");
        string mainName = mainFieldMatch.Groups[1].Value;
        string mainType = mainFieldMatch.Groups[2].Value;

        var nestedFieldsMatch = Regex.Match(column, @":\(([\w\.,\s:]+)\)");
        string nestedFields = nestedFieldsMatch.Groups[1].Value;

        var fields = new List<Field>();
        var extracted = ExtractColumns(nestedFields);
        foreach (var nestedField in extracted)
        {
            fields.Add(ParseSimpleField(nestedField.Trim()));
        }

        return new Field
        {
            Name = mainName,
            Type = mainType,
            Fields = fields,
        };
    }

    static Field ParseSimpleField(string column)
    {
        var match = Regex.Match(column, @"([\w\.]+)(?: as ([\w]+))?:([\w]+)");
        
        if (!match.Success) throw new ArgumentException($"Invalid column format: {column}");
        
        var fullColumnName = match.Groups[1].Value;
        var alias = match.Groups[2].Success ? match.Groups[2].Value : fullColumnName.Split('.').Last();
        var dataType = match.Groups[3].Value;

        return new Field
        {
            Name = alias,
            Type = dataType,
            Fields = []
        };

    } 
}