Inspect.MemoryGraph(new[] { 1, 2, 3 });
Inspect.MemoryGraph(new[] { "a", "b", "c" });

/* output

#{"type":"inspection:memory-graph","stack":[{"id":1,"offset":0,"size":8,"title":null,"value":"Int32[] ref"}],"heap":[{"id":2,"title":"Int32[]","value":"{ 1, 2, 3 }","nestedNodes":[{"id":3,"title":"0","value":"1"},{"id":4,"title":"1","value":"2"},{"id":5,"title":"2","value":"3"}]}],"references":[{"from":1,"to":2}]}
#{"type":"inspection:memory-graph","stack":[{"id":1,"offset":0,"size":8,"title":null,"value":"String[] ref"}],"heap":[{"id":2,"title":"String[]","value":"{ a, b, c }","nestedNodes":[{"id":3,"title":"0","value":"String ref"},{"id":5,"title":"1","value":"String ref"},{"id":7,"title":"2","value":"String ref"}]},{"id":4,"title":"String","value":"a"},{"id":6,"title":"String","value":"b"},{"id":8,"title":"String","value":"c"}],"references":[{"from":3,"to":4},{"from":5,"to":6},{"from":7,"to":8},{"from":1,"to":2}]}

*/