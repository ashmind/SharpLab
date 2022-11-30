using System;

public static class Program {
    public static void Main() {
        Inspect.MemoryGraph((object)null);
    }
}

/* output

#{"type":"inspection:memory-graph","stack":[{"id":1,"offset":0,"size":8,"title":null,"value":"null"}],"heap":[],"references":[]}

*/