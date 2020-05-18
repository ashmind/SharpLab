export const languages = Object.freeze({
    csharp: 'C#',
    vb:     'Visual Basic',
    fsharp: 'F#'
} as const);

export type LanguageName = typeof languages[keyof typeof languages];