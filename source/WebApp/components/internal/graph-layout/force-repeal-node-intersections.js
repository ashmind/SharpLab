function applyForceIfIntersecting({ firstRect: first, firstNode, secondRect: second, secondNode, a }) {
    const isIntersecting = first.left <= second.right
                        && first.right >= second.left
                        && first.top <= second.bottom
                        && first.bottom >= second.top;

    if (!isIntersecting)
        return;

    const linked = firstNode.data.topLevelLinked.indexOf(secondNode) >= 0;
    if (!linked) {
        // first above second
        if (first.top <= second.top) {
            //firstNode.vy -= aFirst;
            secondNode.vy += a; //aSecond;
        }
        else if (first.top < second.top) { // second above first
            firstNode.vy += a; //aFirst;
            //secondNode.vy -= aSecond;
        }
    }
    else {
        // first before second
        if (first.left <= second.left) {
            //firstNode.vx -= aFirst;
            secondNode.vx += a; //aSecond;
        }
        else if (first.left > second.left) { // second before first
            firstNode.vx += a; //aFirst;
            //secondNode.vx -= aSecond;
        }
    }
}

export default getNodeRect => {
    let nodes;
    let strength = 5;
    const force = alpha => {
        const a = strength * alpha;
        for (let i = 0; i < nodes.length; i++) {
            const firstNode = nodes[i];
            const firstRect = getNodeRect(firstNode);
            for (let j = i + 1; j < nodes.length; j++) {
                const secondNode = nodes[j];
                const secondRect = getNodeRect(secondNode);
                applyForceIfIntersecting({ firstRect, firstNode, secondRect, secondNode, a });
            }
        }
        strength += 5;
    };
    force.initialize = ns => {
        nodes = ns.filter(n => !n.data.isDomLayout);
    };

    return force;
};