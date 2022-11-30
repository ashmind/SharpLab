using System;
using System.Linq.Expressions;

public static class Program {
    public static void Main() {
        Inspect.Heap(5);
    }
}

/* output

#{"type":"inspection:memory","title":"System.Int32 at 0x<IGNORE>","labels":[{"name":"header","offset":0,"length":8},{"name":"type handle","offset":8,"length":8},{"name":"m_value","offset":16,"length":4}],"data":[0,0,0,0,0,0,0,0,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,5,0,0,0,0,0,0,0]}

*/