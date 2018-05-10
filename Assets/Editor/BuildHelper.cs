﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

public static class BuildHelper
{
	static Lazy<BuildOptions> _currentOptions = new Lazy<BuildOptions>(
		() => System.Environment.GetCommandLineArgs().Contains("-arg-debug") ?
		BuildOptions.Development | BuildOptions.AllowDebugging :
		BuildOptions.None);

	public static BuildOptions CurrentBuildOptions => _currentOptions.Value;

	public static IEnumerable<Tuple<string, BuildTarget>> BuildNames(bool includeiOS)
	{
#if UNITY_2017_3_OR_NEWER
		yield return Tuple.Create("macOS", BuildTarget.StandaloneOSX);
#else
        yield return Tuple.Create("macOS", BuildTarget.StandaloneOSXIntel64);
#endif
		yield return Tuple.Create("win32", BuildTarget.StandaloneWindows);
		yield return Tuple.Create("win64", BuildTarget.StandaloneWindows64);
		yield return Tuple.Create("android", BuildTarget.Android);
		if (includeiOS)
			yield return Tuple.Create("ios", BuildTarget.iOS);
		yield return Tuple.Create("linux-universal", BuildTarget.StandaloneLinuxUniversal);
		yield return Tuple.Create("linux32", BuildTarget.StandaloneLinux);
		yield return Tuple.Create("linux64", BuildTarget.StandaloneLinux64);
		yield return Tuple.Create("webGL", BuildTarget.WebGL);
	}
}
