namespace LogWrapper
{
    using Mono.Cecil;

    /// <summary>
    /// <c>ManagerBase</c>
    /// </summary>
    internal abstract class ManagerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagerBase" /> class.
        /// </summary>
        /// <param name="pathToAssembly">The path to assembly.</param>
        internal ManagerBase(string pathToAssembly)
        {
            this.OriginalPathToAssembly = pathToAssembly;

            // We need to make 'backup copy' of the original files because 
            // later on we need to write to them. It is not possible (exception thrown)
            // when we hold a reference to them
            var tmpFile = this.OriginalPathToAssembly.BackupCopy();

            this.AssemblyDefinition = AssemblyDefinition.ReadAssembly(tmpFile);
        }

        /// <summary>
        /// Gets the original path to assembly.
        /// </summary>
        /// <value>
        /// The original path to assembly.
        /// </value>
        internal string OriginalPathToAssembly { get; }

        /// <summary>
        /// Gets the assembly definition.
        /// </summary>
        /// <value>
        /// The assembly definition.
        /// </value>
        internal AssemblyDefinition AssemblyDefinition { get; }

        /// <summary>
        /// Normalizes the name of the provided <paramref name="name"/> function name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        internal virtual string NormalizeName(string name)
        {
            var parts = name.Split(' ');

            var normalized = parts[1];

            normalized = normalized.Replace("()", "");

            normalized = normalized.Replace("::", "");

            normalized = normalized.Replace(".", "");

            return normalized;
        }

        /// <summary>
        /// Normalizes the path to class
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        internal virtual string NormalizePath(string text)
        {
            // System.Void Retalix.CodedUI.CashOffice.PendingTillBusinessPeriodsAndAdjustments::PendingTillBusinessPeriodsAndAdjustments_Test()

            var parts = text.Split(' ');

            var name = parts[1];

            name = name.Substring(0, name.IndexOf("::"));

            return name;
        }

        /// <summary>
        /// Function will actually write altered assembly to file
        /// </summary>
        internal virtual void SaveAssembly()
        {
            this.AssemblyDefinition.Write(this.OriginalPathToAssembly);
        }
    }
}
