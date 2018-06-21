﻿/*
 * Copyright (c) 2018 ETH Zürich, Educational Development and Technology (LET)
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Threading;
using SafeExamBrowser.Contracts.Behaviour.OperationModel;
using SafeExamBrowser.Contracts.Communication.Events;
using SafeExamBrowser.Contracts.Communication.Hosts;
using SafeExamBrowser.Contracts.Communication.Proxies;
using SafeExamBrowser.Contracts.Configuration;
using SafeExamBrowser.Contracts.I18n;
using SafeExamBrowser.Contracts.Logging;
using SafeExamBrowser.Contracts.UserInterface;
using SafeExamBrowser.Contracts.WindowsApi;

namespace SafeExamBrowser.Runtime.Behaviour.Operations
{
	internal class ClientOperation : IOperation
	{
		private readonly int timeout_ms;

		protected IConfigurationRepository configuration;
		protected ILogger logger;
		protected IProcessFactory processFactory;
		protected IProxyFactory proxyFactory;
		protected IRuntimeHost runtimeHost;

		protected static IProcess ClientProcess { get; private set; }
		protected static IClientProxy ClientProxy { get; private set; }
		public IProgressIndicator ProgressIndicator { protected get; set; }

		public ClientOperation(
			IConfigurationRepository configuration,
			ILogger logger,
			IProcessFactory processFactory,
			IProxyFactory proxyFactory,
			IRuntimeHost runtimeHost,
			int timeout_ms)
		{
			this.configuration = configuration;
			this.logger = logger;
			this.processFactory = processFactory;
			this.proxyFactory = proxyFactory;
			this.runtimeHost = runtimeHost;
			this.timeout_ms = timeout_ms;
		}

		public virtual OperationResult Perform()
		{
			ProgressIndicator?.UpdateText(TextKey.ProgressIndicator_StartClient, true);

			var success = TryStartClient();

			if (success)
			{
				logger.Info($"Successfully started new client instance.");
			}
			else
			{
				logger.Error($"Failed to start new client instance! Aborting procedure...");
			}

			return success ? OperationResult.Success : OperationResult.Failed;
		}

		public virtual OperationResult Repeat()
		{
			return Perform();
		}

		public virtual void Revert()
		{
			if (ClientProcess != null && !ClientProcess.HasTerminated)
			{
				ProgressIndicator?.UpdateText(TextKey.ProgressIndicator_StopClient, true);
				TryStopClient();
			}
		}

		protected bool TryStartClient()
		{
			var clientReady = false;
			var clientReadyEvent = new AutoResetEvent(false);
			var clientReadyEventHandler = new CommunicationEventHandler(() => clientReadyEvent.Set());

			var clientExecutable = configuration.RuntimeInfo.ClientExecutablePath;
			var clientLogFile = $"{'"' + configuration.RuntimeInfo.ClientLogFile + '"'}";
			var hostUri = configuration.RuntimeInfo.RuntimeAddress;
			var token = configuration.CurrentSession.StartupToken.ToString("D");

			logger.Info("Starting new client process...");
			runtimeHost.ClientReady += clientReadyEventHandler;
			ClientProcess = processFactory.StartNew(clientExecutable, clientLogFile, hostUri, token);

			logger.Info("Waiting for client to complete initialization...");
			clientReady = clientReadyEvent.WaitOne(timeout_ms);
			runtimeHost.ClientReady -= clientReadyEventHandler;

			if (!clientReady)
			{
				logger.Error($"Failed to start client within {timeout_ms / 1000} seconds!");

				return false;
			}

			logger.Info("Client has been successfully started and initialized. Creating communication proxy for client host...");
			ClientProxy = proxyFactory.CreateClientProxy(configuration.RuntimeInfo.ClientAddress);

			if (!ClientProxy.Connect(configuration.CurrentSession.StartupToken))
			{
				logger.Error("Failed to connect to client!");

				return false;
			}

			logger.Info("Connection with client has been established. Requesting authentication...");

			var response = ClientProxy.RequestAuthentication();

			if (ClientProcess.Id != response?.ProcessId)
			{
				logger.Error("Failed to verify client integrity!");

				return false;
			}

			logger.Info("Authentication of client has been successful, client is ready to operate.");

			configuration.CurrentSession.ClientProcess = ClientProcess;
			configuration.CurrentSession.ClientProxy = ClientProxy;

			return true;
		}

		protected bool TryStopClient()
		{
			var success = false;

			var disconnected = false;
			var disconnectedEvent = new AutoResetEvent(false);
			var disconnectedEventHandler = new CommunicationEventHandler(() => disconnectedEvent.Set());

			var terminated = false;
			var terminatedEvent = new AutoResetEvent(false);
			var terminatedEventHandler = new ProcessTerminatedEventHandler((_) => terminatedEvent.Set());

			runtimeHost.ClientDisconnected += disconnectedEventHandler;
			ClientProcess.Terminated += terminatedEventHandler;

			logger.Info("Instructing client to initiate shutdown procedure.");
			ClientProxy.InitiateShutdown();

			logger.Info("Disconnecting from client communication host.");
			ClientProxy.Disconnect();

			logger.Info("Waiting for client to disconnect from runtime communication host...");
			disconnected = disconnectedEvent.WaitOne(timeout_ms);

			if (!disconnected)
			{
				logger.Error($"Client failed to disconnect within {timeout_ms / 1000} seconds!");
			}

			logger.Info("Waiting for client process to terminate...");
			terminated = terminatedEvent.WaitOne(timeout_ms);

			if (!terminated)
			{
				logger.Error($"Client failed to terminate within {timeout_ms / 1000} seconds!");
			}

			runtimeHost.ClientDisconnected -= disconnectedEventHandler;
			ClientProcess.Terminated -= terminatedEventHandler;

			if (disconnected && terminated)
			{
				logger.Info("Client has been successfully terminated.");
				success = true;
			}
			else
			{
				logger.Warn("Attempting to kill client process since graceful termination failed!");
				success = TryKillClient();
			}

			if (success)
			{
				configuration.CurrentSession.ClientProcess = null;
				configuration.CurrentSession.ClientProxy = null;
			}

			return success;
		}

		protected bool TryKillClient(int attempt = 0)
		{
			const int MAX_ATTEMPTS = 5;

			if (attempt == MAX_ATTEMPTS)
			{
				logger.Error($"Failed to kill client process within {MAX_ATTEMPTS} attempts!");

				return false;
			}

			logger.Info($"Killing client process with ID = {ClientProcess.Id}.");
			ClientProcess.Kill();

			if (ClientProcess.HasTerminated)
			{
				logger.Info("Client process has terminated.");

				return true;
			}
			else
			{
				logger.Warn("Failed to kill client process. Trying again...");

				return TryKillClient(++attempt);
			}
		}
	}
}
