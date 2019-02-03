export default (getNodeRect, { left, top }) => {
    let nodes;
    const force = () => {
        for (const node of nodes) {
            const rect = getNodeRect(node);

            if (rect.left < left)
                node.x += left - rect.left;

            if (rect.top < top)
                node.y += top - rect.top;
        }
    };
    force.initialize = ns => { nodes = ns.filter(n => !n.data.isDomLayout); };
    return force;
};