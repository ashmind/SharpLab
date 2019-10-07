open System.Linq;
 
let gt_one i = i > 1
let e = Enumerable.Range(0, 5).Where(fun i -> gt_one);
e.ToArray();