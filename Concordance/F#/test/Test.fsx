#load "../src/Helpers.fs"
#load "../src/Transformation.fs"
#load "../src/Concordance.fs" 

open Concordance.SeqTransformation
open Concordance.Execution

run 1 @"test.txt" 3 2 2 (System.Environment.ProcessorCount/2)

System.Console.ReadLine()