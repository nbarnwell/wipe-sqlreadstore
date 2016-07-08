using System;
using System.Collections.Generic;
using System.Linq;

namespace WipeSqlReadstore
{
    internal class ReferencesComparer : IComparer<Table>
    {
        public int Compare(Table x, Table y)
        {
            /*
             * Tables that directly reference the other earlier in the list,
             * other than that it doesn't really matter, but for the sake
             * of common sense and being deterministic, tables with more
             * references sort earlier in the list, and where there is no
             * reference, and the same number of efferent references, just
             * sort by the table name as a tie-breaker.
             */

            int result = 0;

            if (x.References(y))
            {
                result = -1;
            }

            if (result == 0)
            {
                result = y.ReferencedTables.Count().CompareTo(x.ReferencedTables.Count());
            }

            if (result == 0)
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }

            return result;
        }
    }
}