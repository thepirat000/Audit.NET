// MvsSln Doc: https://github.com/3F/MvsSln

using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;
using net.r_eg.MvsSln.Core.ObjHandlers;
using net.r_eg.MvsSln.Core.SlnHandlers;

namespace SolutionGenerator
{
    public static class Program
    {
        // Args:
        //   0: Input sln path
        //   1: Filters (comma separated)
        //   1: Output sln path
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: SolutionGenerator <input sln path> <filters> <output sln path>");
                return;
            }

            var inputSlnPath = args[0];
            var filters = args[1].Split(",").Where(f => f.Length > 0).ToList();
            var outputSlnPath = args[2];

            var sln = new Sln(inputSlnPath, SlnItems.AllNoLoad);

            var projects = sln.Result.ProjectItems
                .Where(p => IncludeProject(p, filters))
                .ToList();

            for (int i = 0; i < projects.Count; i++)
            {
                Console.WriteLine($"Adding {projects[i].name}");
                // Replace relative path with full path
                projects[i] = projects[i] with { path = projects[i].fullPath };
            }
            Console.WriteLine();

            // Write modified sln
            Console.WriteLine($"Writing modified sln to {outputSlnPath}");
            var handlers = new Dictionary<Type, HandlerValue>()
            {
                [typeof(LProject)] = new(new WProject(projects, sln.Result.ProjectDependencies)),
            };

            using var w = new SlnWriter(outputSlnPath, handlers);

            w.Write(sln.Result.Map);
        }

        static bool IncludeProject(ProjectItem project, List<string> filters)
        {
            if (project.name == "Audit.NET")
            {
                return true;
            }

            if (project.name.EndsWith(".UnitTest") || project.name.EndsWith(".Template"))
            {
                return false;
            }

            if (filters.Count == 0)
            {
                return true;
            }

            var found = filters.Exists(filter => project.name.Contains(filter, StringComparison.OrdinalIgnoreCase));
           
            return found;
        }
    }
}
