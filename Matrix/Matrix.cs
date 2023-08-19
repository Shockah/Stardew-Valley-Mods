using HarmonyLib;
using Shockah.Kokoro;
using StardewModdingAPI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Shockah.Matrix;

public class Matrix : BaseMod, IMatrixApi
{
	public static string MatrixInternalPath => Path.Combine(Constants.GamePath, "matrix-internal");

	public bool IsInTheMatrix => LazyIsInTheMatrix.Value;

	private readonly Lazy<bool> LazyIsInTheMatrix = new(() => Environment.GetCommandLineArgs().Contains("--shockah.matrix"));

	public override void Entry(IModHelper helper)
	{
		base.Entry(helper);

		if (IsInTheMatrix)
		{
			Monitor.Log("Hello from the Matrix", LogLevel.Alert);
			return;
		}

		Monitor.Log("Hello from the real world... or is it?", LogLevel.Alert);
		EnterTheMatrix();
	}

	private void EnterTheMatrix()
	{
		PrepareBaseGameFiles();
		var originalOut = GetOriginalOutStream();

		var child = StartChildProcess(originalOut, Monitor);
		while (!child.HasExited)
			Thread.Sleep(250);
		Environment.Exit(0);
	}

	private void PrepareBaseGameFiles()
	{
		Directory.CreateDirectory(MatrixInternalPath);
		PrepareMatrixCopyOfGameFile("StardewModdingAPI.dll");
		PrepareMatrixCopyOfGameFile("Stardew Valley.dll");
	}

	private void PrepareMatrixCopyOfGameFile(string fileName)
		=> File.Copy(Path.Combine(Constants.GamePath, fileName), Path.Combine(MatrixInternalPath, fileName), overwrite: true);

	private Process StartChildProcess(TextWriter? outStream, IMonitor? monitor)
	{
		var arguments = Environment.GetCommandLineArgs().ToList();
		arguments.Add("--shockah.matrix");

		ProcessStartInfo startInfo = new()
		{
			FileName = Path.Combine(Constants.GamePath, "StardewModdingAPI.exe"),
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		startInfo.ArgumentList.Add(Path.Combine(MatrixInternalPath, "StardewModdingAPI.dll"));
		foreach (var argument in Environment.GetCommandLineArgs().Skip(1))
			startInfo.ArgumentList.Add(argument);
		startInfo.ArgumentList.Add("--shockah.matrix");

		Process process = new()
		{
			StartInfo = startInfo
		};

		if (outStream is not null)
			process.OutputDataReceived += (_, args) =>
			{
				if (args.Data is not null)
					outStream.WriteLine(args.Data);
			};
		if (monitor is not null)
			process.ErrorDataReceived += (_, args) =>
			{
				if (args.Data is not null)
					Monitor.Log($"Child error:\n{args.Data}", LogLevel.Error);
			};

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		return process;
	}

	private TextWriter GetOriginalOutStream()
	{
		var scoreType = AccessTools.TypeByName("StardewModdingAPI.Framework.SCore, StardewModdingAPI")!;
		var logManagerType = AccessTools.TypeByName("StardewModdingAPI.Framework.Logging.LogManager, StardewModdingAPI")!;
		var interceptingTextWriterType = AccessTools.TypeByName("StardewModdingAPI.Framework.Logging.InterceptingTextWriter, StardewModdingAPI")!;
		var scoreGetter = AccessTools.PropertyGetter(scoreType, "Instance")!;
		var logManagerField = AccessTools.Field(scoreType, "LogManager")!;
		var consoleInterceptorField = AccessTools.Field(logManagerType, "ConsoleInterceptor")!;
		var outGetter = AccessTools.PropertyGetter(interceptingTextWriterType, "Out")!;

		var score = scoreGetter.Invoke(null, null);
		var logManager = logManagerField.GetValue(score);
		var consoleInterceptor = consoleInterceptorField.GetValue(logManager);
		var @out = (TextWriter)outGetter.Invoke(consoleInterceptor, null)!;

		return @out;
	}
}