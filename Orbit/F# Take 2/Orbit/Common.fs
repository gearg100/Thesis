namespace Orbit

type MSet<'T> = System.Collections.Generic.HashSet<'T>

type ProblemDef<'T> = { 
    generators: 'T -> 'T seq 
    initData: 'T seq
}

type OrbitSolver<'T> = 
    abstract member Solve : ProblemDef<'T> -> seq<'T>

