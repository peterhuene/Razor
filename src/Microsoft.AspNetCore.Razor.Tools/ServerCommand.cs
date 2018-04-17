﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class ServerCommand : CommandBase
    {
        public ServerCommand(Application parent)
            : base(parent, "server")
        {
            Pipe = Option("-p|--pipe", "name of named pipe", CommandOptionType.SingleValue);
            KeepAlive = Option("-k|--keep-alive", "sets the default idle timeout for the server in seconds", CommandOptionType.SingleValue);
        }

        public CommandOption Pipe { get; }

        public CommandOption KeepAlive { get; }

        protected override bool ValidateArguments()
        {
            if (string.IsNullOrEmpty(Pipe.Value()))
            {
                Pipe.Values.Add(PipeName.ComputeDefault());
            }

            return true;
        }

        protected override Task<int> ExecuteCoreAsync()
        {
            // Make sure there's only one server with the same identity at a time.
            var serverMutexName = MutexName.GetServerMutexName(Pipe.Value());
            Mutex serverMutex = null;
            var holdsMutex = false;

            try
            {
                serverMutex = new Mutex(initiallyOwned: true, name: serverMutexName, createdNew: out holdsMutex);
            }
            catch (Exception ex)
            {
                // The Mutex constructor can throw in certain cases. One specific example is docker containers
                // where the /tmp directory is restricted. In those cases there is no reliable way to execute
                // the server and we need to fall back to the command line.
                // Example: https://github.com/dotnet/roslyn/issues/24124

                Error.Write($"Server mutex creation failed. {ex.Message}");

                return Task.FromResult(-1);
            }

            if (!holdsMutex)
            {
                // Another server is running, just exit.
                Error.Write("Another server already running...");
                return Task.FromResult(1);
            }

            FileStream pidFileStream = null;
            try
            {
                try
                {
                    // Write the process and pipe information to a file in a well-known location.
                    pidFileStream = WritePidFile();
                }
                catch (IOException ex)
                {
                    // Something happened when trying to write the pid file. Log and move on.
                    ServerLogger.LogException(ex, "Failed to create PID file.");
                }

                TimeSpan? keepAlive = null;
                if (KeepAlive.HasValue() && int.TryParse(KeepAlive.Value(), out var result))
                {
                    // Keep alive times are specified in seconds
                    keepAlive = TimeSpan.FromSeconds(result);
                }

                var host = ConnectionHost.Create(Pipe.Value());

                var compilerHost = CompilerHost.Create();
                ExecuteServerCore(host, compilerHost, Cancelled, eventBus: null, keepAlive: keepAlive);
            }
            finally
            {
                serverMutex.ReleaseMutex();
                serverMutex.Dispose();
                pidFileStream?.Close();
            }

            return Task.FromResult(0);
        }

        protected virtual void ExecuteServerCore(ConnectionHost host, CompilerHost compilerHost, CancellationToken cancellationToken, EventBus eventBus, TimeSpan? keepAlive)
        {
            var dispatcher = RequestDispatcher.Create(host, compilerHost, cancellationToken, eventBus, keepAlive);
            dispatcher.Run();
        }

        internal FileStream WritePidFile()
        {
            // To make all the running rzc servers more discoverable, We want to write the process Id and pipe name to a file.
            // The file contents will be in the following format,
            //
            // <PID>
            // rzc
            // <pipename>

            const int DefaultBufferSize = 4096;
            var processId = Process.GetCurrentProcess().Id;
            var fileName = $"rzc-{processId}";

            var path = Environment.GetEnvironmentVariable("DOTNET_BUILD_PIDFILE_DIRECTORY");
            if (string.IsNullOrEmpty(path))
            {
                var homeEnvVariable = PlatformInformation.IsWindows ? "USERPROFILE" : "HOME";
                path = Path.Combine(Environment.GetEnvironmentVariable(homeEnvVariable), ".dotnet", "pids", "build");
            }

            // Make sure the directory exists.
            Directory.CreateDirectory(path);

            path = Path.Combine(path, fileName);
            var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, DefaultBufferSize, FileOptions.DeleteOnClose);

            using (var writer = new StreamWriter(fileStream, Encoding.UTF8, DefaultBufferSize, leaveOpen: true))
            {
                var content = $"{processId}{Environment.NewLine}rzc{Environment.NewLine}{Pipe.Value()}";
                writer.Write(content);
            }

            return fileStream;
        }
    }
}
