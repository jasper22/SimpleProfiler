using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LogWrapper
{
    /// <summary>
    /// <c>Wrapper</c>
    /// </summary>
    /// <seealso cref="LogWrapper.ManagerBase" />
    internal class Wrapper : ManagerBase
    {
        internal const string TEST_METHOD_ATTRIBUTE_FULL_TYPE_NAME = "Xunit.FactAttribute";

        private EtwClassManager etwClassManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wrapper"/> class.
        /// </summary>
        /// <param name="pathToEtwLoggerAssembly">The path to ETW logger assembly.</param>
        /// <param name="pathToTargetFile">The path to target file that logs should be added to it.</param>
        internal Wrapper(string pathToEtwLoggerAssembly, string pathToTargetFile) : base (pathToTargetFile)
        {
            this.etwClassManager = new EtwClassManager(pathToEtwLoggerAssembly);
        }

        /// <summary>
        /// Function will add 'trace' log method calls in each 'test' method
        /// </summary>
        internal void AddLogs()
        {
            // Get all methods that logs should be added to them
            var allMethods = GetAllTestsMethods(this.AssemblyDefinition);

            foreach(var method in allMethods)
            {
                AddLog(method);
            }
        }

        /// <summary>
        /// Gets all methods that is 'test' methods
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>Collection of <see cref="MethodDefinition"/></returns>
        private IEnumerable<MethodDefinition> GetAllTestsMethods(AssemblyDefinition assembly)
        {
            return GetAllMethodsWithAttribute(assembly, TEST_METHOD_ATTRIBUTE_FULL_TYPE_NAME);
        }

        /// <summary>
        /// Gets all methods that has <paramref name="attributeName"/> defined on them
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <returns>Collection of <see cref="MethodDefinition"/></returns>
        private IEnumerable<MethodDefinition> GetAllMethodsWithAttribute(AssemblyDefinition assembly, string attributeName)
        {
            var allMethodsColl = assembly.Modules.SelectMany(module => module.Types, (module, types) => types.Methods);

            var allMethods = allMethodsColl.SelectMany(methodColl => methodColl);

            var testMethods = allMethods.Where(method => method.CustomAttributes.Count > 0)
                                        .Where(method => method.CustomAttributes.Any(attr => attr.AttributeType.FullName.Equals(attributeName, System.StringComparison.OrdinalIgnoreCase)));

            return testMethods;
        }

        /// <summary>
        /// Function will:
        ///     1. Create 'log' method in ETW class
        ///     2. Inject 'Test started' and 'test finished' methods in provided <paramref name="method"/> that call created method in ETW class
        /// </summary>
        /// <param name="method">The method that should be wrapped in logs.</param>
        internal void AddLog(MethodDefinition method)
        {
            // Create 'log' method in ETW class
            var logMethodRef = etwClassManager.CreateEtwTestLogMethod(method.FullName);

            // We need to import this method from ETW assembly
            // otherwise we gonna get errors when we save this assembly
            var logMethod = this.AssemblyDefinition.MainModule.ImportReference(logMethodRef);

            // We don't directly call this method but via static variable
            // so now we need to get reference to this static variable
            var tracerFieldRef = etwClassManager.TracerField;

            // We need to import this field reference from ETW assembly
            var tracerField = this.AssemblyDefinition.MainModule.ImportReference(tracerFieldRef);

            var ilProcessor = method.Body.GetILProcessor();

            // Check if it already contains 'Prolog' section
            // If so - that's mean we already been here -> do nothing
            var firstInstruction = method.Body.Instructions.First();
            if ((firstInstruction.OpCode == OpCodes.Ldsfld) && (firstInstruction.Operand is FieldReference) && (((FieldReference)firstInstruction.Operand).Name.Equals(tracerField.Name)))
            {
                // This is EtwTracer.Tracer.xxxx   code line
                // we already been here -> return
                return;
            }

            // Prolog
            //
            // IL_0000: ldsfld class [ShugiShugi.Common]ShugiShugi.Common.EtwLogger [ShugiShugi.Common]ShugiShugi.Common.EtwLogger::Tracer
            // IL_0005: ldstr "[Test1] Test started"
            // IL_000a: callvirt instance void [ShugiShugi.Common]ShugiShugi.Common.EtwLogger::ShugiShugiTestUnitTest1Test1Log(string)
            //
            var fieldInstruction = Instruction.Create(OpCodes.Ldsfld, tracerField);
            var startLDSTRInstruction = Instruction.Create(OpCodes.Ldstr, $"[{method.Name}] Test started");
            var startCALLInstruction = Instruction.Create(OpCodes.Callvirt, logMethod);

            // Insert it into existing code - before the first instruction
            ilProcessor.InsertBefore(firstInstruction, fieldInstruction);
            ilProcessor.InsertBefore(firstInstruction, startLDSTRInstruction);
            ilProcessor.InsertBefore(firstInstruction, startCALLInstruction);

            // Epilog
            //
            // IL_0023: ldsfld class [ShugiShugi.Common] ShugiShugi.Common.EtwLogger[ShugiShugi.Common] ShugiShugi.Common.EtwLogger::Tracer
            // IL_0028: ldstr "[Test1] Test finished"
            // IL_002d: callvirt instance void[ShugiShugi.Common] ShugiShugi.Common.EtwLogger::ShugiShugiTestUnitTest1Test1Log(string)
            //
            fieldInstruction = Instruction.Create(OpCodes.Ldsfld, tracerField);
            var stopLDSTRInstruction = Instruction.Create(OpCodes.Ldstr, $"[{method.Name}] Test finished");
            var stopCALLInstruction = Instruction.Create(OpCodes.Callvirt, logMethod);

            // Insert it into existing code - before the last instruction
            var lastInstruction = method.Body.Instructions.Last();
            ilProcessor.InsertBefore(lastInstruction, fieldInstruction);
            ilProcessor.InsertBefore(lastInstruction, stopLDSTRInstruction);
            ilProcessor.InsertBefore(lastInstruction, stopCALLInstruction);
        }

        /// <summary>
        /// Function will actually save all altered assemblies to the files
        /// </summary>
        internal void SaveAll()
        {
            this.etwClassManager.SaveAssembly();

            this.SaveAssembly();
        }
    }
}
