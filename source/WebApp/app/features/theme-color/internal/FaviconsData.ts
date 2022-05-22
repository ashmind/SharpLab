export type FaviconsData = {
    svgUrl: string;
    sizes: ReadonlyArray<{
        readonly size: number;
        readonly url: string;
    }>;
};