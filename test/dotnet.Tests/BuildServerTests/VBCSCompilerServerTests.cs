// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.BuildServer;
using Microsoft.DotNet.Cli.CommandFactory;
using Microsoft.DotNet.Cli.Utils;
using Moq;
using NuGet.Frameworks;

namespace Microsoft.DotNet.Tests.BuildServerTests
{
    public class VBCSCompilerServerTests
    {
        [Fact]
        public void GivenAZeroExitShutdownDoesNotThrow()
        {
            var server = new VBCSCompilerServer(CreateCommandFactoryMock().Object);
            server.Shutdown();
        }

        [Fact]
        public void GivenANonZeroExitCodeShutdownThrows()
        {
            const string ErrorMessage = "failed!";

            var server = new VBCSCompilerServer(CreateCommandFactoryMock(exitCode: 1, stdErr: ErrorMessage).Object);

            Action a = () => server.Shutdown();

            a.Should().Throw<BuildServerException>().WithMessage(
                string.Format(
                    CliStrings.ShutdownCommandFailed,
                    ErrorMessage));
        }

        private Mock<ICommandFactory> CreateCommandFactoryMock(int exitCode = 0, string stdErr = "")
        {
            var commandMock = new Mock<ICommand>(MockBehavior.Strict);
            commandMock.Setup(c => c.CaptureStdOut()).Returns(commandMock.Object);
            commandMock.Setup(c => c.CaptureStdErr()).Returns(commandMock.Object);
            commandMock.Setup(c => c.Execute()).Returns(new CommandResult(null, exitCode, "", stdErr));

            var commandFactoryMock = new Mock<ICommandFactory>(MockBehavior.Strict);
            commandFactoryMock
                .Setup(
                    f => f.Create(
                        "exec",
                        new string[] { VBCSCompilerServer.VBCSCompilerPath, "-shutdown" },
                        It.IsAny<NuGetFramework>(),
                        Constants.DefaultConfiguration))
                .Returns(commandMock.Object);

            return commandFactoryMock;
        }
    }
}
