using System;
using System.Linq.Expressions;

public struct S {
    public int a;
    public byte b;
    public SN n;
}

public struct SN {
    public int an;
    public byte bn;
}

public static class Program {
    public static void Main() {
        Inspect.Heap(new S { a = 1, b = 2, n = new SN { an = 3, bn = 4 } });
    }
}

/* output

#{"type":"inspection:memory","title":"S at 0x<IGNORE>","labels":[{"name":"header","offset":0,"length":8},{"name":"type handle","offset":8,"length":8},{"name":"a","offset":16,"length":4},{"name":"b","offset":20,"length":1},{"name":"n","offset":24,"length":5,"nested":[{"name":"an","offset":24,"length":4},{"name":"bn","offset":28,"length":1}]}],"data":[0,0,0,0,0,0,0,0,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,<IGNORE>,1,0,0,0,2,0,0,0,3,0,0,0,4,0,0,0]}

*/