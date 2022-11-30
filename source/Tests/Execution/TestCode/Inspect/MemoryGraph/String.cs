using System;
using System.Linq.Expressions;

public static class Program {
    public static void Main() {
        Inspect.MemoryGraph("a");
    }
}

/* output

#{"type":"inspection:memory-graph","stack":[{"id":1,"offset":0,"size":8,"title":null,"value":"String ref"}],"heap":[{"id":2,"title":"String","value":"a"}],"references":[{"from":1,"to":2}]}

*/