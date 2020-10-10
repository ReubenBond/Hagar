using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Hagar.CodeGenerator.MSBuild.Tasks
{
    public class GetDotNetHost : MSBuildTask
    {
        [Output]
        public string DotNetHost { get; set; }

        public override bool Execute()
        {
            DotNetHost = TryFindDotNetExePath();
            return true;
        }

        private static string TryFindDotNetExePath() => DotNetMuxer.MuxerPathOrDefault();
    }
}