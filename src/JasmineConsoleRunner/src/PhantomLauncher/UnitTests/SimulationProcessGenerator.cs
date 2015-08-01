using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Threading;
using Microsoft.CSharp;

namespace GoofyFoot.PhantomLauncher.UnitTests
{
  /// <summary>
  ///   A generator for dynamic creation of a process that can simulate the PhantomJS process
  ///   for testing purposes.
  /// </summary>
  /// 
  internal static class SimulationProcessGenerator
  {
    /// <summary>The name of the executable to generate.</summary>
    private const string executableName = "ProcessManagerSimulator.exe";
     
    /// <summary>The code to use for generation of the simulation process.</summary>
    private const string code = @"
      using System;
      using System.Threading;

      public static class EntryPoint
      {
        public static void Main(string[] args)
        {
          var callCount = 1;
          
          if ((args != null) && (args.Length > 0))
          {
            int.TryParse(args[0], out callCount);
          }

          for (var index = 0; index < callCount; ++index)
          {
            Console.WriteLine(""Simulation"");
            Thread.Sleep(250);
          }
        }
      }
    ";

    /// <summary>
    ///   Generates an executable to serve as the simulation process.
    /// </summary>
    /// 
    /// <param name="path">The path, without filename, where the executable should be generated.</param>
    /// <param name="deleteIfExists">If <c>true</c>, and the file specified by <paramref name="path"/> already exists, it will be deleted; otherwise, no action will be taken.</param>
    /// 
    /// <returns>The path, including filename, to the executable, if it was able to be generated; otherwise, <c>null</c>.</returns>
    /// 
    public static string Generate(string path,
                                  bool   deleteIfExists = false)
    {
      if ((String.IsNullOrEmpty(path)) || (!Directory.Exists(path)))
      {
        throw new ArgumentException("The path provided must be a valid directory.", "path");
      }
    
      path = Path.Combine(path, SimulationProcessGenerator.executableName);
    
      // If the file already exists, then either remove it or take no further action,
      // depending on the value of the deleteIfExists parameter.

      if (File.Exists(path))
      {
        if (deleteIfExists)
        {
          File.Delete(path);
        }
        else
        {
          return path;
        }
      }

      var compilerParameters = new CompilerParameters(new[] { "System.dll" }, path)
      {
        GenerateExecutable = true,
        GenerateInMemory   = false
      };

      using (var provider = new CSharpCodeProvider())
      {
        var results = provider.CompileAssemblyFromSource(compilerParameters, SimulationProcessGenerator.code);
        
        if (results.Errors.Count > 0)
        {
          return null;
        }
      }

      // The compiler will take a short time to release control of the newly generated
      // executable.  Pause to avoid returning before the executable is available.

      Thread.Sleep(100);
      return path;
    }

  } // End class SimulationProcessGenerator
} // End namespace GoofyFoot.PhantomLauncher.UnitTests
