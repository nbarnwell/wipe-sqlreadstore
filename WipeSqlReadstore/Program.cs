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
            var tables = GetTableRelationships();

            WriteDeleteStatements(tables);
        }

        private static void WriteDeleteStatements(TableSet tables)
        {
            var sorted = new List<Table>();
            Recurse(tables.GetAll(), table => sorted.Insert(0, table));
            FormatOutput(sorted);
        }

        private static TableSet GetTableRelationships()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;

            IEnumerable<Result> results;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                results = connection.Query<Result>(
                    @"
select object_name(parent.object_id) as TableName, object_name(child.object_id) as ReferencedTableName
from sys.tables parent
left join sys.foreign_keys fk on parent.object_id = fk.parent_object_id
left join sys.tables child on fk.referenced_object_id = child.object_id;");
            }

            results =
                results.Where(x => !Regex.IsMatch(x.TableName, @"^sys"));

            var tables = new TableSet();
            foreach (var result in results)
            {
                var table = tables.Get(result.TableName);

                if (!string.IsNullOrEmpty(result.ReferencedTableName))
                {
                    var referenceTable = tables.Get(result.ReferencedTableName);
                    table.AddReferencedTable(referenceTable);
                }
            }
            return tables;
        }

        private static void Recurse(IEnumerable<Table> tables, Action<Table> action)
        {
            var seen = new HashSet<string>();
            Recurse(tables, seen, action, depthFirst: true);
        }

        private static void Recurse(IEnumerable<Table> tables, ICollection<string> seen, Action<Table> action, bool depthFirst = true)
        {
            foreach (var table in tables)
            {
                if (!seen.Contains(table.Name))
                {
                    seen.Add(table.Name);

                    if (!depthFirst)
                    {
                        action(table);
                    }

                    if (table.HasReferences)
                    {
                        Recurse(table.ReferencedTables, seen, action);
                    }

                    if (depthFirst)
                    {
                        action(table);
                    }
                }
            }
        }

        private static void FormatOutput(IEnumerable<Table> tables)
        {
            foreach (var table in tables)
            {
                var defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("delete from ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("[{0}]", table.Name);
                Console.ForegroundColor = defaultColor;
                Console.Write(";");

                if (table.ReferencedTables.Any())
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(" -- References " + string.Join(", ", table.ReferencedTables.Select(x => x.Name)));
                    Console.ForegroundColor = defaultColor;
                }

                Console.WriteLine();
            }
        }
    }
}
