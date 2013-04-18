namespace Orbit

type ProblemDef<'T> = { 
    generators: 'T -> 'T seq 
    initData: 'T seq
}

type Message<'T> = 
| Start of array<'T>*AsyncReplyChannel<seq<'T>>
| Result of array<'T>

