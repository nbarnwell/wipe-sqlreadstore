﻿using System;
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

            var used = new HashSet<string>();
            var sorted = new List<Table>();
            foreach (var table in VisitDepthFirst(tables.GetAll()))
            {
                if (!used.Contains(table.Name))
                {
                    sorted.Insert(0, table);
                    used.Add(table.Name);
                }
            }

            FormatOutput(sorted);
        }

        private static IEnumerable<Table> VisitDepthFirst(IEnumerable<Table> tables)
        {
            foreach (var table in tables)
            {
                if (table.HasReferences)
                {
                    foreach (var referencedTable in VisitDepthFirst(table.ReferencedTables))
                    {
                        yield return referencedTable;
                    }
                }

                yield return table;
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
