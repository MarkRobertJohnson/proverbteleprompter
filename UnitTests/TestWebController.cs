using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using NUnit.Framework;
using ProverbTeleprompter.WebController;

namespace UnitTests
{
	[TestFixture]
	public class TestWebController
	{

		private string _shelloutput;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			Hosting.Start();
		}

		[Test]
		public void CanCallMethods()
		{
			var channelFactory = new WebChannelFactory<IPtController>(typeof (IPtController).FullName);

			var client = channelFactory.CreateChannel();
		//	var page = client.LoadPage("Page1");
			client.SendCommand("Scroll", "2");
		}

		[Test]
		public void WillAjaxCallsWork()
		{
			var client = new WebClient();

			var page = client.DownloadString("http://localhost/pt/Page1");
			var cmdResult = client.DownloadString("http://localhost/pt/api/Scroll/2");

		}

	}
}
