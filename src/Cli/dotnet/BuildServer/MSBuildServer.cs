// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Execution;

namespace Microsoft.DotNet.Cli.BuildServer;

internal class MSBuildServer : IBuildServer
{
    public int ProcessId => 0; // Not yet used

    public string Name => CommonLocalizableStrings.MSBuildServer;

    public void Shutdown()
    {
        BuildManager.DefaultBuildManager.ShutdownAllNodes();
    }
}
