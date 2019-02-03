function applyForceIfIntersecting({ firstRect: first, firstNode, secondRect: second, secondNode, a }) {
    const isIntersecting = first.left <= second.right
                        && first.right >= second.left
                        && first.top <= second.bottom
                        && first.bottom >= second.top;

    if (!isIntersecting)
        return;

        /*
    // first above second
    if (first.top < second.top) {
        firstNode.vy = Math.min(firstNode.vy, 0);
        secondNode.vy = Math.max(secondNode.vy, 0);
    }
    // first below second
    else if (first.top > second.top) {
        firstNode.vy = Math.max(firstNode.vy, 0);
        secondNode.vy = Math.min(secondNode.vy, 0);
    }

    // first before second
    if (first.left <= second.left) {
        firstNode.vx = Math.min(firstNode.vx, 0);
        secondNode.vx = Math.max(secondNode.vx, 2);
    }
    // first after second
    else if (first.left > second.left) {
        firstNode.vx = Math.max(firstNode.vx, 2);
        secondNode.vx = Math.min(secondNode.vx, 0);
    }
*/

    /*const firstToSecondRatio = (first.width + first.height)
                             / (second.width + second.height);
    const aFirst = (1 / firstToSecondRatio) * a;
    const aSecond = firstToSecondRatio * a;*/

    // first above second
    if (first.top <= second.top) {
        //firstNode.vy -= aFirst;
        //secondNode.vy += a; //aSecond;
    }
    else if (first.top < second.top) { // second above first
        //firstNode.vy += a; //aFirst;
        //secondNode.vy -= aSecond;
    }
    // first before second
    if (first.left <= second.left) {
        console.log(`${firstNode.data.node.title}: ${firstNode.data.node.value}`, `${secondNode.data.node.title}: ${secondNode.data.node.value}`);
        //firstNode.vx -= aFirst;
        secondNode.vx += a; //aSecond;
    }
    else if (first.left > second.left) { // second before first
        console.log(`${firstNode.data.node.title}: ${firstNode.data.node.value}`, `${secondNode.data.node.title}: ${secondNode.data.node.value}`);
        firstNode.vx += a; //aFirst;
        //secondNode.vx -= aSecond;
    }
}

export default getNodeRect => {
    let nodes;
    let strength = 5;
    let iteration = 0;
    const force = alpha => {
        console.log('iteration ' + iteration++);
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