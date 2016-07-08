using System.Collections.Generic;

namespace WipeSqlReadstore
{
    internal class Table
    {
        private readonly ISet<string> _referencedTables = new HashSet<string>();

        public string Name { get; }

        public IEnumerable<string> ReferencedTables => _referencedTables;

        public Table(string name)
        {
            Name = name;
        }

        public void AddReferencedTable(string tableName)
        {
            _referencedTables.Add(tableName);
        }

        public bool References(Table table)
        {
            return _referencedTables.Contains(table.Name);
        }
    }
}