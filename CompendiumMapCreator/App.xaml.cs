﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
using CompendiumMapCreator.Format;

namespace CompendiumMapCreator
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			if (!Debugger.IsAttached)
			{
				MessageBox.Show("An unexpected error has occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

				e.Handled = true;
			}
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			if (File.Exists("updater.exe") && this.NeedsUpdate())
			{
				MessageBoxResult result = MessageBox.Show("There is an update available.\n\nDo you want to update?", "DDO Compendium Map Creator", MessageBoxButton.YesNo, MessageBoxImage.Information);

				if (result == MessageBoxResult.Yes)
				{
					Process.Start("updater.exe", "CompendiumMapCreator.exe DCMapCreator");

					this.Shutdown();
				}
			}

			string inputDir = null;
			string outputDir = null;

			for (int i = 0; i < e.Args.Length; i++)
			{
				if ((e.Args[i] == "-e" || e.Args[i] == "-export") && e.Args.Length > (i + 1))
				{
					inputDir = e.Args[i + 1];
				}

				if ((e.Args[i] == "-o" || e.Args[i] == "-output") && e.Args.Length > (i + 1))
				{
					outputDir = e.Args[i + 1];

					if (!outputDir.EndsWith("/") && !outputDir.EndsWith("\\"))
					{
						outputDir += "\\";
					}
				}
			}

			if (string.IsNullOrEmpty(outputDir))
			{
				outputDir = inputDir;
			}

			if (!string.IsNullOrEmpty(inputDir))
			{
				List<string> files = this.GetFiles(inputDir);

				for (int i = 0; i < files.Count; i++)
				{
					try
					{
						Project project = Project.LoadFile(files[i]);

						project.Export(true, outputDir + Path.GetFileNameWithoutExtension(files[i]) + ".png");
					}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
					catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
					{
					}
				}

				this.Shutdown();
			}
			this.StartupUri = new Uri("View/MainWindow.xaml", UriKind.RelativeOrAbsolute);
		}

		private List<string> GetFiles(string path)
		{
			void GetFilesRecursive(string dir, List<string> list)
			{
				string[] dirs = Directory.GetDirectories(dir);

				for (int i = 0; i < dirs.Length; i++)
				{
					GetFilesRecursive(dirs[i], list);
				}

				list.AddRange(Directory.GetFiles(dir));
			}

			List<string> files = new List<string>();

			GetFilesRecursive(path, files);

			return files;
		}

		private bool NeedsUpdate()
		{
			HttpWebRequest request = WebRequest.CreateHttp("https://api.github.com/repos/Xannden/DCMapCreator/releases/latest");

			request.UserAgent = "Xannden";

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Temp));

			Temp temp = (Temp)serializer.ReadObject(response.GetResponseStream());

			return temp.tag_name != "V1.5";
		}

		[DataContract]
		internal class Temp
		{
#pragma warning disable CS0649
			[DataMember]
			internal string tag_name = null;
#pragma warning restore CS0649
		}
	}
}