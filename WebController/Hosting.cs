using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using ProverbTeleprompter.Properties;

namespace ProverbTeleprompter.WebController
{
	public class Hosting
	{
		private const string FirewallRuleName = "ProverbTeleprompterControllerAccess";

		public static void Start()
		{

			try
			{
				PtController.Initialize();
				if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator) ||
					!IsFirewallRuleEnabled(Settings.Default.ControllerPort, FirewallRuleName))
					ReserveHttpAndSetFirewallRule();
				HostService();
			}
			catch (AddressAccessDeniedException ex)
			{
				ReserveHttpAndSetFirewallRule();
				HostService();

			}
		}


		private static void HostService()
		{
			var host = new WebServiceHost(typeof(PtController), new Uri(string.Format("http://localhost:{0}/pt", Settings.Default.ControllerPort)));
			host.Open();
		}


		private static void ReserveHttpAndSetFirewallRule()
		{
			var reserveDnsName = "+";
			var allowedExecutable = Assembly.GetEntryAssembly().Location;
			var tempBat = Path.Combine(Path.GetTempPath(), "ReserveHttpAndSetFirewallRule.bat");
			if (File.Exists(tempBat))
				File.Delete(tempBat);
			File.AppendAllText(tempBat,
							   string.Format(
								@"
@echo off
:: Using the deprecated port opening was the only one that opened the port correctly and immediatly
netsh firewall delete portopening protocol=TCP port={3} profile=current
netsh firewall add portopening protocol=TCP port={3} name={4} mode=enable scope=all profile=current

:: The allowed program was not working either
:: netsh firewall delete allowedprogram program=""{2}"" profile=current
:: netsh firewall add allowedprogram program=""{2}"" name={4} mode=enable scope=all profile=current

::For some reason, the advfirewall never takes affect, but the legacy one does
:: netsh advfirewall  firewall delete rule name={4}
:: netsh advfirewall  firewall add rule name={4} dir=in action=allow localport={3} program=""Any"" protocol=tcp interfacetype=any
netsh http delete urlacl url=http://{1}:{3}/pt
netsh http add urlacl url=http://{1}:{3}/pt user={0} listen=yes",
								WindowsIdentity.GetCurrent().Name, reserveDnsName, allowedExecutable, Settings.Default.ControllerPort, FirewallRuleName));
			var p = new Process();
			var psi = new ProcessStartInfo("cmd.exe", string.Format(@"/C ""{0}""", tempBat));
			psi.CreateNoWindow = true;
			p.StartInfo = psi;
			p.EnableRaisingEvents = true;
			if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
				psi.Verb = "runas";
			p.Start();
			p.WaitForExit();
			var ec = p.ExitCode;
		}

		private static bool IsFirewallRuleEnabled(int port, string ruleName)
		{
			var tempBat = Path.Combine(Path.GetTempPath(), "IsFirewallRuleEnabled.bat");
			if (File.Exists(tempBat))
				File.Delete(tempBat);
			File.AppendAllText(tempBat,
							   string.Format(
								@"
@echo off
netsh firewall show portopening
"));

			var p = new Process();
			var psi = new ProcessStartInfo("cmd.exe", string.Format(@"/C ""{0}""", tempBat));
			psi.CreateNoWindow = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			p.StartInfo = psi;
			psi.UseShellExecute = false;
			p.EnableRaisingEvents = true;
			p.Start();
			p.WaitForExit();

			var error = p.StandardError.ReadToEnd();
			var output = p.StandardOutput.ReadToEnd();

			var regex = new Regex(string.Format("{0}.*tcp.*enable.*inbound.*{1}", Settings.Default.ControllerPort, FirewallRuleName), 
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			var m = regex.Match(output);
			return m.Success;
		}
	}
}
