using System.Collections.Generic;
using System.Linq;

namespace WipeSqlReadstore
{
    internal class Table
    {
        private readonly IDictionary<string, Table> _referencedTables = new Dictionary<string, Table>();

        public string Name { get; }

        public IEnumerable<Table> ReferencedTables => _referencedTables.Values;

        public bool HasReferences => _referencedTables.Count > 0;

        public Table(string name)
        {
            Name = name;
        }

        public void AddReferencedTable(Table table)
        {
            if (!_referencedTables.ContainsKey(table.Name))
            {
                _referencedTables.Add(table.Name, table);
            }
        }

        public bool References(Table table)
        {
            bool hasDirectReference = _referencedTables.ContainsKey(table.Name);

            if (hasDirectReference)
            {
                return true;
            }

            var hasIndirectReference =
                _referencedTables.Values
                                 .Any(x => x.References(table));

            return hasIndirectReference;
        }
    }
}