﻿// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.
using Femc_Config_Adjuster.Helpers;
using Femc_Config_Adjuster.Services;
using Femc_Config_Adjuster.ViewModels.Pages;
using Femc_Config_Adjuster.ViewModels.Windows;
using Femc_Config_Adjuster.Views.Windows;
using FemcConfig.Library.Config;
using FemcConfig.Library.Config.Sections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Wpf.Ui;

namespace Femc_Config_Adjuster
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
	{
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        public const string APP_VERSION = "3.1.0";
        public const string APP_UPDATE_ENDPOINT = "https://api.github.com/repos/MadMax1960/Concursus/releases";
        private static readonly IHost _host = Host
			.CreateDefaultBuilder()
			.ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!); })
			.ConfigureServices((context, services) =>
			{
				services.AddHostedService<ApplicationHostService>();

				// Page resolver service
				services.AddSingleton<IPageService, PageService>();

				// Theme manipulation
				services.AddSingleton<IThemeService, ThemeService>();

				// TaskBar manipulation
				services.AddSingleton<ITaskBarService, TaskBarService>();

				// Service containing navigation, same as INavigationWindow... but without window
				services.AddSingleton<INavigationService, NavigationService>();

				// Main window with navigation
				services.AddSingleton<INavigationWindow, MainWindow>();
				services.AddSingleton<MainWindowViewModel>();

				// Auto-register pages.
				var types = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass);
				foreach (var type in types)
				{
					if (type.Namespace?.StartsWith("Femc_Config_Adjuster.Views.Pages") == true)
					{
						services.AddSingleton(type);
					}
				}

				services.AddSingleton<SettingsViewModel>();

                // FEMC config library.
                services.AddSingleton<AppService>();

                // Register setting sections.
				services.AddSingleton(s =>
                {
					var app = s.GetRequiredService<AppService>();

                    var sectionType = typeof(ISection);
                    var sectionTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(x => x.GetTypes())
                        .Where(x => sectionType.IsAssignableFrom(x) && x.IsClass)
                        .ToArray();

                    var sections = new List<ISection>();
                    foreach (var section in sectionTypes)
                    {
                        var instance = (ISection)Activator.CreateInstance(section, app)!;
                        sections.Add(instance);
                    }

					return sections.ToArray();
				});
            }).Build();

		/// <summary>
		/// Gets registered service.
		/// </summary>
		/// <typeparam name="T">Type of the service to get.</typeparam>
		/// <returns>Instance of the service or <see langword="null"/>.</returns>
		public static T GetService<T>()
			where T : class
		{
			return _host.Services.GetService(typeof(T)) as T;
		}

        public static string SelectFolder()
        {
            var dialog = new OpenFolderDialog();
            var result = dialog.ShowDialog();

            // Show the dialog
            if (result==true)
                {
                    // Return the selected folder path
                    return dialog.FolderName;
                }
            return null;
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        /// 

        private async void OnStartup(object sender, StartupEventArgs e)
		{
			try
			{
                _host.Start();
			}
			catch(Exception ex)
			{
                HandleException(ex);
            }
		}

		private static void HandleException(Exception ex)
		{
            string path=WriteCrashLog(ex);
            MessageBox.Show("An error occurred: " + ex.Message + " Please create an issue on GitHub if this issue still persists after deleting your config or restarting the app.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            MessageBoxResult result = MessageBoxResult.No;
            if(ex.Message== "Failed to find Reloaded II ENV variable.")
            {
                result = MessageBox.Show("Would you like to select the reloaded mods folder directly?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Code to open the GitHub issue page or perform another action
                    File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FemcConfigApp", "reloadpath.txt"), SelectFolder());
                    MessageBox.Show("The application will now be restarted");
                    RestartApplication();
                }
            }
            else if (ex.Message == "Failed to find FEMC dir.")
            {
                result = MessageBox.Show("Would you like to clear the app cache (This might fix the errors if you recently changed the location of your reloaded directory)?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if(result == MessageBoxResult.Yes)
                {
                    if (File.Exists(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FemcConfigApp", "reloadpath.txt")))
                    {
                        File.Delete(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FemcConfigApp", "reloadpath.txt"));
                    }
                    MessageBox.Show("The application will now be restarted");
                    RestartApplication();
                }
            }
            if (result == MessageBoxResult.No)
            {
                // Start the default web browser with the specified URL
                if (path != "Crash Log Write Failed")
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                // Close the application
                Application.Current.Shutdown();
            }
        }

        private static string WriteCrashLog(Exception ex)
        {
            // Get the path to the Roaming AppData directory
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Define a folder for the crash logs
            string logFolderPath = Path.Combine(appDataPath, "FemcConfigApp", "CrashLogs");

            // Ensure the directory exists
            Directory.CreateDirectory(logFolderPath);

            // Define the log file path with a timestamp
            string logFileName = $"CrashLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string logFilePath = Path.Combine(logFolderPath, logFileName);

            // Write the exception details to the log file
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath))
                {
                    writer.WriteLine("Crash Log - " + DateTime.Now);
                    writer.WriteLine();
                    writer.WriteLine("Exception Message:");
                    writer.WriteLine(ex.Message);
                    writer.WriteLine();
                    writer.WriteLine("Stack Trace:");
                    writer.WriteLine(ex.StackTrace);
                }

                Console.WriteLine($"Crash log written to: {logFilePath}");
				return logFilePath;
            }
            catch (Exception logException)
            {
                // Handle any exceptions that occur while writing the log
                Console.WriteLine("Failed to write crash log: " + logException.Message);
				return "Crash Log Write Failed";
            }
        }
        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        /// 
        public static void RestartApplication()
        {
            // Get the current application process
            Process currentProcess = Process.GetCurrentProcess();

            // Create a new process start info using the current process file name
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = currentProcess.MainModule.FileName, // Path to the executable
                Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1)) // Pass the current command-line arguments
            };

            // Start the new process
            Process.Start(startInfo);

            // Shut down the current application
            Application.Current.Shutdown();
        }

        private async void OnExit(object sender, ExitEventArgs e)
		{
			try
			{
				await _host.StopAsync();

				_host.Dispose();
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}
		}

		/// <summary>
		/// Occurs when an exception is thrown by an application but not handled.
		/// </summary>
		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			// For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
		}
	}
}
