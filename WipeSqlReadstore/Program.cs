using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;

namespace WipeSqlReadstore
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;

            IEnumerable<Result> results;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                results = connection.Query<Result>(@"
select object_name(t.object_id) as TableName, object_name(tb.object_id) as ReferencedTableName
from sys.tables t
left join sys.foreign_keys fk on t.object_id = fk.parent_object_id
left join sys.tables tb on fk.referenced_object_id = tb.object_id;");
            }

            results =
                results.Where(x => !Regex.IsMatch(x.TableName, @"^sys"));

            IDictionary<string, Table> tables = new Dictionary<string, Table>();
            foreach (var result in results)
            {
                Table table;
                if (!tables.TryGetValue(result.TableName, out table))
                {
                    table = new Table(result.TableName);
                    tables.Add(table.Name, table);
                }

                if (!string.IsNullOrEmpty(result.ReferencedTableName))
                {
                    table.AddReferencedTable(result.ReferencedTableName);
                }
            }

            var sorted = tables.Values.ToList();
            sorted.Sort(new ReferencesComparer());

            FormatOutput(sorted);
        }

        private static void FormatOutput(IEnumerable<Table> tables)
        {
            foreach (var table in tables)
            {
                var defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("delete from ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(table.Name);
                Console.ForegroundColor = defaultColor;
                Console.Write(";");

                if (table.ReferencedTables.Any())
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(" -- References " + string.Join(", ", table.ReferencedTables));
                    Console.ForegroundColor = defaultColor;
                }

                Console.WriteLine();
            }
        }
    }
}
