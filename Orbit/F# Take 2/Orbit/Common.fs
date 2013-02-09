namespace Orbit

type MSet<'T> = System.Collections.Generic.HashSet<'T>

type ProblemDef<'T> = { 
    generators: 'T -> 'T seq 
    initData: 'T seq
}

