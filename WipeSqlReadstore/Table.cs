using System.Collections.Generic;
using System.Linq;

namespace WipeSqlReadstore
{
    internal class Table
    {
        private readonly IDictionary<string, Table> _referencingTables = new Dictionary<string, Table>();

        public string Name { get; }

        public IEnumerable<Table> ReferencingTables => _referencingTables.Values;

        public bool IsReferenced => _referencingTables.Count > 0;

        public Table(string name)
        {
            Name = name;
        }

        public void AddReferencingTable(Table table)
        {
            if (!_referencingTables.ContainsKey(table.Name))
            {
                _referencingTables.Add(table.Name, table);
            }
        }

        public bool IsReferencedBy(Table table)
        {
            bool directlyReferenced = _referencingTables.ContainsKey(table.Name);

            if (directlyReferenced)
            {
                return true;
            }

            var indirectlyReferenced =
                _referencingTables.Values
                                 .Any(x => x.IsReferencedBy(table));

            return indirectlyReferenced;
        }
    }
}