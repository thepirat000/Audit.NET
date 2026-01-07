// MvsSln Doc: https://github.com/3F/MvsSln

using System.Text;

using Microsoft.Build.Evaluation;

using net.r_eg.MvsSln;
using net.r_eg.MvsSln.Core;
using net.r_eg.MvsSln.Core.ObjHandlers;
using net.r_eg.MvsSln.Core.SlnHandlers;

using ProjectItem = net.r_eg.MvsSln.Core.ProjectItem;

namespace SolutionGenerator
{
    public static class Program
    {
        // Args:
        //   0: Input sln path
        //   1: Filters (comma separated, e.g.: "entityframework,!core,tests")
        //      To include the matching test projects, add the filter "tests", otherwise they will be excluded.
        //   2: Output sln path
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: SolutionGenerator <input sln path> <filters> [<output sln path>]");
                Console.WriteLine("Example: SolutionGenerator \"C:\\Audit.NET\\Audit.NET.sln\" \"entityframework,!core,tests\" \"C:\\Projects\\MyNewSolution.sln\"");
                return;
            }

            var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            var inputSlnPath = args[0];
            var filters = args[1].Split(",").Where(f => f.Length > 0).ToList();
            
            var outputSlnPath = args.Length > 2 && args[2].Length > 0 ? args[2] : $"{currentDirectory.Name}.sln";
            var outputDir = Path.GetDirectoryName(outputSlnPath)!;

            var flags = args.Length > 3 ? args[3] : string.Empty;

            if (!outputSlnPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                outputSlnPath += ".sln";
            }

            using var sln = new Sln(inputSlnPath, SlnItems.AllNoLoad);

            // Apply filters
            var projects = sln.Result.ProjectItems
                .Where(p => IncludeProject(p, filters))
                .ToList();

            if (projects.Count == 1)
            {
                Console.WriteLine("No projects found");

                return;
            }

            Console.WriteLine();
            for (int i = 0; i < projects.Count; i++)
            {
                Console.WriteLine($"Adding {projects[i].name}");

                // Replace relative path with full path
                projects[i] = projects[i] with { path = projects[i].fullPath };
            }
            Console.WriteLine();

            // New project
            var testProjectName = Path.GetFileNameWithoutExtension(outputSlnPath) + ".csproj";
            var testProject = new ProjectItem(ProjectType.CsSdk, testProjectName, slnDir: outputDir);

            projects.Add(testProject);
            sln.Result.ProjectDependencies.GuidList.Add(testProject.pGuid);
            sln.Result.ProjectDependencies.Projects.Add(testProject.pGuid, testProject);

            var projectConfPlat = sln.Result.ProjectConfigurationPlatforms.Values.First()
                .Where(pc => projects.Exists(prj => prj.pGuid == pc.PGuid))
                .ToList();

            var solutionFolders = sln.Result.SolutionFolders;

            // Write modified sln
            Console.WriteLine($"Writing modified sln to {outputSlnPath}");
            var handlers = new Dictionary<Type, HandlerValue>()
            {
                [typeof(LProject)] = new(new WProject(projects, sln.Result.ProjectDependencies)),
                [typeof(LNestedProjects)] = new(new WNestedProjects(sln.Result.SolutionFolders, projects)),
                [typeof(LProjectConfigurationPlatforms)] = new(new WProjectConfigurationPlatforms(projectConfPlat)),
                [typeof(LSolutionConfigurationPlatforms)] = new(new WSolutionConfigurationPlatforms(projectConfPlat)),
                [typeof(LProjectSolutionItems)] = new(new WProjectSolutionItems(solutionFolders)),
            };

            using (var w = new SlnWriter(outputSlnPath, handlers))
            {
                w.Options = SlnWriterOptions.CreateProjectsIfNotExist;
                w.Write(sln.Result.Map);
            }

            // Create the new project 
            CreateTestProject(projects, testProject, outputDir);
        }

        static bool IncludeProject(ProjectItem project, List<string> filters)
        {
            if (project.name == "Audit.NET")
            {
                return true;
            }

            if (project.name.EndsWith(".Template"))
            {
                return false;
            }

            if (!filters.Exists(f => f.Equals("tests", StringComparison.OrdinalIgnoreCase)))
            {
                if (project.name.EndsWith(".UnitTest"))
                {
                    return false;
                }
            }

            if (filters.Count == 0)
            {
                return true;
            }

            var positiveFilters = filters.FindAll(f => !f.StartsWith("!"));
            var negativeFilters = filters.FindAll(f => f.StartsWith("!")).ConvertAll(f => f.TrimStart('!'));

            var included = positiveFilters.Exists(filter => project.name.Contains(filter, StringComparison.OrdinalIgnoreCase));
            var excluded = negativeFilters.Exists(filter => project.name.Contains(filter, StringComparison.OrdinalIgnoreCase));

            return included && !excluded;
        }

        static void CreateTestProject(List<ProjectItem> projects, ProjectItem testProject, string outputDir)
        {
            var proj = new Project(NewProjectFileOptions.None);
            proj.Xml.Sdk = "Microsoft.NET.Sdk";
            var xp = new XProject(proj);

            xp.SetProperties(new Dictionary<string, string>()
            {
                { "OutputType", "Exe" },
                { "TargetFramework", "net9.0" },
                { "LangVersion", "latest" },
            });
            
            foreach (var project in projects.Where(p => p.pGuid != testProject.pGuid))
            {
                xp.AddProjectReference(project.fullPath, project.pGuid, project.name, false);
            }

            var program = """
                          using Audit.Core;
                          
                          using System;
                          
                          namespace Test2
                          {
                              public static class Program
                              {
                                  static void Main(string[] args)
                                  {
                                      Console.WriteLine("Hello World!");
                                  }
                              }
                          }
                          """;
            File.WriteAllText(Path.Combine(outputDir, "Program.cs"), program);

            // Write the new project
            xp.Save(testProject.fullPath, Encoding.UTF8);
        }
    }
}
