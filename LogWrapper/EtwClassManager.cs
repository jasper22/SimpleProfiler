using System;
using System.IO;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LogWrapper
{
    /// <summary>
    /// <c>EtwClassManager</c>
    /// </summary>
    internal class EtwClassManager : ManagerBase
    {
        internal const string ETW_CLASS_FULL_NAME = "ShugiShugi.Common.EtwLogger";

        internal const string ETW_CLASS_REPORTTESTEVENT_METHOD_NAME = "ReportTestEvent";

        internal const string ETW_CLASS_TRACER_FIELD_NAME = "Tracer";

        internal const string EVENT_SOURCE_ASSEMBLY_NAME = "Microsoft.Diagnostics.Tracing.EventSource.dll";

        internal const string NON_EVENT_ATTRIB_FULL_TYPE_NAME = "Microsoft.Diagnostics.Tracing.NonEventAttribute";

        /// <summary>
        /// Initializes a new instance of the <see cref="EtwClassManager"/> class.
        /// </summary>
        /// <param name="pathToAssembly">The path to assembly.</param>
        internal EtwClassManager(string pathToAssembly): base (pathToAssembly)
        {
            this.EtwLoggerClass = this.AssemblyDefinition.MainModule.Types
                                                                    .SingleOrDefault(type => type.FullName.Equals(ETW_CLASS_FULL_NAME))
                                                                    .Resolve();

            this.NonEventAttribute = GetNonEventAttribute();

            this.ReportTestEventMethod = this.EtwLoggerClass.Methods
                                                            .SingleOrDefault(m => m.Name.Equals(ETW_CLASS_REPORTTESTEVENT_METHOD_NAME, System.StringComparison.OrdinalIgnoreCase) &&
                                                                                  m.HasParameters &&
                                                                                  m.Parameters.Count == 1)
                                                            .Resolve();

            this.TracerField = this.EtwLoggerClass.Fields.SingleOrDefault(field => field.Name.Equals(ETW_CLASS_TRACER_FIELD_NAME, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the class <c>EtwLogger</c> as <see cref="TypeDefinition"/>
        /// </summary>
        /// <value>
        /// The etw logger class.
        /// </value>
        private TypeDefinition EtwLoggerClass { get; }

        /// <summary>
        /// Gets the [NonEvent] attribute.
        /// </summary>
        /// <value>
        /// The [NonEvent] attribute.
        /// </value>
        internal CustomAttribute NonEventAttribute { get; }

        /// <summary>
        /// Gets the reference to <see cref="EtwLogger.ReportTestEvent"/> method.
        /// </summary>
        /// <value>
        /// The reference to <see cref="EtwLogger.ReportTestEvent"/> method.
        /// </value>
        /// <remarks>
        ///     public void ReportTestEvent(string message)
        /// </remarks>
        internal MethodReference ReportTestEventMethod { get; }

        /// <summary>
        /// Gets the reference to 'public static EtwLogger Tracer' variable
        /// </summary>
        /// <value>
        /// The reference to 'public static EtwLogger Tracer' variable
        /// </value>
        internal FieldReference TracerField { get; }

        /// <summary>
        /// Creates the ETW test log method.
        /// </summary>
        /// <param name="sourceFunctionName">Name of the source function.</param>
        /// <returns><see cref="MethodReference"/></returns>
        /// <remarks>
        ///     This function will dynamically create a method in ETW class.
        ///     
        ///     For example:
        ///     
        ///     [NonEvent]
        ///     public void TestFunction1Log(string message)
        ///     {
        ///         ReportTestEvent("[ShugiShugi.Test.ShugiShugiTestClass] " + message);
        ///     }
        ///     
        ///  Please note that created method actually call existing method in ETW class: ReportTestEvent()
        /// 
        ///     IL code example:
        ///     ----------------------------------------------------------------------------------
        /// 	IL_0000: ldarg.0
        ///     IL_0001: ldstr "[ShugiShugi.Test.UnitTest1] "
	    ///     IL_0006: ldarg.1
	    ///     IL_0007: call string[mscorlib] System.String::Concat(string, string)        
        ///     IL_000c: call instance void ShugiShugi.Common.EtwLogger::ReportTestEvent(string)
        ///     IL_0011: ret
        ///     ----------------------------------------------------------------------------------
        ///     
        /// </remarks>
        internal MethodReference CreateEtwTestLogMethod(string sourceFunctionName)
        {
            var normalizedName = NormalizeName(sourceFunctionName);

            var functionName = $"{normalizedName}Log";

            var alreadyExist = this.EtwLoggerClass.Methods.SingleOrDefault(m => m.Name.Equals(functionName));
            if (alreadyExist != null)
            {
                return alreadyExist;
            }

            var methodDefition = new MethodDefinition(functionName, MethodAttributes.Public, this.AssemblyDefinition.MainModule.TypeSystem.Void);

            //methodDefition.HasThis = true;
            //methodDefition.ExplicitThis = true;
            //methodDefition.CallingConvention = MethodCallingConvention.Default;

            // Add string parameter
            var messageParameter = new ParameterDefinition("message", ParameterAttributes.None, this.AssemblyDefinition.MainModule.TypeSystem.String);
            methodDefition.Parameters.Add(messageParameter);

            // According to ETW guidance all methods that not emitting event should have [NonEvent] attribute on them 
            // So here I am adding this attribute to method
            methodDefition.CustomAttributes.Add(this.NonEventAttribute);

            var ilProcessor = methodDefition.Body.GetILProcessor();

            // Load .this
            // https://stackoverflow.com/a/1785394/1144952
            var loadThis = Instruction.Create(OpCodes.Ldarg_0);
            ilProcessor.Append(loadThis);

            sourceFunctionName = NormalizePath(sourceFunctionName);

            // Concat '[test.full.name]' and 'message' argument
            var message = $"[{sourceFunctionName}] ";
            var loadString = Instruction.Create(OpCodes.Ldstr, message);
            var loadArgument = Instruction.Create(OpCodes.Ldarg_1);
            var callStringConcat = Instruction.Create(OpCodes.Call, this.AssemblyDefinition.MainModule.ImportReference(typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })));

            ilProcessor.Append(loadString);
            ilProcessor.Append(loadArgument);
            ilProcessor.Append(callStringConcat);

            // Call ReportTestEvent()
            var callTestReport = Instruction.Create(OpCodes.Call, this.ReportTestEventMethod);
            ilProcessor.Append(callTestReport);

            // All done
            var returnFromFunction = Instruction.Create(OpCodes.Ret);
            ilProcessor.Append(returnFromFunction);

            // Add to class
            this.EtwLoggerClass.Methods.Add(methodDefition);

            return methodDefition;
        }

        /// <summary>
        /// Create custom attribute as [NonEvent] attribute
        /// </summary>
        /// <returns><see cref="CustomAttribute"/></returns>
        /// <remarks>https://stackoverflow.com/a/12859571/1144952</remarks>
        internal CustomAttribute GetNonEventAttribute()
        {
            // Actually this NonEventAttribute defined in Microsoft.Diagnostics.Tracing.EventSource.dll file
            // and this DLL must reside in sourceFolder path
            var pathToFile = Path.Combine(Path.GetDirectoryName(this.OriginalPathToAssembly), EVENT_SOURCE_ASSEMBLY_NAME);

            var assembly = AssemblyDefinition.ReadAssembly(pathToFile);

            var type = assembly.MainModule.GetType(NON_EVENT_ATTRIB_FULL_TYPE_NAME);

            var typeCtor = type.Methods.FirstOrDefault(m => m.IsConstructor);

            // Make a reference to it from current (!) assembly definition module
            var targetCtorMethod = this.AssemblyDefinition.MainModule.ImportReference(typeCtor);

            var customAttr = new CustomAttribute(targetCtorMethod);

            return customAttr;
        }
    }
}
