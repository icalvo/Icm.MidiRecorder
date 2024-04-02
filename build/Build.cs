using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Octokit;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
[GitHubActions(
    nameof(PublishNuGet),
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnWorkflowDispatchRequiredInputs = [nameof(Version)],
    InvokedTargets = [nameof(PublishNuGet)],
    ImportSecrets = ["NUGET_TOKEN", "GITHUB_TOKEN"])]
[GitHubActions(
    nameof(PullRequest),
    GitHubActionsImage.UbuntuLatest,
    AutoGenerate = true,
    OnPullRequestBranches = ["main"],
    InvokedTargets = [nameof(PullRequest)],
    EnableGitHubToken = true,
    ImportSecrets = [nameof(NugetToken)])]
[SuppressMessage("ReSharper", "AllUnderscoreLocalParameterName")]
class Build : NukeBuild
{
    const string ChangelogFileName = "CHANGELOG.md";
    const string NoFunctionalityTag = "NO_FUNCTIONALITY";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [CI] readonly GitHubActions GitHubActions;

    //[GitRepository] readonly GitRepository GitRepository;

    [Parameter] [Secret] readonly string NugetToken;

    [Solution(SuppressBuildProjectCheck = true)] readonly Solution Solution;

    [Parameter("Version to be deployed")] public readonly string Version = "0.1.2-test1";

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TargetProjectDirectory => SourceDirectory / "CommandLine";
    AbsolutePath PackageDirectory => TargetProjectDirectory / "packages";

    string MainVersion
    {
        get
        {
            var split = Version?.Split("-", 2);
            return split?[0];
        }
    }

    Target Clean =>
        _ => _
            .Description("✨ Clean")
            .Before(Restore)
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj")
                    .ForEach(x => x.DeleteDirectory());
            });

    Target Restore =>
        _ => _
            .Description("🧱 Restore")
            .DependsOn(Clean)
            .Executes(() =>
            {
                DotNetRestore(s => s
                    .SetProjectFile(Solution));
            });

    Target Compile =>
        _ => _
            .Description("🧱 Build")
            .DependsOn(Restore)
            .Requires(() => Version)
            .Executes(() =>
            {
                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .SetVersion(Version)
                    .SetAssemblyVersion(MainVersion)
                    .SetFileVersion(MainVersion)
                    .SetInformationalVersion(Version)
                    .EnableNoRestore());
            });

    Target UnitTests =>
        _ => _
            .Description("🐛 Unit Tests")
            .DependsOn(Compile)
            .Executes(() =>
            {
                DotNetTest(o => o
                    .SetProjectFile(SourceDirectory / "UnitTests")
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .SetLoggers("console;verbosity=normal"));
            });

    Target Pack =>
        _ => _
            .Description("📦 NuGet Pack")
            .DependsOn(Compile)
            .Requires(() => Version)
            .Produces(PackageDirectory / "*.nupkg")
            .Executes(() =>
            {
                PackageDirectory.CreateOrCleanDirectory();
                DotNetPack(s => s
                    .SetProject(TargetProjectDirectory)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .SetProperty("PackageVersion", Version)
                    .SetOutputDirectory(PackageDirectory));
            });

    Target ChangelogVerification =>
        _ => _
            .Description("👀 Changelog Verification")
            .Requires(() => Version)
            .Executes(() =>
            {
                Assert.FileExists(RootDirectory / ChangelogFileName);
                Assert.True(
                    File.ReadLines(RootDirectory / ChangelogFileName)
                        .Any(line => line.StartsWith($"## [{MainVersion}")),
                    $"There is no entry for version {Version} in {ChangelogFileName}");
            });

    Target Push =>
        _ => _
            .Description("📢 NuGet Push")
            .DependsOn(Pack, UnitTests, ChangelogVerification)
            .Consumes(Pack)
            .Triggers(TagCommit)
            .Executes(() =>
            {
                DotNetNuGetPush(s => s
                    .SetTargetPath(PackageDirectory / "*.nupkg")
                    .SetApiKey(NugetToken)
                    .SetSource("https://api.nuget.org/v3/index.json"));
            });

    Target TagCommit =>
        _ => _
            .Description("🏷 Tag commit and push")
            .Requires(() => GitHubActions)
            .Requires(() => Version)
            .Executes(async () =>
            {
                var tokenAuth = new Credentials(GitHubActions.Token);
                var github = new GitHubClient(new ProductHeaderValue("build-script")) { Credentials = tokenAuth };
                var split = GitHubActions.Repository.Split("/");
                var owner = split[0];
                var name = split[1];
                GitTag tag = await github.Git.Tag.Create(
                    owner,
                    name,
                    new NewTag
                    {
                        Tag = $"v{Version}",
                        Object = GitHubActions.Sha,
                        Type = TaggedType.Commit,
                        Tagger = new Committer(GitHubActions.Actor, $"{GitHubActions.Actor}@users.noreply.github.com",
                            DateTimeOffset.UtcNow),
                        Message = "Package published in NuGet.org"
                    });

                await github.Git.Reference.Create(
                    owner,
                    name,
                    new NewReference($"refs/tags/v{Version}", tag.Object.Sha));

                Assert.FileExists(RootDirectory / ChangelogFileName);
                var newRelease = new NewRelease(Version)
                {
                    Draft = false,
                    Prerelease = Version != MainVersion,
                    Name = Version,
                    Body = File.ReadAllText(RootDirectory / ChangelogFileName)
                };
                await github.Repository.Release.Create(owner, name, newRelease);
            });

    Target PullRequest =>
        _ => _
            .Description("🏷 Pull Request")
            .Requires(() => GitHubActions)
            .Triggers(UnitTests)
            .Executes(async () =>
            {
                var tokenAuth = new Credentials(GitHubActions.Token);
                var github = new GitHubClient(new ProductHeaderValue("build-script")) { Credentials = tokenAuth };
                var split = GitHubActions.Repository.Split("/");
                var owner = split[0];
                var name = split[1];
                if (GitHubActions.PullRequestNumber != null)
                {
                    PullRequest pullRequest =
                        await github.PullRequest.Get(owner, name, GitHubActions.PullRequestNumber.Value);
                    if (pullRequest.Body.Contains(NoFunctionalityTag))
                    {
                        return;
                    }

                    var pullRequestFiles =
                        await github.PullRequest.Files(owner, name, GitHubActions.PullRequestNumber.Value);
                    try
                    {
                        Assert.True(
                            pullRequestFiles.Any(x => x.FileName == ChangelogFileName),
                            $"You didn't update {ChangelogFileName}. Update it or write '{NoFunctionalityTag}' somewhere in the PR description.");
                    }
                    catch
                    {
                        foreach (var fileName in pullRequestFiles.Select(x => x.FileName))
                        {
                            Log.Information("PR File: {FileName}", fileName);
                        }

                        throw;
                    }
                }
            });

    Target PublishNuGet =>
        _ => _
            .Description("Publish NuGet")
            .Triggers(Push);

    public static int Main() => Execute<Build>(x => x.Compile);
}
