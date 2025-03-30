using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EntityArchitect.CRUD.Queries
{
    public abstract class SqlParser
    {
        public static List<Field> ParseSql(string sql, Assembly assembly)
        {
            var columnsSegment = ExtractColumnsSegment(sql);
            var columns = ExtractColumns(columnsSegment);
            return ParseFields(columns, assembly);
        }


        private static string ExtractColumnsSegment(string sql)
        {
            var selectIndex = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            if (selectIndex == -1)
                throw new ArgumentException("Missing SELECT clause in SQL statement.");

            int depth = 0;
            int fromIndex = -1;
            for (int i = selectIndex; i < sql.Length - 4; i++)
            {
                if (sql[i] == '(') depth++;
                if (sql[i] == ')') depth--;

                if (depth == 0 && sql.Substring(i, 4).Equals("FROM", StringComparison.OrdinalIgnoreCase))
                {
                    fromIndex = i;
                    break;
                }
            }

            if (fromIndex == -1)
                throw new ArgumentException("Could not find matching FROM clause outside of subqueries.");

            return sql.Substring(selectIndex + 6, fromIndex - (selectIndex + 6)).Trim();
        }


        private static List<string> ExtractColumns(string nestedFields)
        {
            var columns = new List<string>();
            var buffer = new StringBuilder();
            var depth = 0;

            foreach (var ch in nestedFields)
            {
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
            }

            if (buffer.Length > 0)
                columns.Add(buffer.ToString().Trim());

            return columns;
        }

        private static List<Field> ParseFields(List<string> columnStrings, Assembly assembly)
        {
            var fields = new List<Field>();

            foreach (var column in columnStrings)
            {
                if(string.IsNullOrEmpty(column))
                    continue;
                if (IsComplexType(column))
                    fields.Add(ParseComplexField(column, assembly));
                else
                    fields.Add(ParseSimpleField(column, assembly));
            }

            return fields;
        }

        private static bool IsComplexType(string column)
        {
            return Regex.IsMatch(column, @"^\w+:\(", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(column, @":\(\(.*\)\)", RegexOptions.Singleline);
        }

        private static Field ParseComplexField(string column, Assembly assembly)
        {
            // Pattern for subqueries like field:((subquery)):alias:type
            var subQueryPattern = @"^(?<name>\w+):\(\((?<subquery>.*?)\)\):(?<alias>\w+)(?::(?<type>\w+))?$";
            var subQueryMatch = Regex.Match(column, subQueryPattern, RegexOptions.Singleline);

            if (subQueryMatch.Success)
            {
                return new Field
                {
                    Name = subQueryMatch.Groups["alias"].Value,
                    SubQuery = subQueryMatch.Groups["subquery"].Value.Trim(),
                    Type = subQueryMatch.Groups["type"].Success ? subQueryMatch.Groups["type"].Value : "string",
                    Fields = new List<Field>(),
                    IsArray = false
                };
            }

            // Pattern for nested complex fields: field:(nested fields)[]:type
            var complexFieldPattern = @"^(?<name>\w+):\((?<fields>.*)\)(?<array>\[\])?:(?<type>\w+)$";
            var complexMatch = Regex.Match(column, complexFieldPattern, RegexOptions.Singleline);

            if (!complexMatch.Success)
                throw new ArgumentException($"Column format is invalid: {column}");

            var mainName = complexMatch.Groups["name"].Value;
            var nestedFields = complexMatch.Groups["fields"].Value;
            var mainType = complexMatch.Groups["type"].Value;
            var isArray = complexMatch.Groups["array"].Success;
            
            var extracted = ExtractColumns(nestedFields);
            var fields = new List<Field>();

            foreach (var nestedField in extracted)
            {
                if (string.IsNullOrEmpty(nestedField))
                    continue;

                fields.Add(IsComplexType(nestedField)
                    ? ParseComplexField(nestedField.Trim(), assembly)
                    : ParseSimpleField(nestedField.Trim(), assembly));
            }

            return new Field
            {
                Name = mainName,
                Type = mainType,
                Fields = fields,
                IsArray = isArray
            };
        }

        private static Field ParseSimpleField(string column, Assembly assembly)
        {
            var pattern = @"^(?<db>[\w\.]+)(?:\s+AS\s+(?<alias>[\w\.]+))?(?::(?<type>\w+)(?::(?<modifier>\w+))?)?$";
            var match = Regex.Match(column, pattern, RegexOptions.IgnoreCase);

            if (!match.Success)
                throw new ArgumentException($"Invalid column format: {column}");

            var dbName = match.Groups["db"].Value;
            var alias = match.Groups["alias"].Success ? match.Groups["alias"].Value : dbName;
            var type = match.Groups["type"].Success ? match.Groups["type"].Value : "string";
            var modifier = match.Groups["modifier"].Value;

            Type? enumerationType = null;
            if (type.Equals("enumeration", StringComparison.OrdinalIgnoreCase))
                enumerationType = assembly.GetTypes().FirstOrDefault(x => x.Name == modifier);

            return new Field
            {
                Name = alias,
                Type = type,
                IsKey = modifier.Equals("Key", StringComparison.OrdinalIgnoreCase),
                EnumerationType = enumerationType,
                Fields = new List<Field>(),
                IsArray = false,
                SubQuery = null
            };
        }

        internal static string RemoveTypes(string text)
        {
            const string pattern = @"(@\w+):\w+(:\w+)?";
            return Regex.Replace(text, pattern, "$1");
        }

        internal static string CleanupSql(string inputSql)
        {
            // Zamień nazwapola:((subquery)):alias na (subquery) AS alias
            var subQueryPattern = @"\w+:\(\((.*?)\)\):(?<alias>\w+)";
            var step0 = Regex.Replace(inputSql, subQueryPattern, "($1) AS ${alias}", RegexOptions.Singleline);

            // Usuń typy np. :GUID, :enumeration, itp.
            var step1 = Regex.Replace(step0, @":\w+(?::\w+)?", "", RegexOptions.IgnoreCase);

            // Usuń nazwy pól z nawiasami np. teams:(...)[]:Alias
            var nestedPattern = @"\w+:\(((?>[^()]+|\((?<depth>)|\)(?<-depth>))*(?(depth)(?!)))\)(\[\])?(?::\w+)?";
            while (Regex.IsMatch(step1, nestedPattern, RegexOptions.Singleline))
                step1 = Regex.Replace(step1, nestedPattern, "$1", RegexOptions.Singleline);

            // Drobne czyszczenie końcowe
            step1 = Regex.Replace(step1, @"\[\]", "", RegexOptions.IgnoreCase);
            step1 = Regex.Replace(step1, @",\s*\)", ")", RegexOptions.IgnoreCase);
            step1 = Regex.Replace(step1, @",\s*,", ",", RegexOptions.IgnoreCase);
            step1 = Regex.Replace(step1, @"\s{2,}", " ").Trim();

            // Usuń przecinek przed FROM
            step1 = Regex.Replace(step1, @",\s*(FROM)", " $1", RegexOptions.IgnoreCase);

            step1 = Regex.Replace(step1, @"^,|,$", "", RegexOptions.IgnoreCase);

            return step1;
        }



        /// <summary>
        /// Klasa reprezentująca pojedyncze pole w zapytaniu
        /// </summary>
        public class Field
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public List<Field> Fields { get; set; }
            public bool IsArray { get; set; }
            public bool IsKey { get; set; }
            public Type? EnumerationType { get; set; }
            public string? SubQuery { get; set; }
        }
    }
}
