// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Cli.Commands.Restore;
using Microsoft.DotNet.Cli.Extensions;

namespace Microsoft.DotNet.Cli.Commands.Publish;

internal static class PublishCommandParser
{
    public static readonly string DocsLink = "https://aka.ms/dotnet-publish";

    public static readonly CliArgument<IEnumerable<string>> SlnOrProjectArgument = new(CliStrings.SolutionOrProjectArgumentName)
    {
        Description = CliStrings.SolutionOrProjectArgumentDescription,
        Arity = ArgumentArity.ZeroOrMore
    };

    public static readonly CliOption<string> OutputOption = new ForwardedOption<string>("--output", "-o")
    {
        Description = CliCommandStrings.PublishOutputOptionDescription,
        HelpName = CliCommandStrings.PublishOutputOption
    }.ForwardAsOutputPath("PublishDir");

    public static readonly CliOption<IEnumerable<string>> ManifestOption = new ForwardedOption<IEnumerable<string>>("--manifest")
    {
        Description = CliCommandStrings.ManifestOptionDescription,
        HelpName = CliCommandStrings.ManifestOption
    }.ForwardAsSingle(o => $"-property:TargetManifestFiles={string.Join("%3B", o.Select(CommandDirectoryContext.GetFullPath))}")
    .AllowSingleArgPerToken();

    public static readonly CliOption<bool> NoBuildOption = new ForwardedOption<bool>("--no-build")
    {
        Description = CliCommandStrings.NoBuildOptionDescription,
        Arity = ArgumentArity.Zero
    }.ForwardAs("-property:NoBuild=true");

    public static readonly CliOption<bool> NoLogoOption = new ForwardedOption<bool>("--nologo")
    {
        Description = CliCommandStrings.PublishCmdNoLogo,
        Arity = ArgumentArity.Zero
    }.ForwardAs("-nologo");

    public static readonly CliOption<bool> NoRestoreOption = CommonOptions.NoRestoreOption;

    public static readonly CliOption<bool> SelfContainedOption = CommonOptions.SelfContainedOption;

    public static readonly CliOption<bool> NoSelfContainedOption = CommonOptions.NoSelfContainedOption;

    public static readonly CliOption<string> RuntimeOption = CommonOptions.RuntimeOption;

    public static readonly CliOption<string> FrameworkOption = CommonOptions.FrameworkOption(CliCommandStrings.PublishFrameworkOptionDescription);

    public static readonly CliOption<string> ConfigurationOption = CommonOptions.ConfigurationOption(CliCommandStrings.PublishConfigurationOptionDescription);

    private static readonly CliCommand Command = ConstructCommand();

    public static CliCommand GetCommand()
    {
        return Command;
    }

    private static CliCommand ConstructCommand()
    {
        var command = new DocumentedCommand("publish", DocsLink, CliCommandStrings.PublishAppDescription);

        command.Arguments.Add(SlnOrProjectArgument);
        RestoreCommandParser.AddImplicitRestoreOptions(command, includeRuntimeOption: false, includeNoDependenciesOption: true);

        command.Options.Add(OutputOption);
        command.Options.Add(CommonOptions.ArtifactsPathOption);
        command.Options.Add(ManifestOption);
        command.Options.Add(NoBuildOption);
        command.Options.Add(SelfContainedOption);
        command.Options.Add(NoSelfContainedOption);
        command.Options.Add(NoLogoOption);
        command.Options.Add(FrameworkOption);
        command.Options.Add(RuntimeOption.WithHelpDescription(command, CliCommandStrings.PublishRuntimeOptionDescription));
        command.Options.Add(ConfigurationOption);
        command.Options.Add(CommonOptions.VersionSuffixOption);
        command.Options.Add(CommonOptions.InteractiveMsBuildForwardOption);
        command.Options.Add(NoRestoreOption);
        command.Options.Add(CommonOptions.VerbosityOption);
        command.Options.Add(CommonOptions.ArchitectureOption);
        command.Options.Add(CommonOptions.OperatingSystemOption);
        command.Options.Add(CommonOptions.DisableBuildServersOption);

        command.SetAction(PublishCommand.Run);

        return command;
    }
}
