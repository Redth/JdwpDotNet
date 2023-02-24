// See https://aka.ms/new-console-template for more information

using JdwpDotNet;

var localJdwpPort = 8100;
var testPkgName = "com.companyname.mauiapp14";

Console.WriteLine("Enter your package name:");

var pkg = Console.ReadLine();

if (string.IsNullOrWhiteSpace(pkg))
	pkg = testPkgName;


var adb = new AndroidSdk.Adb();


var pidofResult = adb.Shell($"pidof {pkg}")?.FirstOrDefault()?.Trim();

if (int.TryParse(pidofResult, out var pid))
{
	Console.WriteLine($"Found PID: {pid} for {pkg}");

	var forwardResult = adb.RunCommand("forward", $"tcp:{localJdwpPort}", $"jdwp:{pid}")?.GetOutput();

	Console.WriteLine($"Forwarded tcp:{localJdwpPort} to jdwp:{pid}");


	Console.WriteLine("Connecting to JDWP...");

	var d = new JdwpClient("127.0.0.1", localJdwpPort);
		
	await d.ConnectAsync();

	Console.WriteLine("Connected, press return to disconnect.");
	Console.ReadLine();


	await d.DisconnectAsync();

	Console.WriteLine("Disconnected from JDWP...");
}
else
{
	Console.WriteLine($"Failed to find PID for package: {pkg}");
}

