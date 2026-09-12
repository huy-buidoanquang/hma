using System.Reflection;
using System.Text.RegularExpressions;
using Hma.Application.Abstractions.Persistence;
using Hma.Application.Abstractions.Reporting;
using Hma.Desktop.Wpf;
using Hma.Domain.Entities;
using Hma.Infrastructure.SqlServer;
using Hma.Reporting;

namespace Hma.Architecture.Tests;

public partial class ArchitectureRulesTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Project_dependencies_follow_the_architecture_direction()
    {
        AssertHmaReferences(typeof(Entity).Assembly);
        AssertHmaReferences(typeof(IHmaDbContext).Assembly, "Hma.Domain");
        AssertHmaReferences(typeof(HmaDbContext).Assembly, "Hma.Application", "Hma.Domain");
        AssertHmaReferences(typeof(DocumentPrinter).Assembly, "Hma.Application", "Hma.Domain");
        AssertHmaReferences(typeof(App).Assembly,
            "Hma.Application", "Hma.Domain", "Hma.Infrastructure.SqlServer", "Hma.Reporting");
    }

    [Fact]
    public void Domain_and_application_do_not_reference_wpf_or_sql_server_adapters()
    {
        var forbidden = new[]
        {
            "PresentationCore", "PresentationFramework", "System.Xaml", "WindowsBase",
            "Microsoft.Data.SqlClient", "Hma.Infrastructure.SqlServer", "Hma.Desktop.Wpf"
        };

        foreach (var assembly in new[] { typeof(Entity).Assembly, typeof(IHmaDbContext).Assembly })
        {
            var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name).ToHashSet();
            Assert.DoesNotContain(forbidden, references.Contains);
        }
    }

    [Fact]
    public void Interfaces_live_under_an_abstractions_namespace()
    {
        foreach (var assembly in ProductionAssemblies())
        {
            var invalid = assembly.GetTypes()
                .Where(type => type.IsInterface && type.Namespace?.Contains(".Abstractions", StringComparison.Ordinal) != true)
                .Select(type => type.FullName)
                .ToArray();
            Assert.Empty(invalid);
        }
    }

    [Fact]
    public void ViewModels_folder_contains_only_view_models_and_their_base_type()
    {
        var directory = Path.Combine(RepositoryRoot, "src", "Hma.Desktop.Wpf", "Presentation");
        var invalid = Directory.GetDirectories(directory, "ViewModels", SearchOption.AllDirectories)
            .SelectMany(viewModelDirectory => Directory.GetFiles(viewModelDirectory, "*.cs"))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not "WorkspaceBase" && !name!.EndsWith("ViewModel", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(invalid);
    }

    [Fact]
    public void View_models_do_not_expose_persisted_domain_entities()
    {
        var persistedTypes = typeof(IHmaDbContext).GetProperties()
            .Select(property => property.PropertyType)
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            .Select(type => type.GetGenericArguments()[0])
            .ToHashSet();
        var violations = typeof(App).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => ContainsPersistedType(property.PropertyType, persistedTypes))
                .Select(property => $"{type.FullName}.{property.Name}: {property.PropertyType}"))
            .OrderBy(value => value)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Reporting_port_does_not_expose_persisted_domain_entities()
    {
        var persistedTypes = typeof(IHmaDbContext).GetProperties()
            .Select(property => property.PropertyType)
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
            .Select(type => type.GetGenericArguments()[0])
            .ToHashSet();
        var violations = typeof(IDocumentRenderer).GetMethods()
            .SelectMany(method => method.GetParameters().Select(parameter => (method, parameter)))
            .Where(item => ContainsPersistedType(item.parameter.ParameterType, persistedTypes))
            .Select(item => $"{item.method.Name}: {item.parameter.ParameterType}")
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void Namespaces_match_source_folders()
    {
        foreach (var project in ProductionProjects())
        {
            var projectDirectory = Path.Combine(RepositoryRoot, "src", project);
            foreach (var file in Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                         .Where(IsProductionSource))
            {
                var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(projectDirectory, file));
                var expected = string.IsNullOrEmpty(relativeDirectory)
                    ? project
                    : project + "." + relativeDirectory.Replace(Path.DirectorySeparatorChar, '.');
                var match = NamespacePattern().Match(File.ReadAllText(file));
                Assert.True(match.Success, $"Missing namespace: {file}");
                Assert.Equal(expected, match.Groups[1].Value);
            }
        }
    }

    [Fact]
    public void Source_files_have_one_top_level_type()
    {
        foreach (var project in ProductionProjects())
        {
            var projectDirectory = Path.Combine(RepositoryRoot, "src", project);
            foreach (var file in Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                         .Where(IsProductionSource))
            {
                var count = TopLevelTypePattern().Matches(File.ReadAllText(file)).Count;
                Assert.True(count <= 1, $"More than one top-level type: {file}");
            }
        }
    }

    [Fact]
    public void Workspace_notifications_render_only_in_the_main_shell()
    {
        var desktop = Path.Combine(RepositoryRoot, "src", "Hma.Desktop.Wpf");
        var presentation = Path.Combine(desktop, "Presentation");
        var workspaceMarkup = Directory.GetFiles(presentation, "*.xaml", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path) is not "MainWindow.xaml" and not "LoginWindow.xaml")
            .Select(path => (path, text: File.ReadAllText(path)))
            .ToArray();

        var violations = workspaceMarkup
            .Where(item => item.text.Contains("StatusBanner", StringComparison.Ordinal)
                           || item.text.Contains("ToastMessage", StringComparison.Ordinal)
                           || item.text.Contains("ToastIsError", StringComparison.Ordinal))
            .Select(item => item.path)
            .ToArray();
        Assert.Empty(violations);

        var shell = File.ReadAllText(Path.Combine(presentation, "Shell", "Views", "MainWindow.xaml"));
        Assert.Contains("{Binding Toast.Message", shell, StringComparison.Ordinal);
        Assert.Contains("{Binding Toast.IsError", shell, StringComparison.Ordinal);
    }

    private static bool IsProductionSource(string path) =>
        !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        && !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
        && Path.GetFileName(path) is not "GlobalUsings.cs" and not "AssemblyInfo.cs";

    private static IEnumerable<Assembly> ProductionAssemblies()
    {
        yield return typeof(Entity).Assembly;
        yield return typeof(IHmaDbContext).Assembly;
        yield return typeof(HmaDbContext).Assembly;
        yield return typeof(DocumentPrinter).Assembly;
        yield return typeof(App).Assembly;
    }

    private static string[] ProductionProjects() =>
    [
        "Hma.Domain", "Hma.Application", "Hma.Infrastructure.SqlServer", "Hma.Reporting", "Hma.Desktop.Wpf"
    ];

    private static void AssertHmaReferences(Assembly assembly, params string[] expected)
    {
        var actual = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name.StartsWith("Hma.", StringComparison.Ordinal))
            .OrderBy(name => name)
            .ToArray();
        Assert.Equal(expected.OrderBy(name => name), actual);
    }

    private static bool ContainsPersistedType(Type type, HashSet<Type> persistedTypes)
    {
        if (persistedTypes.Contains(type)) return true;
        if (type.IsArray) return ContainsPersistedType(type.GetElementType()!, persistedTypes);
        return type.IsGenericType && type.GetGenericArguments().Any(argument => ContainsPersistedType(argument, persistedTypes));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Hma.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Cannot locate repository root.");
    }

    [GeneratedRegex(@"^namespace\s+([A-Za-z0-9_.]+);", RegexOptions.Multiline)]
    private static partial Regex NamespacePattern();

    [GeneratedRegex(@"^(?:public|internal)\s+(?:(?:sealed|abstract|static|partial)\s+)*(?:class|record|interface|enum|struct)\s+", RegexOptions.Multiline)]
    private static partial Regex TopLevelTypePattern();
}
