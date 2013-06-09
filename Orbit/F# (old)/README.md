Orbit in F#
=================

In order to compile and run the F# version one has to install the .Net 4.5 or Mono 2.10.8 runtime 
and the F# 3.0 compiler and libraries. Assuming that a compatible runtime is installed, F# can be
compiled from its [sources](https://github.com/fsharp/fsharp).

Contents
 - **Solution** contains a VS2012 Solution for building this implementation and the corresponding F# projects
 - **src** contains the source files of the implementation
 - **test** contains a VS2012 unit test source file and an .fsx script
 - **target** will contain the compiled files

To compile this version of Orbit, there are 2 options:
 - open the provided solution and build the Orbit project.
 - use the **Makefile**. 
The executable will be created in **/target**.

To test the implementation there are also 2 options:
 - use provided OrbitUnitTests project in the solution. After building the project, the tests will be run automatically.
 - use the provided **Test.fsx** script, found in "/test" with in the F# Interactive.

