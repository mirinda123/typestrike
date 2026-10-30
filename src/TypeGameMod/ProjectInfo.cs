using System.Reflection;
using System.Runtime.CompilerServices;
[assembly:AssemblyTitle("TypeGameWASD")]
[assembly:AssemblyVersion(TypeGameMod.ProjectInfo.AssemblyVersion)]
[assembly:AssemblyFileVersion(TypeGameMod.ProjectInfo.AssemblyVersion)]
[assembly:InternalsVisibleTo("TypeGameMod.Tests")]
namespace TypeGameMod
{
    internal static class ProjectInfo
    {
        internal const string Version = "1.19.2";
        internal const string AssemblyVersion = Version + ".0";
        internal const string ProtocolFamily = "1.18.4";
    }
}
