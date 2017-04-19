using System.Collections;
using System.Collections.Generic;

namespace WipeSqlReadstore
{
    internal class TableSet
    {
        private readonly IDictionary<string, Table> _tables = new Dictionary<string, Table>();

        public Table Get(string name)
        {
            Table table;
            if (!_tables.TryGetValue(name, out table))
            {
                table = new Table(name);
                _tables.Add(name, table);
            }

            return table;
        }

        public IEnumerable<Table> GetAll()
        {
            return _tables.Values;
        }
    }
}