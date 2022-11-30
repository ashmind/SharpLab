using System;
using System.Linq.Expressions;

public struct S {
    public int a;
    public byte b;
}

public static class Program {
    public static void Main() {
        Inspect.Heap(new S { a = 1, b = 2 });
    }
}

/* output

#{"type":"inspection:memory","title":"S at 0x<IGNORE>","labels":[{"name":"header","offset":0,"length":8},{"name":"type handle","offset":8,"length":8},{"name":"a","offset":16,"length":4},{"name":"b","offset":20,"length":1}],"data":[0,0,0,0,0,0,0,0,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,1,0,0,0,2,0,0,0]}

*/