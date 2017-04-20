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
            var allTables = GetTableRelationships().GetAll();

            if (args.Length > 0)
            {
                var includeArg =
                    args.Select((x, i) => new { Index = i, Arg = x })
                        .FirstOrDefault(x => x.Arg.StartsWith("-inc"));

                if (includeArg != null)
                {
                    var includeList =
                        args[includeArg.Index + 1]
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    allTables = allTables.Where(x => includeList.Contains(x.Name));
                }

                var excludeArg =
                    args.Select((x, i) => new { Index = i, Arg = x })
                        .FirstOrDefault(x => x.Arg.StartsWith("-exc"));

                if (excludeArg != null)
                {
                    var excludeList =
                        args[excludeArg.Index + 1]
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    allTables = allTables.Where(x => !excludeList.Contains(x.Name));
                }

            }

            WriteDeleteStatements(allTables);
        }

        private static TableSet GetTableRelationships()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;

            IEnumerable<Relationship> relationships;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                relationships = connection.Query<Relationship>(
                                              @"
                                        select 
                                            object_name(child.object_id) as ChildTableName,
                                            object_name(parent.object_id) as ParentTableName,
                                            fk.name as ReferenceName
                                        from sys.tables child
                                        left join sys.foreign_keys fk on child.object_id = fk.referenced_object_id
                                        left join sys.tables parent on fk.parent_object_id = parent.object_id;")
                                          .Where(IsNotSysTable);
            }

            var tables = new TableSet();
            foreach (var relationship in relationships)
            {
                var childTable = tables.Get(relationship.ChildTableName);

                if (!string.IsNullOrEmpty(relationship.ParentTableName))
                {
                    var parentTable = tables.Get(relationship.ParentTableName);
                    childTable.AddReferencingTable(parentTable);
                }
            }

            return tables;
        }

        private static void Recurse(IEnumerable<Table> tables, Action<Table> action)
        {
            var seen = new HashSet<string>();
            Recurse(tables, seen, action);
        }

        private static void Recurse(IEnumerable<Table> tables, ICollection<string> seen, Action<Table> action)
        {
            foreach (var table in tables)
            {
                if (!seen.Contains(table.Name))
                {
                    seen.Add(table.Name);

                    if (table.IsReferenced)
                    {
                        Recurse(table.ReferencingTables, seen, action);
                    }

                    action(table);
                }
            }
        }

        private static void WriteDeleteStatements(IEnumerable<Table> tables)
        {
            Recurse(tables, OutputTableDeleteStatement);
        }

        private static void OutputTableDeleteStatement(Table table)
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("delete from ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[{0}]", table.Name);
            Console.ForegroundColor = defaultColor;
            Console.Write(";");

            if (table.ReferencingTables.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(" -- Referenced by " + string.Join(", ", table.ReferencingTables.Select(x => x.Name)));
                Console.ForegroundColor = defaultColor;
            }

            Console.WriteLine();
        }

        private static bool IsNotSysTable(Relationship x)
        {
            return (string.IsNullOrEmpty(x.ParentTableName) || !Regex.IsMatch(x.ParentTableName, @"^sys"))
                   && !Regex.IsMatch(x.ChildTableName, @"^sys");
        }
    }
}
