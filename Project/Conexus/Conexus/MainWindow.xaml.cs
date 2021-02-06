﻿/*
> Conexus (v1.3.0) for Darkest Dungeon
    Created by MatthiosArcanus(Discord)/Hypocrita(Steam)/Hypocrita20XX(GitHub) 
    A GUI-based program designed to streamline the process of organizing mods according to an existing Steam collection or list of links
    Handles downloading and updating mods through the use of SteamCMD (https://developer.valvesoftware.com/wiki/SteamCMD)

> APIs used:
    Ookii.Dialogs (v3.1.0)
    Authors: Sven Groot, Augusto Proiete
    Source: http://www.ookii.org/software/dialogs/

    Extended WPF Toolkit (v4.0.2
    Author: Xceed Software
    Source: https://github.com/xceedsoftware/wpftoolkit

    Peanut Butter INI (v2.0.2)
    Author: Davys McColl
    Source: https://github.com/fluffynuts/PeanutButter

> Code used/adapated:
    Function: Copy Folders
    Author: Timm
    Source: http://www.csharp411.com/c-copy-folder-recursively/

    Function: Password Reveal Functionality
    Author: DaisyTian-MSFT
    Source: https://docs.microsoft.com/en-us/answers/questions/99602/wpf-passwordbox-passwordrevealmode-was-not-found-i.html

> License: MIT
    Copyright (c) 2019, 2020, 2021 MatthiosArcanus/Hypocrita_2013/Hypocrita20XX

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

#region Using Statements

using Ookii.Dialogs.Wpf;
using PeanutButter.INI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;

#endregion

namespace Conexus
{
    public partial class MainWindow : Window
    {
        #region Declarations
        //Lists to store info related to the mods that will/are downloaded
        List<string> modInfo = new List<string>();
        List<string> appIDs = new List<string>();
        //Bools to store the value of each combobox
        bool downloadMods;
        bool updateMods;

        //Bool to store which method the user has selected
        bool steam;

        //Added v1.3.0
        //Create a global dateTime for this session
        string dateTime = Regex.Replace(DateTime.Now.ToString(), @"['<''>'':''/''\''|''?''*'' ']", "_", RegexOptions.None);

        //Added v1.2.0
        //Changed v1.3.0, so that it's not zero-based (better for most to understand)
        //Keeps track of the line count in the log
        int lineCount = 1;
        //Stores all logs in a list, for later storage in a text file
        List<string> log = new List<string>();

        //Added v1.3.0
        //Stores logs temporarily until the message textblock is initiated
        List<string> logTmp = new List<string>();

        //Added v1.3.0
        //Create a root directory in the user's Documents folder for all generated data
        string rootPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Conexus";
        //Create a data directory for all text files (HTML.txt, Mods.txt, ModInfo.txt)
        string dataPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Conexus\\Data";
        //Create a config directory for all user data
        string configPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Conexus\\Config";
        //Create a directory that will hold Links.txt
        string linksPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Conexus\\Links";
        //Create a directory that will hold logs
        string logsPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Conexus\\Logs";

        /*
         * 
         * New config file as of v1.3.0
         * config.ini, stored in Documents\Conexus\Config
         * 
         * [System]
         * Root=\Documents\Conexus
         * Data=\Documents\Conexus\Data
         * Config=\Documents\Conexus\Config
         * Links=\Documents\Conexus\Links
         * Logs="\Documents\Conexus\Logs"
         * 
         * [Directories]
         * Mods=\DarkestDungeon\mods
         * SteamCmd=\steamcmd
         * 
         * [URL]
         * Collection=https://steamcommunity.com
         * 
         * [Misc]
         * Mode=download
         * Method=steam
         * 
         * [Login]
         * Username=""
         * Password=""
         * 
         */
        INIFile ini;

        string root = "";
        string data = "";
        string config = "";
        string links = "";
        string logs = "";

        string mods = "";
        string steamcmd = "";

        string urlcollection = "";

        string mode = "";
        string method = "";

        //If the user provides this info, we also need to read the Steam username and password
        string username = "";
        string password = "";

        //Added v1.3.0
        //Ensures that certain messages don't happen until the textblock is initialized
        bool loaded = false;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            //Added v1.2.0? (Missing from source, not sure if in final build for v1.20 and v1.2.1)
            //Very basic, unstead exception handling
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            ini = new INIFile(configPath + "\\config.ini");

            this.DataContext = this;
        }

        //Added v1.2.0? (Missing from source, not sure if in final build for v1.20 and v1.2.1)
        //Very basic, untested exception handling
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowMessage("WARNING: exception occured! " + (e.ExceptionObject as Exception).Message);
            ShowMessage("WARNING: Please post your logs on Github! https://github.com/Hypocrita20XX/Conexus/issues");

            //If an exception does happen, I'm assuming Conexus will crash, so here's this just in case
            //Ensure the Logs folder exists
            if (!Directory.Exists(logsPath))
                Directory.CreateDirectory(logsPath);

            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        #region ComboBox Functionality

        //Changed v1.3.0, added logging
        //Handles mode selection
        void Mode_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //Get the index of the selected item
            //0 = download
            //1 = update
            int i = cmbMode.SelectedIndex;

            //If the user wishes to download mods
            if (i == 0)
            {
                //Change local variables accordingly
                downloadMods = true;
                updateMods = false;

                //Added v1.3.0
                //Log info relating to what the user wants to do
                ShowMessage("INPUT: Mode switched to \"Download Mods\"");
            }

            //If the user wishes to update their existing mods
            if (i == 1)
            {
                //Change local variables accordingly
                downloadMods = false;
                updateMods = true;

                //Added v1.3.0
                //Log info relating to what the user wants to do
                ShowMessage("INPUT: Mode switched to \"Update Mods\"");
            }

            if (loaded)
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.3.0, added logging
        //Handles method (ex platform) selection
        void Platform_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //Get the index of the selected item
            //0 = steam
            //1 = list
            int i = cmbPlatform.SelectedIndex;

            //If the user is using Steam
            if (i == 0)
            {
                //Change local variables accordingly
                steam = true;

                //Added v1.3.0
                //Log info relating to what the user wants to do
                ShowMessage("INPUT: Method switched to \"Steam Collection\"");
            }

            //If the user is using a list
            if (i == 1)
            {
                //Change local variables accordingly
                steam = false;

                //Added v1.3.0
                //Log info relating to what the user wants to do
                ShowMessage("INPUT: Method switched to \"List\"");
            }

            if (loaded)
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        #endregion

        #region Button Functionality

        //Changed v1.3.0, added logging
        //Provides functionality to allow the user to select the mods directory
        void ModDir_Click(object sender, RoutedEventArgs e)
        {
            //Create a new folder browser to allow easy navigation to the user's desired directory
            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
            //Show the folder browser
            folderBrowser.ShowDialog();
            //Set a correct description for the browser (seems to be non-functional, low priority to fix)
            folderBrowser.Description = "Mods Directory";
            //Ensure this description is used (seems to be non-functional, low priority to fix)
            folderBrowser.UseDescriptionForTitle = true;
            //Set the content of the button to what the user has selected
            mods = folderBrowser.SelectedPath;

            //Added v1.3.0
            //Log info relating to what the user wants to do
            ShowMessage("INPUT: Mods directory set to \"" + mods + "\"");

            //Added v1.2.0
            //Verify that the provided directory is valid, not empty, and contains Darkest.exe
            if (VerifyModDir(folderBrowser.SelectedPath))
            {
                //Set the settings variable to the one selected
                mods = folderBrowser.SelectedPath;

                //Added v1.3.0
                ini["Directories"]["Mods"] = mods;
                ini.Persist();

                //Added v1.3.0
                //Log info relating to what the user wants to do
                ShowMessage("VERIFY: Mods directory is valid and has been saved to config file");

                ModDir.Content = mods;
            }
            //If the given path is invalid, let the user know why
            else
            {
                //If the given path contains any info, provide that with the error message
                if (folderBrowser.SelectedPath.Length > 0)
                    ShowMessage("WARN: Invalid mods location: " + folderBrowser.SelectedPath + "!");
                //If the given path is blank, provide that information
                else
                    ShowMessage("WARN: Invalid mods Location: no path given!");
            }

            if (loaded)
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.3.0, added logging
        //Provides functionality to allow the user to select the SteamCMD directory
        void SteamCMDDir_Click(object sender, RoutedEventArgs e)
        {
            //Create a new folder browser to allow easy navigation to the user's desired directory
            VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog();
            //Show the folder browser
            folderBrowser.ShowDialog();
            //Set a correct description for the browser (seems to be non-functional, low priority to fix)
            folderBrowser.Description = "SteamCMD Directory";
            //Ensure this description is used (seems to be non-functional, low priority to fix)
            folderBrowser.UseDescriptionForTitle = true;
            //Set the content of the button to what the user has selected
            steamcmd = folderBrowser.SelectedPath;

            //Added v1.3.0
            //Log info relating to what the user wants to do
            ShowMessage("INPUT: SteamCMD directory set to \"" + steamcmd + "\"");

            //Added v1.2.0
            //Verify that the provided directory is valid, not empty, and contains steamcmd.exe
            if (VerifySteamCMDDir(folderBrowser.SelectedPath))
            {
                string tmp = Path.GetFullPath(folderBrowser.SelectedPath);

                //Changed v1.3.0
                //Set the settings variable to the one selected
                steamcmd = folderBrowser.SelectedPath;

                //Added v1.3.0
                ini["Directories"]["SteamCMD"] = steamcmd;
                ini.Persist();

                //Added v1.3.0
                //Log info relating to what the user wants to do
                ShowMessage("VERIFY: SteamCMD directory is valid and has been saved to config file");

                //Addvd v1.3.0
                SteamCMDDir.Content = steamcmd;
            }
            //If the given path is invalid, let the user know why
            else
            {
                //If the given path contains any info, provide that with the error message
                if (folderBrowser.SelectedPath.Length > 0)
                    ShowMessage("WARN: Invalid SteamCMD location: " + folderBrowser.SelectedPath + "!");
                //If the given path is blank, provide that information
                else
                    ShowMessage("WARN: Invalid SteamCMD location: no path given!");
            }

            if (loaded)
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Added v1.2.1
        //Changed v1.3.0, added exception handling
        //Opens a link to Conexus on Nexus Mods
        void URL_Nexus_Click(object sender, RoutedEventArgs e)
        {
            //Attempt to open a link in the user's default browser
            try
            {
                //Let user know what's happening
                ShowMessage("INFO: Attempting to open link to Conexus on Nexus Mods");
                //Attempt to open link
                System.Diagnostics.Process.Start("https://www.nexusmods.com/darkestdungeon/mods/858?");
                //Let user know what happened
                ShowMessage("INFO: Link successfully opened");
            }
            //Exception for no default browser
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                //Let user know what happened
                if (noBrowser.ErrorCode == -2147467259)
                    ShowMessage("ERROR: No default browser found! " + noBrowser.Message);
            }
            //Unspecified exception
            catch (System.Exception other)
            {
                //Let user know what happened
                ShowMessage("ERROR: Unspecified exception! " + other.Message);
            }
            //Save log file no matter what happens
            finally
            {
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
            }
        }

        //Added v1.2.1
        //Changed v1.3.0, added exception handling
        //Opens a link to Conexus on Github
        void URL_Github_Click(object sender, RoutedEventArgs e)
        {
            //Attempt to open a link in the user's default browser
            try
            {
                //Let user know what's happening
                ShowMessage("INFO: Attempting to open link to Conexus' Github repository");
                //Attempt to open link
                System.Diagnostics.Process.Start("https://github.com/Hypocrita20XX/Conexus");
                //Let user know what happened
                ShowMessage("INFO: Link successfully opened");
            }
            //Exception for no default browser
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                //Let user know what happened
                if (noBrowser.ErrorCode == -2147467259)
                    ShowMessage("ERROR: No default browser found! " + noBrowser.Message);
            }
            //Unspecified exception
            catch (System.Exception other)
            {
                //Let user know what happened
                ShowMessage("ERROR: Unspecified exception! " + other.Message);
            }
            //Save log file no matter what happens
            finally
            {
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
            }
        }

        //Added v1.2.1
        //Changed v1.3.0, added exception handling
        //Opens a link to the wiki on Github
        void URL_Wiki_Click(object sender, RoutedEventArgs e)
        {
            //Attempt to open a link in the user's default browser
            try
            {
                //Let user know what's happening
                ShowMessage("INFO: Attempting to open link to Conexus' Github wiki");
                //Attempt to open link
                System.Diagnostics.Process.Start("https://github.com/Hypocrita20XX/Conexus/wiki");
                //Let user know what happened
                ShowMessage("INFO: Link successfully opened");
            }
            //Exception for no default browser
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                //Let user know what happened
                if (noBrowser.ErrorCode == -2147467259)
                    ShowMessage("ERROR: No default browser found! " + noBrowser.Message);
            }
            //Unspecified exception
            catch (System.Exception other)
            {
                //Let user know what happened
                ShowMessage("ERROR: Unspecified exception! " + other.Message);
            }
            //Save log file no matter what happens
            finally
            {
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
            }
        }

        //Added v1.2.1
        //Changed v1.3.0, added exception handling
        //Opens a link to the issue tracker on Github
        void URL_Issue_Click(object sender, RoutedEventArgs e)
        {
            //Attempt to open a link in the user's default browser
            try
            {
                //Let user know what's happening
                ShowMessage("INFO: Attempting to open link to Conexus' Github issue tracker");
                //Attempt to open link
                System.Diagnostics.Process.Start("https://github.com/Hypocrita20XX/Conexus/issues");
                //Let user know what happened
                ShowMessage("INFO: Link successfully opened");
            }
            //Exception for no default browser
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                //Let user know what happened
                if (noBrowser.ErrorCode == -2147467259)
                    ShowMessage("ERROR: No default browser found! " + noBrowser.Message);
            }
            //Unspecified exception
            catch (System.Exception other)
            {
                //Let user know what happened
                ShowMessage("ERROR: Unspecified exception! " + other.Message);
            }
            //Save log file no matter what happens
            finally
            {
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
            }
        }

        #endregion

        #region Checkbox Functionality

        //Added v1.2.0
        //Reveals the username when the checkbox is checked
        void UsernameReveal_Checked(object sender, RoutedEventArgs e)
        {
            //Assign the text in the textbox to the given password
            SteamUsername_TextBox.Text = SteamUsername.Password;
            //Hide the password box
            SteamUsername.Visibility = Visibility.Collapsed;
            //Show the text box
            SteamUsername_TextBox.Visibility = Visibility.Visible;
        }

        //Added v1.2.0
        //Hides the username when the checkbox is unchecked
        void UsernameReveal_Unchecked(object sender, RoutedEventArgs e)
        {
            //Assign the text in the password to what's in the text box
            SteamUsername.Password = SteamUsername_TextBox.Text;
            //Hide the text box
            SteamUsername_TextBox.Visibility = Visibility.Collapsed;
            //Show the password box
            SteamUsername.Visibility = Visibility.Visible;
        }

        //Added v1.2.0
        //Reveals the password when the checkbox is unchecked
        void PasswordReveal_Checked(object sender, RoutedEventArgs e)
        {
            //Assign the text in the textbox to the given password
            SteamPassword_TextBox.Text = SteamPassword.Password;
            //Hide the password box
            SteamPassword.Visibility = Visibility.Collapsed;
            //Show the text box
            SteamPassword_TextBox.Visibility = Visibility.Visible;
        }

        //Added v1.2.0
        //Hides the password when the checkbox is unchecked
        void PasswordReveal_Unchecked(object sender, RoutedEventArgs e)
        {
            //Assign the text in the password to what's in the text box
            SteamPassword.Password = SteamPassword_TextBox.Text;
            //Hide the text box
            SteamPassword_TextBox.Visibility = Visibility.Collapsed;
            //Show the password box
            SteamPassword.Visibility = Visibility.Visible;
        }

        #endregion

        #region Main Functionality 

        //Changed v1.2.0, to async
        //Main workhorse function
        async void OrganizeMods_Click(object sender, RoutedEventArgs e)
        {
            //Changed 1.3.0, to reflect Documents\Conexus
            //If this directory is deleted or otherwise not found, it needs to be created, otherwise stuff will break
            if (!Directory.Exists(dataPath))
            {
                ShowMessage("WARN: Conexus\\Data is missing! Creating now");

                Directory.CreateDirectory(dataPath);
            }

            //Added v1.2.0
            //Disable input during operation
            URLLink.IsEnabled = false;
            SteamCMDDir.IsEnabled = false;
            ModDir.IsEnabled = false;
            SteamUsername.IsEnabled = false;
            SteamUsername_TextBox.IsEnabled = false;
            UsernameReveal.IsEnabled = false;
            SteamPassword.IsEnabled = false;
            SteamPassword_TextBox.IsEnabled = false;
            cmbMode.IsEnabled = false;
            cmbPlatform.IsEnabled = false;
            PasswordReveal.IsEnabled = false;
            OrganizeMods.IsEnabled = false;

            //Added v1.2.1
            //Log info relating to what the user wants to do
            ShowMessage("INFO: Using " + System.Environment.OSVersion);

            //Added v1.3.0
            //Provide further clarification
            if (System.Environment.OSVersion.ToString().Contains("10"))
                ShowMessage("INFO: Using supported Windows version, 10");
            else
                ShowMessage("WARN: Potentially using unsupported OS!");

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //If the user wants to use a Steam collection, ensure all functionality relates to that
            if (steam)
            {
                //Added v1.2.1
                //Changed v1.3.0, formatting
                //Log info relating to what the user wants to do
                ShowMessage("INFO: Using a Steam collection");

                //Changed v1.2.0, to async
                //Check the provided URL to make sure it's valid
                if (await VerifyCollectionURLAsync(URLLink.Text, dataPath))
                {
                    //Added v1.3.0
                    urlcollection = URLLink.Text;

                    //It is assumed that at this point, the user has entered a valid URL to the collection
                    if (urlcollection.Length > 0)
                    {
                        ini["URL"]["Collection"] = urlcollection;
                        ini.Persist();

                        //Added v1.3.0
                        //Log info relating to what the user wants to do
                        ShowMessage("PROC: Collection URL has been saved");

                        //Added v1.3.0
                        //Save log to file
                        WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
                    }
                    //Otherwise we need to quit and provide an error message
                    else
                    {
                        //Added v1.2.0
                        //Changed v1.3.0, formatting
                        //Provide feedback
                        ShowMessage("WARN: Invalild URL! Process has stopped");

                        //Added v1.3.0
                        //Save log to file
                        WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                        //Added v1.2.0
                        //Enable input after operation
                        URLLink.IsEnabled = true;
                        SteamCMDDir.IsEnabled = true;
                        ModDir.IsEnabled = true;
                        SteamUsername.IsEnabled = true;
                        SteamUsername_TextBox.IsEnabled = true;
                        UsernameReveal.IsEnabled = true;
                        SteamPassword.IsEnabled = true;
                        SteamPassword_TextBox.IsEnabled = true;
                        cmbMode.IsEnabled = true;
                        cmbPlatform.IsEnabled = true;
                        PasswordReveal.IsEnabled = true;
                        OrganizeMods.IsEnabled = true;

                        //Exit out of this function
                        return;
                    }

                    //If the user wants to download mods, send them through that chain
                    if (downloadMods)
                    {
                        //Added v1.2.1
                        //Changed v1.3.0, formatting
                        //Log info relating to what the user wants to do
                        ShowMessage("INFO: User is downloading mods");

                        //Added v1.2.0
                        //Changed v1.3.0, formatting
                        //Provide feedback
                        ShowMessage("INFO: Mod info will now be obtained from the collection link");

                        //Changed v1.2.0, to async
                        //Create all necessary text files
                        await DownloadHTMLAsync(urlcollection, dataPath);

                        //Added v1.2.0
                        //Changed v1.3.0, formatting
                        //Provide feedback
                        ShowMessage("INFO: HTML has been downloaded and processed");

                        //Added v1.2.0
                        //Changed v1.3.0, formatting
                        //Provide feedback
                        ShowMessage("INFO: Mods will now be downloaded");

                        //Changed v1.2.0, to async
                        //Start downloading mods
                        await DownloadModsFromSteamAsync();

                        //Added v1.3.0
                        //Save log to file
                        WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
                    }

                    //Changed v1.2.0, to simplify if statement after implementing mod list addition support
                    //If the user wants to update mods, send them through that chain so long as they've run through the download chain once
                    if (updateMods)
                    {
                        //Added v1.2.1
                        //Changed v1.3.0, formatting
                        //Log info relating to what the user wants to do
                        ShowMessage("INFO: User is updating mods");

                        //Added v1.2.0
                        //Changed v1.3.0, formatting
                        //Provide feedback
                        ShowMessage("INFO: Mod info will now be updated");

                        //Added v1.2.0
                        //Force update the mod info text files to account for any additions in the collection
                        await DownloadHTMLAsync(urlcollection, dataPath);

                        //Added v1.2.0
                        //Changed v1.3.0, formatting
                        //Provide feedback
                        ShowMessage("INFO: Mods will now be updated");

                        //Changed v1.2.0, to async
                        await UpdateModsFromSteamAsync();
                    }
                }
                //URL is not valid, don't do anything
                else
                {
                    //Added v1.2.0
                    //Enable input after operation
                    URLLink.IsEnabled = true;
                    SteamCMDDir.IsEnabled = true;
                    ModDir.IsEnabled = true;
                    SteamUsername.IsEnabled = true;
                    SteamUsername_TextBox.IsEnabled = true;
                    UsernameReveal.IsEnabled = true;
                    SteamPassword.IsEnabled = true;
                    SteamPassword_TextBox.IsEnabled = true;
                    cmbMode.IsEnabled = true;
                    cmbPlatform.IsEnabled = true;
                    PasswordReveal.IsEnabled = true;
                    OrganizeMods.IsEnabled = true;

                    //Added v1.3.0
                    ShowMessage("WARN: Invalid URL!");

                    //Added v1.3.0
                    //Save log to file
                    WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                    return;
                }
            }
            //Otherwise, the user wants to use a list of URLs
            else
            {
                //Added v1.2.1
                //Changed v1.3.0, formatting
                //Log info relating to what the user wants to do
                ShowMessage("INFO: Using a list of links");

                //If the user wants to download mods, send them through that chain
                if (downloadMods)
                {
                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("INFO: Mod info will now be obtained from the Links file");

                    //Changed v1.2.0, to async
                    //Parse IDs from the user-populated list
                    await ParseFromListAsync(linksPath);

                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("INFO: Mod info has been obtained, mods will now be downloaded");

                    //Changed v1.2.0, to async
                    await DownloadModsFromSteamAsync();
                }

                //Changed v1.2.0, to simplify if statement after implementing mod list addition support
                //If the user wants to update mods, send them through that chain
                if (updateMods)
                {
                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("INFO: Mod info will now be updated");

                    //Changed v1.2.0, to async
                    await ParseFromListAsync(linksPath);

                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("INFO: Mods will now be updated");

                    //Changed v1.2.0, to async
                    await UpdateModsFromSteamAsync();
                }
            }

            //Added v1.2.0
            //Enable input after operation
            URLLink.IsEnabled = true;
            SteamCMDDir.IsEnabled = true;
            ModDir.IsEnabled = true;
            SteamUsername.IsEnabled = true;
            SteamUsername_TextBox.IsEnabled = true;
            UsernameReveal.IsEnabled = true;
            SteamPassword.IsEnabled = true;
            SteamPassword_TextBox.IsEnabled = true;
            cmbMode.IsEnabled = true;
            cmbPlatform.IsEnabled = true;
            PasswordReveal.IsEnabled = true;
            OrganizeMods.IsEnabled = true;

            //Added v1.2.0
            //Changed v1.3.0, formatting
            //Provide feedback
            ShowMessage("INFO: Selected process has finished successfully");

            //Added v1.3.0
            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.2.0, to async
        //Download source HTML from a given Steam collection URL
        async Task DownloadHTMLAsync(string url, string fileDir)
        {
            //Changed v1.3.0, to reflect Documents\Conexus
            //If the Data folder does not exist, create it
            if (!Directory.Exists(fileDir))
            {
                //Added v1.3.0
                ShowMessage("INFO: " + fileDir + " does not exist, creating now");

                Directory.CreateDirectory(fileDir);
            }

            //Added v1.2.0
            //Reset lists
            if (modInfo.Count > 0)
                modInfo.Clear();
            if (appIDs.Count > 0)
                appIDs.Clear();

            //Overwrite whatever may be in ModInfo.txt, if it exists
            if (File.Exists(dataPath + "\\ModInfo.txt"))
            {
                File.WriteAllText(dataPath + "\\ModInfo.txt", String.Empty);

                //Added v1.3.0
                ShowMessage("PROC: ModInfo contents have been ovewritten");
            }

            //Added v1.3.0
            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //Create a new WebClient
            WebClient webClient = new WebClient();
            //Download the desired collection and save the file
            await Task.Run(() => webClient.DownloadFile(url, fileDir + "\\HTML.txt"));
            //Added v1.2.0
            //Free up resources, cleanup
            webClient.Dispose();

            //Added v1.2.0
            //Changed v1.3.0, formatting
            ShowMessage("PROC: Source HTML downloaded successfully");

            //Added v1.3.0
            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //Move on to parsing through the raw source
            await IterateThroughHTMLAsync(fileDir);
        }

        //Changed v1.2.0, to async
        //Go through the source line by line
        async Task IterateThroughHTMLAsync(string fileDir)
        {
            //Temp variable to store an individual line
            string line;
            //List of strings to store a line that houses all neccesary info for each mod
            List<string> mods = new List<string>();
            //Create a file reader and load the previously saved source file
            StreamReader file = new StreamReader(@fileDir + "\\HTML.txt");

            //Added v1.3.0
            ShowMessage("PROC: Parsing HTML source now");

            //Iterate through the file one line at a time
            while ((line = file.ReadLine()) != null)
            {
                //If a line contains "a href" and "workshopItemTitle," then this line contains mod information
                if (line.Contains("a href") & line.Contains("workshopItemTitle"))
                {
                    //Add this line to the mods list
                    await Task.Run(() => mods.Add(line.Substring(line.IndexOf("<"))));

                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("PROC: Found mod info: " + line.Substring(line.IndexOf("<")));
                }
            }

            //Added v1.2.0
            //Changed 1.2.2, formatting
            //Provide feedback
            ShowMessage("PROC: Finished search for mod info in provided collection URL");

            //Added v1.2.0
            //Close file, cleanup
            file.Close();

            //Write this information to a file
            WriteToFile(mods.ToArray(), fileDir + "\\Mods.txt");

            //Added v1.2.0
            //Changed v1.3.0, formatting
            //Provide feedback
            ShowMessage("INFO: Mod info will now be seperated into its useful parts");

            //Added v1.3.0
            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //Move on to parsing out the relevant info
            await SeparateInfoAsync(fileDir);
        }

        //Changed v1.2.0, to async
        //Parses out all relevant info from the source's lines
        async Task SeparateInfoAsync(string fileDir)
        {
            //Temp variable to store an individual line
            string line;
            //Stores the initial folder index
            int folderIndex = 0;
            //Stores the final folder index (with leading zeroes)
            string folderIndex_S = "";
            //Load the previously stored file for further refinement
            StreamReader file = new StreamReader(@fileDir + "\\Mods.txt");

            //Add v1.3.0
            //Provide feedback
            ShowMessage("PROC: Parsing through source for all relevant data");

            //Iterate through each line the file
            while ((line = file.ReadLine()) != null)
            {
                //First pass, remove everything up to ?id=
                string firstPass = line.Substring(line.IndexOf("?id="));
                //Second pass, remove everything after </div>
                string secondPass = firstPass.Substring(0, firstPass.IndexOf("</div>"));
                //Strip the app id from the string and store that in its own variables
                string id = secondPass.Substring(0, secondPass.IndexOf("><div") - 1);
                //Remove remaining fluff from the id string
                id = id.Substring(4);
                //Strip the mod name from the string and store that in its own variable
                string name = secondPass.Substring(secondPass.IndexOf("\"workshopItemTitle\">") + ("\"workshopItemTitle\">").Length);
                //Remove any invalid characters in the mod name
                name = Regex.Replace(name, @"['<''>'':''/''\''|''?''*']", "", RegexOptions.None);
                
                //Add leading zeroes to the folder index, two if the index is less than 10
                if (folderIndex < 10)
                    folderIndex_S = "00" + folderIndex.ToString();

                //Add leading zeroes to the folder index, one if the index is more than 9 and less than 100
                if (folderIndex > 9 & folderIndex < 100)
                    folderIndex_S = "0" + folderIndex.ToString();

                //If the index is greater than 100, no leading zeroes should be added
                if (folderIndex > 100)
                    folderIndex_S = folderIndex.ToString();

                //Create the final name that will be used to identify the folder/mod
                string final = folderIndex_S + "_" + id + "_" + name;

                //Changed v1.2.0, to async
                //Add the final name to the modInfo list
                await Task.Run(() => modInfo.Add(final));

                //Changed v1.2.0, to async
                //Add the app id to the appIDs list
                await Task.Run(() => appIDs.Add(id));

                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Found mod info: " + final);

                //Increment folderIndex
                folderIndex++;
            }

            //Added v1.2.0
            //Changed v1.3.0, formatting
            //Provide feedback
            ShowMessage("INFO: Finished finalizing each mods' information");

            //Added v1.2.0
            //Close file, cleanup
            file.Close();

            //Write the modInfo to a text file
            WriteToFile(modInfo.ToArray(), @fileDir + "\\ModInfo.txt");

            //Added v1.3.0
            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.2.0, to async
        //Go through the provided text file and find/create relevant mod info
        async Task ParseFromListAsync(string fileDir)
        {
            //Examples:
            // > Format: https://steamcommunity.com/sharedfiles/filedetails/?id=1282438975
            // > Ignore: * 50% Stealth Chance in Veteran Quests

            //Overwrite whatever may be in ModInfo.txt, if it exists
            if (File.Exists(linksPath + "\\ModInfo.txt"))
            {
                File.WriteAllText(linksPath + "\\ModInfo.txt", String.Empty);

                //Added v1.3.0
                ShowMessage("PROC: ModInfo contents have been ovewritten");
            }

            //Added v1.2.0
            //Reset lists
            if (modInfo.Count > 0)
                modInfo.Clear();
            if (appIDs.Count > 0)
                appIDs.Clear();

            //Added v1.3.0
            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //Temp variable to store an individual line
            string line;
            //Stores the initial folder index
            int folderIndex = 0;
            //Stores the final folder index (with leading zeroes)
            string folderIndex_S = "";
            //Load the previously stored file for further refinement
            StreamReader file = new StreamReader(@fileDir + "\\Links.txt");

            //Added v1.3.0
            ShowMessage("PROC: Links.txt will now be parsed for mod info");

            //Iterate through each line the file
            while ((line = file.ReadLine()) != null)
            {
                //If the line being looked at is a comment, marked by *, then skip this line
                //Otherwise, we need to get the ID from this line
                if (!line.Contains("*"))
                {
                    //Remove everything up to ?id=, plus 4 to remove ?id= in the link
                    string id = line.Substring(line.IndexOf("?id=") + 4);

                    //Add leading zeroes to the folder index, two if the index is less than 10
                    if (folderIndex < 10)
                        folderIndex_S = "00" + folderIndex.ToString();

                    //Add leading zeroes to the folder index, one if the index is more than 9 and less than 100
                    if (folderIndex > 9 & folderIndex < 100)
                        folderIndex_S = "0" + folderIndex.ToString();

                    //If the index is greater than 100, no leading zeroes should be added
                    if (folderIndex > 100)
                        folderIndex_S = folderIndex.ToString();

                    //Add the final name to the modInfo list
                    await Task.Run(() => modInfo.Add(folderIndex_S + "_" + id));

                    //Add this ID to the appIDs list
                    await Task.Run(() => appIDs.Add(id));

                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("PROC: Found mod info in Links file: " + folderIndex_S + "_" + id);

                    //Increment folderIndex
                    folderIndex++;
                }
            }

            //Added v1.2.0
            //Changed v1.3.0, formatting
            //Provide feedback
            ShowMessage("INFO: Finished processing mod info in Links file");

            //Write the modInfo to a text file
            WriteToFile(modInfo.ToArray(), @fileDir + "\\ModInfo.txt");

            //Added v1.2.0
            //Close file, cleanup
            file.Close();

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.2.0, to async
        //Handles downloading mods through SteamCMD
        async Task DownloadModsFromSteamAsync()
        {
            //Stores the proper commands that will be passed to SteamCMD
            string cmdList = "";

            //Added v1.3.0
            //Provide feedback
            ShowMessage("PROC: Commands will now be obtained");

            //Get a list of commands for each mod stored in a single string
            for (int i = 0; i < appIDs.Count; i++)
            {
                //Changed v1.2.0, to async
                await Task.Run(() => cmdList += "+\"workshop_download_item 262060 " + appIDs[i] + "\" ");

                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Adding command to list: " + " +\"workshop_download_item 262060 " + appIDs[i] + "\" ");
            }

            //Added v1.2.0
            //Changed v1.3.0, formatting
            //Provide feedback
            ShowMessage("INFO: SteamCMD will take over now");

            //Added v1.3.0
            //Save logs to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //Create a process that will contain all relevant SteamCMD commands for all mods
            ProcessStartInfo processInfo = new ProcessStartInfo(steamcmd + "\\steamcmd.exe", " +login " + SteamUsername.Password + " " + SteamPassword.Password + " " + cmdList + "+quit");

            //Create a wrapper that will run all commands, wait for the process to finish, and then proceed to copying and renaming folders/files
            using (Process process = new Process())
            {
                //Set the commands for this process
                process.StartInfo = processInfo;
                //Start the process with the provided commands
                await Task.Run(() => process.Start());
                //Changed v1.2.0, to async
                //Wait until SteamCMD finishes
                await Task.Run(() => process.WaitForExit());
                //Move on to copying and renaming the mods
                await RenameAndMoveModsAsync("DOWNLOAD");
            }

            //Added v1.3.0
            //Provide feedback
            ShowMessage("INFO: SteamCMD has finished downloading mods");

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.2.0, to async
        //Handles updating mods through SteamCMD
        async Task UpdateModsFromSteamAsync()
        {
            //Move all mods from the mods directory to the SteamCMD directory for updating.
            await RenameAndMoveModsAsync("UPDATE");

            //Stores the proper commands that will be passed to SteamCMD
            string cmdList = "";

            //Added v1.3.0
            //Provide feedback
            ShowMessage("PROC: Commands will now be obtained");

            //Get a list of commamds for each mod stored in a single string
            for (int i = 0; i < appIDs.Count; i++)
            {
                //Changed v1.2.0, to async
                await Task.Run(() => cmdList += "+\"workshop_download_item 262060 " + appIDs[i] + "\" ");

                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Adding command to list: " + " +\"workshop_download_item 262060 " + appIDs[i] + "\" ");
            }

            //Added v1.2.0
            //Changed v1.3.0, formatting
            //Provide feedback
            ShowMessage("INFO: SteamCMD will take over now");

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //Create a process that will contain all relevant SteamCMD commands for all mods
            ProcessStartInfo processInfo = new ProcessStartInfo(steamcmd + "\\steamcmd.exe", " +login " + SteamUsername.Password + " " + SteamPassword.Password + " " + cmdList + "+quit");

            //Create a wrapper that will run all commands, wait for the process to finish, and then proceed to copying and renaming folders/files
            using (Process process = new Process())
            {
                //Set the commands for this process
                process.StartInfo = processInfo;
                //Changed v1.2.0, to async
                //Start the commandline process
                await Task.Run(() => process.Start());
                //Changed v1.2.0, to async
                //Wait until SteamCMD finishes
                await Task.Run(() => process.WaitForExit());
                //Move on to copying and renaming the mods
                await RenameAndMoveModsAsync("DOWNLOAD");
            }

            //Added v1.3.0
            //Provide feedback
            ShowMessage("INFO: SteamCMD has finished downloading mods");

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.2.0, to async
        //Creates organized folders in the mods directory, then copies files from the SteaCMD directory to those folders
        //Requires that an operation be specified (DOWNLOAD or UPDATE)
        async Task RenameAndMoveModsAsync(string DownloadOrUpdate)
        {
            //Create source/destination path list variables
            string[] source = new string[appIDs.Count];
            string[] destination = new string[modInfo.Count];

            //If the user has downloaded/updated mods, copy all files/folders from the SteamCMD directory to the mod directory
            if (DownloadOrUpdate == "DOWNLOAD")
            {
                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Acquiring paths to copy from");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Get the proper path to copy from
                for (int i = 0; i < appIDs.Count; i++)
                {
                    //Changed v1.2.0, to async
                    await Task.Run(() => source[i] = Path.Combine(steamcmd + "\\steamapps\\workshop\\content\\262060\\", appIDs[i]));
                }

                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Acquiring paths to copy to");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Get the proper path that will be copied to
                for (int i = 0; i < modInfo.Count; i++)
                {
                    //Changed v1.2.0, to async
                    await Task.Run(() => destination[i] = Path.Combine(mods, modInfo[i]));
                }

                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Copying files, please wait");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Copy all folders/files from the SteamCMD directory to the mods directory
                for (int i = 0; i < destination.Length; i++)
                {
                    //Changed v1.2.0, to async
                    await CopyFoldersAsync(source[i], destination[i]);

                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("PROC: Files copied from " + source[i] + " to " + destination[i]);
                }

                //Added v1.2.0
                //Changed v1.3.0, formatting/phrasing
                //Provide feedback
                ShowMessage("INFO: Files copied, original copy will now be deleted");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Check to ensure the last mod is in the destination directory
                if (Directory.Exists(destination[modInfo.Count - 1]) && modInfo.Count != 0)
                {
                    //Added v1.2.0
                    //Hopefully more reliable directory deletion
                    DirectoryInfo dirInfo = new DirectoryInfo(@steamcmd + "\\steamapps\\workshop\\content\\262060\\");

                    foreach (var dir in Directory.GetDirectories(@steamcmd + "\\steamapps\\workshop\\content\\262060\\"))
                    {
                        //if (!dir.Contains("_DD_TextFiles") && !dir.Contains("_Logs"))
                        //{
                            await Task.Run(() => Directory.Delete(dir, true));

                            //Changed v1.3.0, formatting
                            //Provide feedback
                            ShowMessage("PROC: " + dir + " deleted");
                        //}
                    }

                    //Added v1.2.0
                    //Changed v1.3.0, formatting/phrasing
                    //Provide feedback
                    ShowMessage("INFO: Original copies have been deleted");

                    //Added v1.3.0
                    //Save log to file
                    WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
                }
            }

            //If the userwants to update mods, copy all files/folders from the mod directory to the SteamCMD directory
            if (DownloadOrUpdate == "UPDATE")
            {
                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Acquiring paths to copy from");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Get the proper path to copy from
                for (int i = 0; i < appIDs.Count; i++)
                {
                    //Changed v1.2.0, to async
                    await Task.Run(() => source[i] = Path.Combine(mods + "\\", modInfo[i]));
                }

                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Acquiring paths to copy to");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Get the proper path that will be copied to
                for (int i = 0; i < modInfo.Count; i++)
                {
                    //Changed v1.2.0, to async
                    await Task.Run(() => destination[i] = Path.Combine(steamcmd + "\\steamapps\\workshop\\content\\262060\\", appIDs[i]));
                }

                //Added v1.2.0
                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("PROC: Copying files, please wait");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Copy all folders/files from the SteamCMD directory to the mods directory
                for (int i = 0; i < destination.Length; i++)
                {
                    //Changed v1.2.0, to async
                    await CopyFoldersAsync(source[i], destination[i]);

                    //Added v1.2.0
                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("PROC: Files copied from " + source[i] + " to " + destination[i]);
                }

                //Added v1.2.0
                //Changed v1.3.0, formatting/phrasing
                //Provide feedback
                ShowMessage("PROC: Files have been copied, deleting originals");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //Check to ensure the last mod is in the destination directory
                if (Directory.Exists(destination[modInfo.Count - 1]) && modInfo.Count != 0)
                {
                    //Added v1.2.0
                    //Hopefully more reliable directory deletion
                    DirectoryInfo dirInfo = new DirectoryInfo(@mods);

                    foreach (var dir in Directory.GetDirectories(@mods))
                    {
                        //if (!dir.Contains("_DD_TextFiles") && !dir.Contains("_Logs"))
                        //{
                            await Task.Run(() => Directory.Delete(dir, true));

                            //Changed v1.3.0, formatting
                            //Provide feedback
                            ShowMessage("PROC: " + dir + " deleted");
                        //}
                    }

                    //Added v1.2.0
                    //Changed v1.3.0, formatting/phrasing
                    //Provide feedback
                    ShowMessage("INFO: Original copies have been deleted");

                    //Added v1.3.0
                    //Save log to file
                    WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
                }
            }

            //Added v1.2.0
            //Changed v1.3.0, formatting
            //Provide feedback
            ShowMessage("INFO: Mods have now been moved and renamed, originals have been deleted");

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.2.0, to async
        //A base function that will copy/rename any given folder(s)
        //Can be used recursively for multiple directories
        async Task CopyFoldersAsync(string source, string destination)
        {
            //Check if the directory exists, if not, create it
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);

            //Added v1.2.0
            //Ok this is a really lazy way to do this, but it works, so I don't care
            //To allow for easy additions to a collection, we make sure the mods have a folder
            //Prior to this, the HTML functions are processed, so to avoid crashes,
            //we just create an empty dummy folder
            if (!Directory.Exists(source))
            {
                //Added v1.3.0
                //Provide feedback
                ShowMessage("PROC: " + source + " does not exist, creating now (new mod detected)");

                Directory.CreateDirectory(source);
            }

            //Create an array of strings containing all files in the given source directory
            string[] files = Directory.GetFiles(source);

            //Iterate through these files and copy to the destination
            foreach (string file in files)
            {
                //Get the name of the file
                string name = Path.GetFileName(file);
                //Get the destination for this file
                string dest = Path.Combine(destination, name);

                //Changed v1.2.0, to async
                //Copy this file to the destination
                await Task.Run(() => File.Copy(file, dest, true));
            }

            //Create an array of strings containing any and all sub-directories
            string[] folders = Directory.GetDirectories(source);

            //Iterate through these sub-directories
            foreach (string folder in folders)
            {
                //Get the name of the folder
                string name = Path.GetFileName(folder);
                //Get the destination for this folder
                string dest = Path.Combine(destination, name);

                //Changed v1.2.0, to async
                //Recursively copy any files in this directory, any sub-directories, and all files therein
                await CopyFoldersAsync(folder, dest);
            }

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Utility function to write text to a file
        void WriteToFile(string[] text, string fileDir)
        {
            //Added v1.3.0
            if (!File.Exists(fileDir))
                File.Create(fileDir).Dispose();
            else
                File.WriteAllLines(@fileDir, text);
        }

        //Added v1.2.0
        //Changed v1.3.0, overall formatting, functionality to handle unitialized textblock so I can use this everywhere 
        //without interspersing with random temp variables
        //Utility function to handle messages
        void ShowMessage(string msg)
        {
            //Added v1.3.0
            //Because I insist on proper formatting, here's a series of if statements
            //whose only job is to add leading zeroes
            //Yes, we're going up to a million lines
            //No, no sane person should ever reach this
            //I'm doing it anyway

            //Create a temporary string
            string lcStr = "";
            //If lineCount is less than 10, add six leading zeroes
            if (lineCount < 10)
                lcStr = "000000" + lineCount.ToString();
            //If lineCount is less than 100 and greater than/equal to 10, add five leading zeros
            else if (lineCount < 100 && lineCount >= 10)
                lcStr = "00000" + lineCount.ToString();
            //If lineCount is less than 1000 and greater than/equal to 100, add four leading zeroes
            else if (lineCount < 1000 && lineCount >= 100)
                lcStr = "0000" + lineCount.ToString();
            //If lineCount is less than 10000 and greater than/equal to than 1000, add three leading zeroes
            else if (lineCount < 10000 && lineCount >= 1000)
                lcStr = "000" + lineCount.ToString();
            //If lineCount is less than 100000 and greater than/equal to than 10000, add two leading zeroes
            else if (lineCount < 100000 && lineCount >= 10000)
                lcStr = "00" + lineCount.ToString();
            //If lineCount is less than 1000000 and greater than/equal to than 100000, add one leading zero
            else if (lineCount < 1000000 && lineCount >= 100000)
                lcStr = "0" + lineCount.ToString();
            //If lineCount is greater than 1000000, no leading zeroes are needed
            else if (lineCount >= 1000000)
            {
                //Seriously, how?
                ShowMessage("Impressive, and I thought I liked mods. Nice!)");
                lcStr = lineCount.ToString();
            }
            //If it's somehow something else, no leading zeroes
            else
                lcStr = lineCount.ToString();

            //Added v1.3.0
            //Check to see if the Messages extblock is loaded
            //If so, proceed as normal
            if (Messages != null && Messages.IsLoaded)
            {
                //Added v1.3.0
                //If logTmp is not empty, then messages were added before the textblock was initiated
                //and those messages should be added now to the textblock
                if (logTmp.Count > 0)
                {
                    //Show desired message with appropriate line count
                    //Messages.Text += logTmp;

                    for (int i = 0; i < logTmp.Count; i++)
                    {
                        //Show desired message without line count or date
                        Messages.Text += logTmp[i].Substring(logTmp[i].IndexOf("*")+1);
                        //Remove the asterisk to provide a properly formatted log message
                        string tmp = Regex.Replace(logTmp[i], @"['*']", " ", RegexOptions.None);
                        //Save this message to the log list
                        log.Add(tmp.Substring(0, tmp.Length - 2));
                    }

                    //Clear out logTmp
                    logTmp.Clear();

                    //This specific part of the program will only hit once, so we can safely do this twice without issue
                    //Add the current message to the textblock and list
                    //Show desired message with appropriate line count
                    Messages.Text += msg + "\n";
                    //Save this message to the log list
                    log.Add("[" + lcStr + "] " + "[" + Regex.Replace(DateTime.Now.ToString(), @"['<''>'':''/''\''|''?''*'' ']", "_", RegexOptions.None) + "] " + msg);

                    //Increment lineCount
                    lineCount++;

                    //Scroll to the end of the scroll viewer
                    MessageScrollViewer.ScrollToEnd();
                }
                //Otherwise, proceed as normal
                else
                {
                    //Show desired message with appropriate line count
                    Messages.Text += msg + "\n";
                    //Save this message to the log list
                    log.Add("[" + lcStr + "] " + "[" + Regex.Replace(DateTime.Now.ToString(), @"['<''>'':''/''\''|''?''*'' ']", "_", RegexOptions.None) + "] " + msg);

                    //Increment lineCount
                    lineCount++;
                }
            }
            //Otherwise, the message needs stored until it is loaded and can accept messages
            else
            {
                //Messages textblock has not initiated yet, so we need to store the messages until it is
                //Add an asterisk at the end of the date for later removal of the line count and date for the log window
                logTmp.Add("[" + lcStr + "] " + "[" + Regex.Replace(DateTime.Now.ToString(), @"['<''>'':''/''\'' | '' ? '' * '' ']", "_", RegexOptions.None) + "]*" + msg + "\n");

                //Increment lineCount
                lineCount++;
            }
        }

        #endregion

        #region Verification Functionality

        //Added v1.2.0
        //Changed v1.3.0, added logging and log saving
        //Goes through several verification steps to ensure a proper Steam collection URL has been entered
        async Task<bool> VerifyCollectionURLAsync(string url, string fileDir)
        {
            /*
             * 
             * URL verification is a bit tricky
             * This is because Steam has a landing page with "valid" results, 
             * as opposed to a 404 page, or similar.
             * Because of this, the HTML contents of the given URL need to be downloaded
             * so that we can be sure we have a valid collection URL.
             * 
             * This validation only tests for links that have somehow gotten messed up
             * IE https://steamcommunity.com/workshop/filedetails/?id=2362884526 (valid) versus
             * https://steamcommunity.com/workshop/filfadsfafaedetails/?id=2362884526 (invalid)
             * 
             * It does not test for something such as https://steamc431241134ommunity.com/workshop/filedetails/?id=2362884526
             * which will not lead to a Steam site at all (in fact it leads to a completely invalid site)
             * For this, we need another validation, which checks if the link is in any way valid
             * 
             * There also needs to be a check to ensure that the site is actually for Steam
             * For instance someone accidently pastes a Youtube link instead of a Steam collection link.
             * This check basically will search through a line or two of the HTML code
             * and compare it to a known good Steam site
             * 
             * Order of validation:
             * 1.) Check for any valid link
             * 2.) Check to make sure it's a Steam site
             * 3.) Check to make sure it leads to a collection
             * 
             */

            //Create a new WebClient
            WebClient webClient = new WebClient();

            //Assume the URL is valid unless an exception occurs
            bool validURL = true;

            //Attempt to download the HTML from the provided URL
            try
            {
                //Added v1.3.0
                ShowMessage("INFO: Attempting to download HTML from given collection URL");
                //Download the desired collection and save the file
                await Task.Run(() => webClient.DownloadFile(url, fileDir + "\\HTML.txt"));
                //Added v1.3.0
                ShowMessage("INFO: Successfully downloaded HTML");
            }
            //Not a valid URL
            catch (WebException)
            {
                //Clear URLLink Text
                URLLink.Text = string.Empty;
                //Provide a message to the user
                //URLLink.Watermark = "Not a valid URL: " + url;
                //Flag this URL as invalid
                validURL = false;
                //Changed v1.3.0, better formatting
                //Provide additional logging
                ShowMessage("ERROR: Provided URL is not valid!");
            }
            //No URL at all, or something else that was unexpected
            catch (ArgumentException)
            {
                //Clear URLLink Text
                URLLink.Text = string.Empty;
                //Provide a message to the user
                //URLLink.Watermark = "Not a valid URL: " + url;
                //Flag this URL as invalid
                validURL = false;
                //Changed v1.3.0, better formatting
                //Provide additional logging
                ShowMessage("ERROR: Provided URL is not valid or does not exist!");
            }
            //I don't know why this triggers, but it does, and it's not for valid reasons
            catch (NotSupportedException)
            {
                //Clear URLLink Text
                URLLink.Text = string.Empty;
                //Provide a message to the user
                URLLink.Watermark = "Not a valid URL: " + url;
                //Flag this URL as invalid
                validURL = false;
                //Changed v1.3.0, better formatting
                //Provide additional logging
                ShowMessage("ERROR: Provided URL is not valid!");
            }
            //URL is too long
            catch (PathTooLongException)
            {
                //Clear URLLink Text
                URLLink.Text = string.Empty;
                //Provide a message to the user
                //URLLink.Watermark = "URL is too long (more than 260 characters)";
                //Flag this URL as invalid
                validURL = false;
                //Changed v1.3.0, better formatting
                //Provide additional logging
                ShowMessage("ERROR: Provided URL is too long! (Greater than 260 characters)");
            }
            finally
            {
                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
            }

            //If the link is valid, leads to an actual site, we need to check for a valid Steam site
            if (validURL)
            {
                //Added v1.3.0
                ShowMessage("VERIFY: Given link is valid");

                //Download the desired collection and save the file
                await Task.Run(() => webClient.DownloadFile(url, fileDir + "\\HTML.txt"));

                //Added v1.3.0
                ShowMessage("INFO: HTML source has been downloaded");

                /*
                 * Now we need to check to see if this is a valid Steam site
                 * 
                 * We need something to compare to though, to do this
                 * 
                 * A valid Steam site has various tells, 
                 * most important of which is that it will contain links that start with https://steamcommunity-a.akamaihd.net
                 * These links start on line 8 on several Steam sites I looked like, but start on line 12 in a collection
                 * Because of this slight discrepency, we need to look at a range of lines
                 * Let's say we start at line 0,up to 50 (as this is zero-based, we'll stop at 49)
                 * We shouldn't go further than is needed though, as this will affect overall performance
                 * 
                 * While we're doing this check, we'll also look for the next verification's tell, "Steam Workshop: Darkest Dungeon"
                 * Both checks use the same iteration process and should be combined for performance reasons
                 * Because of this, we'll go further than the previous 50 lines, to 100 (stopping at 99)
                 * The HTML I looked at contained this tell on line 71, but we need to be sure
                 * 
                 */

                //Temp variable to store an individual line
                string line;
                //List of strings to store all ines in a given range
                List<string> lines = new List<string>();
                //Create a file reader and load the saved HTML file
                StreamReader file = new StreamReader(@fileDir + "\\HTML.txt");

                //Keeps track of line count
                int lineCount = 0;

                //Stores the result of the verification check for a valid Steam link
                bool isValidSteam = false;
                //Stores the result of the verification check for a valid Steam collection link
                bool isValidCollection = false;

                //Changed v1.3.0, formatting
                //Provide feeback
                ShowMessage("PROC: Searching for valid Steam collection links");

                //Iterate through the given file up to line 100, line by line
                while ((line = file.ReadLine()) != null && lineCount < 100)
                {
                    //Check 2
                    //If we find a line that contains "https://steamcommunity-a.akamaihd.net", we can safely say this is a Steam link
                    if (line.Contains("steamcommunity-a.akamaihd.net"))
                        isValidSteam = true;

                    //If we find a line that contains "Steam Workshop: Darkest Dungeon", we can say this is a Steam Collection link
                    if (line.Contains("Steam Workshop: Darkest Dungeon"))
                        isValidCollection = true;

                    //Increment lineCount
                    lineCount++;
                }

                //Changed v1.3.0, formatting
                //Provide feeback
                ShowMessage("PROC: Search complete");

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                //If these checks fail, this is not a valid Steam collection link and the user needs to know that
                if (!isValidSteam && !isValidCollection || isValidSteam && !isValidCollection)
                {
                    //Clear URLLink Text
                    URLLink.Text = string.Empty;
                    //Provide a message to the user
                    URLLink.Watermark = "Not a valid URL: " + url;

                    //Cleanup
                    webClient.Dispose();
                    file.Close();

                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("WARN: No Steam collection link found, please check the link provided!");

                    //Added v1.3.0
                    //Save log to file
                    WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                    return false;
                }
                else
                {
                    //Cleanup
                    webClient.Dispose();
                    file.Close();

                    //Changed v1.3.0, formatting
                    //Provide feedback
                    ShowMessage("VERIFY: A valid Steam collection link has been found");

                    //Added v1.3.0
                    //Save log to file
                    WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                    return true;
                }
            }
            //Otherwise this is not a valid link and the user needs to know that
            else
            {
                //Clear URLLink Text
                URLLink.Text = string.Empty;
                //Provide a message to the user
                URLLink.Watermark = "Not a valid URL: " + url;

                //Changed v1.3.0, formatting
                //Provide feedback
                ShowMessage("WARN: No valid Steam collection link found, please check the link provided!");

                //Cleanup
                webClient.Dispose();

                //Added v1.3.0
                //Save log to file
                WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

                return false;
            }
        }

        //Added v1.2.0
        //Goes through several verification steps to ensure that the given SteamCMD directory is valid, contains steamcmd.exe
        bool VerifySteamCMDDir(string fileDir)
        {
            /*
             * 
             * This verification check is fairly straightforward: we check the given directory and see if we can find steamcmd.exe
             * Steamcmd.exe is thankfully located in the root directory, which is what the user is asked to find
             * so we can assume that if it's not in the given directory, the given directory is not valid
             * 
             */

            //Verify if this directory contains steamcmd.exe
            if (File.Exists(fileDir + "\\steamcmd.exe"))
                return true;
            else
                return false;
        }

        //Added v1.2.0
        //Goes through several verification steps to ensure that the given mods directory is valid, contains Darkest.exe
        bool VerifyModDir(string fileDir)
        {
            /*
             * 
             * This verification check is fairly straightforward: we check the given directory and see if we can find Darkest.exe
             * Unlike steamcmd.exe, this is located in a different folder DarkestDungeon\_windows
             * So we first need to navigate to the root directory, then to _windows and check for the exe
             * 
             */

            try
            {
                //Temp string to store the root directory
                string dirRoot = fileDir.Substring(0, fileDir.Length - 5);
                //Temp string to store the _windows directory
                string win = dirRoot + "\\_windows";

                //Verify if this directory contains steamcmd.exe
                if (File.Exists(win + "\\Darkest.exe"))
                    return true;
                else
                    return false;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        #endregion

        #region Data Saving Functionality

        //Changed v1.3.0, added logging/log saving, exception handling in case userdata is corrupt
        //Called when the UI window has loaded, used to set proper info in the UI from the settings file
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Added v1.3.0
            //A bunch of checks to make sure every necessary path exists
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
                Directory.CreateDirectory(dataPath);
                Directory.CreateDirectory(configPath);
                Directory.CreateDirectory(linksPath);
                Directory.CreateDirectory(logsPath);

                //Added v1.3.0
                ShowMessage("INFO: No folder found in User\\Documents");
                ShowMessage("INFO: Created Conexus\\Config, Conexus\\Data, Conexus\\Links, and Conexus\\Logs");
            }

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);

                //Added v1.3.0
                ShowMessage("WARN: Conexus\\Data missing! Folder created");
            }

            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);

                //Added v1.3.0
                ShowMessage("WARN: Conexus\\Config missing! Folder created");
            }

            if (!Directory.Exists(linksPath))
            {
                Directory.CreateDirectory(linksPath);

                //Added v1.3.0
                ShowMessage("WARN: Conexus\\Links missing! Folder created");
            }

            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);

                //Added v1.3.0
                ShowMessage("WARN: Conexus\\Logs missing! Folder created");
            }

            //Changed v1.3.0, to use Documents\Conexus
            //Make sure that Links.txt exists
            if (!File.Exists(linksPath + "\\Links.txt"))
            {
                File.Create(linksPath + "\\Links.txt").Dispose();

                //Added v1.3.0
                ShowMessage("VERIFY: Links.txt not found, creating file");
            }
            else
            {
                //Added v1.3.0
                ShowMessage("VERIFY: Links.txt found");
            }


            if (File.ReadAllBytes(configPath + "\\config.ini").Length == 0)
            {
                //Initialize data structure
                ini["System"]["Root"] = rootPath.Replace(@"\\", @"\");
                ini["System"]["Data"] = dataPath.Replace(@"\\", @"\");
                ini["System"]["Config"] = configPath.Replace(@"\\", @"\");
                ini["System"]["Links"] = linksPath.Replace(@"\\", @"\");
                ini["System"]["Logs"] = logsPath.Replace(@"\\", @"\");

                ini["Directories"]["Mods"] = "";
                ini["Directories"]["SteamCMD"] = "";

                ini["URL"]["Collection"] = "";

                ini["Misc"]["Mode"] = "download";
                ini["Misc"]["Method"] = "steam";

                ini["Login"]["Username"] = "";
                ini["Login"]["Password"] = "";

                ini.Persist();

                //Added v1.3.0
                ShowMessage("VERIFY: Created INI with default settings");
            }
            else
            {
                //Read values from the INI file
                root = ini["System"]["Root"].Replace(@"\\", @"\");
                data = ini["System"]["Data"].Replace(@"\\", @"\");
                config = ini["System"]["Config"].Replace(@"\\", @"\");
                links = ini["System"]["Links"].Replace(@"\\", @"\");
                logs = ini["System"]["Logs"].Replace(@"\\", @"\");

                mods = ini["Directories"]["Mods"].Replace(@"\\", @"\");
                steamcmd = ini["Directories"]["SteamCMD"].Replace(@"\\", @"\");

                urlcollection = ini["URL"]["Collection"].Replace(@"\\", @"\");

                mode = ini["Misc"]["Mode"].Replace(@"\\", @"\");
                method = ini["Misc"]["Method"].Replace(@"\\", @"\");

                username = ini["Login"]["Username"].Replace(@"\\", @"\");
                password = ini["Login"]["Password"].Replace(@"\\", @"\");

                //Added v1.3.0
                ShowMessage("VERIFY: Loaded INI");
            }

            //Changed v1.3.0, now uses INI
            //Check the contents of the ini variable in the settings file, if so, set the UI variable to it
            if (urlcollection != "")
            {
                URLLink.Text = urlcollection;

                //Added v1.3.0
                ShowMessage("VERIFY: Now showing the saved URL on the UI");
            }
            else
            {
                URLLink.Text = string.Empty;

                //Added v1.3.0
                ShowMessage("VERIFY: No saved collection URL");
            }

            //Changed v1.3.0, now uses INI
            //Check the contents of the ini variable in the settings file, if so, set the UI variable to it
            if (steamcmd != "")
            {
                SteamCMDDir.Content = steamcmd;

                //Added v1.3.0
                ShowMessage("VERIFY: Saved SteamCMD directory found, now showing on the UI");
            }
            else
            {
                SteamCMDDir.Content = "Select SteamCMD Directory";
                //Added v1.3.0
                ShowMessage("VERIFY: No saved SteamCMD directory found");
            }

            //Changed v1.3.0, now uses INI
            //Check the contents of the ini variable in the settings file, if so, set the UI variable to it
            if (mods != "")
            {
                ModDir.Content = mods;

                //Added v1.3.0
                ShowMessage("VERIFY: Saved mods directory found, now showing on the UI");
            }
            else
            {
                ModDir.Content = "Select Mods Directory";
                //Added v1.3.0
                ShowMessage("VERIFY: No saved mods directory found");
            }

            //Added v1.2.0
            //Changed v1.3.0, now uses INI
            //Check the contents of the ini variable in the settings file, if so, set the UI variable to it
            if (username != "")
            {
                SteamUsername.Password = username;

                //Added v1.3.0
                ShowMessage("VERIFY: Saved Steam username found, now showing (obscured) on the UI");
            }
            else
            {
                SteamUsername.Password = "";

                //Added v1.3.0
                ShowMessage("VERIFY: No saved Steam username found");
            }

            //Added v1.2.0
            //Changed v1.3.0, now uses INI
            //Check the contents of the ini variable in the settings file, if so, set the UI variable to it
            if (password != "")
            {
                SteamPassword.Password = password;

                //Added v1.3.0
                ShowMessage("VERIFY: Saved Steam password found, now showing (obscured) on the UI");
            }
            else
            {
                SteamPassword.Password = "";

                //Added v1.3.0
                ShowMessage("VERIFY: No saved Steam password found");
            }

            //Changed v1.3.0, now uses INI
            //Check the platform variable and set the platform combobox accordingly
            if (method == "steam")
            {
                cmbPlatform.SelectedIndex = 0;
                steam = true;

                //Added v1.3.0
                ShowMessage("VERIFY: Saved preferred method found: Steam collection");
            }
            else if (method == "other")
            {
                cmbPlatform.SelectedIndex = 1;
                steam = false;

                //Added v1.3.0
                ShowMessage("VERIFY: Saved preferred method found: list of links");
            }

            //Added v1.3.0
            if (mode == "download")
            {
                //Added v1.3.0
                cmbMode.SelectedIndex = 0;

                //Added v1.3.0
                ShowMessage("VERIFY: \"Download Mods\" has been set as the designated mode");
            }
            else if (mode == "update")
            {
                //Added v1.3.0
                cmbMode.SelectedIndex = 1;

                //Added v1.3.0
                ShowMessage("VERIFY: \"Update Mods\" has been set as the designated mode");
            }

            //Added v1.3.0
            //A check to see if the user has downloaded mods
            //Hopefully the simplicity of this solution won't bite me in the butt
            //If the mods path is not empty, let's check the directory and see if any folders are in there
            if (mods != "" && Directory.GetDirectories(mods).Length > 0)
            {
                //Added v1.3.0
                mode = "update";
            }
            //Otherwise if there is no mods path, let's just assume they need to download
            else if (mods == "")
            {
                //Added v1.3.0
                mode = "download";
            }

            loaded = true;
        }

        //Changed v1.3.0, added logging/log saving
        //Called right after the user indicates they want to close the program (through the use of the "X" button)
        //Used to ensure all proper data is set to their corrosponding variables in the settings file
        void Window_Closing(object sender, EventArgs e)
        {
            //Changed v1.3.0, now uses INI
            //Check to ensure the URLLink content is indeed provided (length greater than 0 indicates data in the field)
            //Make sure the variable in the settings file is correct
            if (urlcollection != "")
            {
                //Added v1.3.0
                ini["URL"]["Collection"] = urlcollection;

                //Added v1.3.0
                ShowMessage("VERIFY: Chosen collection URL saved");
            }

            //Changed v1.3.0, now uses INI
            //Check to ensure the SteamCMDDir content is indeed provided (length greater than 0 indicates data in the field)
            //Make sure the variable in the settings file is correct
            if (steamcmd != "")
            {
                //Added v1.3.0
                ini["Directories"]["SteamCMD"] = steamcmd;

                //Added v1.3.0
                ShowMessage("VERIFY: Chosen SteamCMD directory saved");
            }

            //Changed v1.3.0, now uses INI
            //Check to ensure the ModDir content is indeed provided (length greater than 0 indicates data in the field)
            //Make sure the variable in the settings file is correct
            if (mods != "")
            {
                //Added v1.3.0
                ini["Directories"]["Mods"] = mods;

                //Added v1.3.0
                ShowMessage("VERIFY: Chosen mods directory saved");
            }

            //Changed v1.3.0, now uses INI
            //Save which platform the user has chosen
            if (steam)
            {
                //Added v1.3.0
                ini["Misc"]["Method"] = "steam";

                //Added v1.3.0
                ShowMessage("VERIFY: Chosen method (Steam collection) saved");
            }
            //Added v1.3.0
            else
            {
                //Added v1.3.0
                ini["Misc"]["Method"] = "other";

                //Added v1.3.0
                ShowMessage("VERIFY: Chosen method (list of links) saved");
            }

            //Added v1.3.0
            if (mode == "download")
            {
                //Added v1.3.0
                ini["Misc"]["Mode"] = "update";

                //Added v1.3.0
                ShowMessage("VERIFY: Mods have been downloaded, Conexus will next start in update mode");
            }

            if (mods != "" && Directory.GetDirectories(mods).Length > 0)
            {
                //Added v1.3.0
                ini["Misc"]["Mode"] = "update";

                //Added v1.3.0
                ShowMessage("VERIFY: Mods have been downloaded, Conexus will next start in update mode");
            }
            //Otherwise if there is no mods path, let's just assume they need to download
            else if (mods == "")
            {
                //Added v1.3.0
                ini["Misc"]["Mode"] = "download";

                //Added v1.3.0
                ShowMessage("VERIFY: Mods have not been downloaded, Conexus will next start in download mode");
            }


            //Added v1.3.0
            ShowMessage("PROC: All user data has been saved!");

            //Added v1.3.0
            ShowMessage("INFO: Conexus will close now");

            ini.Persist();

            //Added v1.3.0
            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));
        }

        //Changed v1.3.0, added logging
        //Final call that happens right after the window starts to close
        //Used to save all relevant data to the settings file
        void Window_Closed(object sender, EventArgs e)
        {
            //Save all data to the settings file
            //UserSettings.Default.Save();

            //Added v1.2.0
            //Ensure the Logs folder exists
            if (!Directory.Exists(logsPath))
            {
                //Added v1.3.0
                ShowMessage("WARN: _Logs folder is missing! Creating now");

                Directory.CreateDirectory(logsPath);
            }

            //Save log to file
            WriteToFile(log.ToArray(), Path.Combine(logsPath, dateTime + ".txt"));

            //Added v1.3.0
            ini.Persist();
        }

        #endregion
    }
}