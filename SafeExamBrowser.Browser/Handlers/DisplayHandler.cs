﻿/*
 * Copyright (c) 2019 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.Linq;
using CefSharp;
using CefSharp.Structs;
using SafeExamBrowser.Browser.Events;

namespace SafeExamBrowser.Browser.Handlers
{
	/// <remarks>
	/// See https://cefsharp.github.io/api/71.0.0/html/T_CefSharp_IDisplayHandler.htm.
	/// </remarks>
	internal class DisplayHandler : IDisplayHandler
	{
		public event FaviconChangedEventHandler FaviconChanged;

		public void OnAddressChanged(IWebBrowser chromiumWebBrowser, AddressChangedEventArgs addressChangedArgs)
		{
		}

		public bool OnAutoResize(IWebBrowser chromiumWebBrowser, IBrowser browser, Size newSize)
		{
			return false;
		}

		public bool OnConsoleMessage(IWebBrowser chromiumWebBrowser, ConsoleMessageEventArgs consoleMessageArgs)
		{
			return false;
		}

		public void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> urls)
		{
			if (urls.Any())
			{
				FaviconChanged?.Invoke(urls.First());
			}
		}

		public void OnFullscreenModeChange(IWebBrowser chromiumWebBrowser, IBrowser browser, bool fullscreen)
		{
		}

		public void OnStatusMessage(IWebBrowser chromiumWebBrowser, StatusMessageEventArgs statusMessageArgs)
		{
		}

		public void OnTitleChanged(IWebBrowser chromiumWebBrowser, TitleChangedEventArgs titleChangedArgs)
		{
		}

		public bool OnTooltipChanged(IWebBrowser chromiumWebBrowser, ref string text)
		{
			return false;
		}
	}
}