export default () => {
    let nodes;
    const force = () => {
        for (const node of nodes) {
            const { nested: { parent, dx, dy } } = node.data;
            node.fx = parent.x + dx;
            node.fy = parent.y + dy;
        }
    };
    force.initialize = ns => {
        nodes = ns.filter(n => n.data.nested);
    };

    return force;
};