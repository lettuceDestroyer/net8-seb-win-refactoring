﻿/*
 * Copyright (c) 2017 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SafeExamBrowser.Contracts.Logging;
using SafeExamBrowser.Core.Logging;

namespace SafeExamBrowser.Core.UnitTests.Logging
{
	[TestClass]
	public class ModuleLoggerTests
	{
		[TestMethod]
		public void MustCorrectlyForwardCalls()
		{
			var exception = new Exception();
			var loggerMock = new Mock<ILogger>();
			var logObserverMock = new Mock<ILogObserver>();
			var logText = new LogText("Log text");
			var sut = new ModuleLogger(loggerMock.Object, typeof(ModuleLoggerTests));

			sut.Info("Info");
			sut.Warn("Warning");
			sut.Error("Error");
			sut.Error("Error", exception);
			sut.Log("Raw text");
			sut.Log(logText);
			sut.Subscribe(logObserverMock.Object);
			sut.Unsubscribe(logObserverMock.Object);
			sut.GetLog();

			loggerMock.Verify(l => l.Info($"[{nameof(ModuleLoggerTests)}] Info"), Times.Once);
			loggerMock.Verify(l => l.Warn($"[{nameof(ModuleLoggerTests)}] Warning"), Times.Once);
			loggerMock.Verify(l => l.Error($"[{nameof(ModuleLoggerTests)}] Error"), Times.Once);
			loggerMock.Verify(l => l.Error($"[{nameof(ModuleLoggerTests)}] Error", exception), Times.Once);
			loggerMock.Verify(l => l.Log("Raw text"), Times.Once);
			loggerMock.Verify(l => l.Log(logText), Times.Once);
			loggerMock.Verify(l => l.Subscribe(logObserverMock.Object), Times.Once);
			loggerMock.Verify(l => l.Unsubscribe(logObserverMock.Object), Times.Once);
			loggerMock.Verify(l => l.GetLog(), Times.Once);
		}
	}
}