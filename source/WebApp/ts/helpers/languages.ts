export const languages = Object.freeze({
    csharp: 'C#',
    vb:     'Visual Basic',
    fsharp: 'F#',
    il:     'IL'
} as const);

export type LanguageName = typeof languages[keyof typeof languages];