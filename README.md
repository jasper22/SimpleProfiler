# Simple Profiler

## Idea

The main idea of this project it to show a way to add 'profiling' methods calls into 'measured' methods.

## How it works

Basically we have a main project `ShugiShugi` that hold a reference to `ShugiShugi.Common` project to make ETW calls. Then we have a `ShugiShugi.Test` project that run  usual XUnit tests agains `ShugiShugi` project.

What I want to do - is to dynamically add 'Test started' and 'Test finished' functions calls in each XUnit test method. So it gonna look like this:

```csharp

    [Fact]
    public void TestFunc1()
    {
        Log("Test started");

        // test code here
        // Assert.xxxxx

        Log("Test finished");
    }
```

Note: I'm doing it on tests function because we have to choose functions where to add dynamic code. Methods that have attributes is easy to `Select`, otherwise I have to manually pass the names of the methods....

To make it interesting the `Log` functions themselves will be also dynamically generated in `ShugiShugi.Common` project.

So here are the steps:

1. Select all methods that have `[Fact]` attribute on them
2. On each method:
   * Create new method in `ShugiShugi.Common` project with the name of test method + Log as a method name
   * Inject `IL` code to call this method above in the beginning and end of test function


* This project use [Mono.Cecil](https://github.com/jbevain/cecil) as an engine to insert `IL` code

## Quick start

1. Clone
2. Build
3. Checkout the `ShugiShugi.Test.dll` file with [ilSpy](https://github.com/icsharpcode/ILSpy) or any other assembly code viewer

## Features

* Added `MSBuild` `Target` to `ShugiShugi.Test` project that will automatically run `LogWrapper` on `ShugiShugi.Test` output DLL