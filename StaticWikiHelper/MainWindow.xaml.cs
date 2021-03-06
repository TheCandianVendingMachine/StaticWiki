﻿using System;
using System.Windows;
using System.IO;
using StaticWiki;
using System.ComponentModel;
using Ookii.Dialogs.Wpf;
using System.Linq;

namespace StaticWikiHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemWatcher fileSystemWatcher;
        private string sourceDirectory;
        private string destinationDirectory;
        private string themeFileName;
        private string titleName;
        private string navigationFileName;
        private string[] contentExtensions = new string[0];

        private const string navigationName = "Navigation.list";

        private const string configurationFileName = "staticwiki.ini";
        private const string configurationSectionName = "General";
        private const string configurationSourceDirectoryName = "SourceDir";
        private const string configurationOutputDirectoryName = "OutputDir";
        private const string configurationTitleName = "Title";
        private const string configurationThemeFileName = "ThemeFile";
        private const string configurationContentExtensionsName = "ContentExtensions";

        private bool autoUpdatesEnabled = true;

        public MainWindow()
        {
            InitializeComponent();

            Title = "Static Wiki Helper";

            updateButton.Visibility = Visibility.Hidden;

            Log("Starting Static Wiki");
        }

        private string logFileName
        {
            get
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(path, "Static Wiki");

                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }

                return Path.Combine(appFolder, "StaticWiki.log");
            }
        }

        protected void Log(string message)
        {
            try
            {
                StreamWriter writer = null;

                if(!File.Exists(logFileName))
                {
                    writer = File.CreateText(logFileName);
                }
                else
                {
                    writer = File.AppendText(logFileName);
                }

                if (writer != null)
                {
                    writer.Write(string.Format("{0} - {1}\n", DateTime.Now.ToString(), message));
                }

                writer.Close();
            }
            catch(Exception)
            {
                return;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (fileSystemWatcher != null)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
                fileSystemWatcher = null;
            }

            Log("Closing Static Wiki");

            base.OnClosing(e);
        }

        private void OpenProject(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Open Project Folder";
            dialog.ShowNewFolderButton = false;
            dialog.UseDescriptionForTitle = true;

            if(dialog.ShowDialog(this) == true)
            {
                var basePath = dialog.SelectedPath;

                Log(string.Format("Attempting to open project at '{0}'", basePath));

                noProjectLabel.Visibility = Visibility.Visible;
                projectLoadedLabel.Visibility = Visibility.Hidden;
                updateButton.Visibility = Visibility.Hidden;

                try
                {
                    var configPath = Path.Combine(basePath, configurationFileName);
                    var iniParser = new Ini(configPath);

                    if(iniParser.GetSections().Length == 0)
                    {
                        throw new Exception(string.Format("Unable to open '{0}'", configPath));
                    }

                    sourceDirectory = iniParser.GetValue(configurationSourceDirectoryName, configurationSectionName);
                    destinationDirectory = iniParser.GetValue(configurationOutputDirectoryName, configurationSectionName);
                    titleName = iniParser.GetValue(configurationTitleName, configurationSectionName);
                    themeFileName = iniParser.GetValue(configurationThemeFileName, configurationSectionName);

                    var contentExtensionsString = iniParser.GetValue(configurationContentExtensionsName, configurationSectionName);

                    if(contentExtensionsString.Length > 0)
                    {
                        contentExtensions = contentExtensionsString.Split(",".ToCharArray()).Select(x => x.Trim()).ToArray();
                    }
                }
                catch (Exception exception)
                {
                    Log(string.Format("Unable to load project due to exception: {0}", exception));
                    MessageBox.Show("Unable to load project", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                sourceDirectory = Path.Combine(basePath, sourceDirectory);
                destinationDirectory = Path.Combine(basePath, destinationDirectory);
                themeFileName = Path.Combine(basePath, themeFileName);
                navigationFileName = Path.Combine(basePath, navigationName);

                if(!Directory.Exists(sourceDirectory) || !Directory.Exists(destinationDirectory) || !File.Exists(themeFileName))
                {
                    Log(string.Format("Unable to load project due to invalid paths: Source Directory: {0}; Output Directory: {1}; Theme File: {2}", sourceDirectory, destinationDirectory, themeFileName));
                    MessageBox.Show("Unable to load project due to invalid paths in configuration file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }

                fileSystemWatcher = new FileSystemWatcher();
                fileSystemWatcher.Path = sourceDirectory;
                fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
                fileSystemWatcher.Filter = "*.md";
                fileSystemWatcher.Changed += new FileSystemEventHandler(OnChanged);
                fileSystemWatcher.EnableRaisingEvents = true;

                Log("Successfully loaded project");

                noProjectLabel.Visibility = Visibility.Hidden;

                projectLoadedLabel.Visibility = Visibility.Visible;
                projectLoadedLabel.Content = string.Format("Loaded '{0}'", titleName);

                updateButton.Visibility = Visibility.Visible;

                if(autoUpdatesEnabled)
                {
                    Process();
                }
            }
        }

        private void Process()
        {
            var logMessage = "";

            StaticWikiCore.ProcessSubDirectory(sourceDirectory, destinationDirectory, themeFileName, navigationFileName, contentExtensions, titleName, ref logMessage, 0);

            if (logMessage.Length > 0)
            {
                Log(string.Format("Static Wiki Message: {0}", logMessage));
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (!Directory.Exists(sourceDirectory) || !Directory.Exists(destinationDirectory) || !File.Exists(themeFileName) || !autoUpdatesEnabled)
            {
                return;
            }

            Process();
        }

        private void HandleManualUpdate(object source, RoutedEventArgs e)
        {
            if (!Directory.Exists(sourceDirectory) || !Directory.Exists(destinationDirectory) || !File.Exists(themeFileName))
            {
                return;
            }

            Process();
        }

        private void EnableAutoUpdates(object source, RoutedEventArgs e)
        {
            autoUpdatesEnabled = true;
        }

        private void DisableAutoUpdates(object source, RoutedEventArgs e)
        {
            autoUpdatesEnabled = false;
        }
    }
}
