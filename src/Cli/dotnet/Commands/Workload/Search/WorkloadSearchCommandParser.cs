// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Microsoft.DotNet.Cli.Commands.Workload.Search;

internal static class WorkloadSearchCommandParser
{
    public static readonly CliArgument<string> WorkloadIdStubArgument =
        new(CliCommandStrings.WorkloadIdStubArgumentName)
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = CliCommandStrings.WorkloadIdStubArgumentDescription
        };

    public static readonly CliOption<string> VersionOption = InstallingWorkloadCommandParser.VersionOption;

    private static readonly CliCommand Command = ConstructCommand();

    public static CliCommand GetCommand()
    {
        return Command;
    }

    private static CliCommand ConstructCommand()
    {
        var command = new CliCommand("search", CliCommandStrings.WorkloadSearchCommandDescription);
        command.Subcommands.Add(WorkloadSearchVersionsCommandParser.GetCommand());
        command.Arguments.Add(WorkloadIdStubArgument);
        command.Options.Add(CommonOptions.HiddenVerbosityOption);
        command.Options.Add(VersionOption);

        command.SetAction((parseResult) => new WorkloadSearchCommand(parseResult).Execute());

        return command;
    }
}
