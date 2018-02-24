﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;
using HtmlAgilityPack;
using notifier.Languages;
using notifier.Properties;

namespace notifier {
	class Update : Main {

		// http client used to check for updates
		private HttpClient http = new HttpClient();

		// flag defining the update state
		private bool updating = false;

		/// <summary>
		/// Class constructor
		/// </summary>
		public Update() {
		}

		/// <summary>
		/// Asynchronous method to connect to the repository and check if there is an update available
		/// </summary>
		/// <param name="verbose">Indicates if the process displays a message when a new update package is available</param>
		/// <param name="startup">Indicates if the update check process has been started at startup</param>
		public async void AsyncCheckForUpdate(bool verbose = true, bool startup = false) {
			try {

				// gets the list of tags in the Github repository tags webpage
				HttpResponseMessage response = await http.GetAsync(GITHUB_REPOSITORY + "/tags");

				var document = new HtmlAgilityPack.HtmlDocument();
				document.LoadHtml(await response.Content.ReadAsStringAsync());

				HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//span[@class='tag-name']");

				if (collection == null || collection.Count == 0) {
					return;
				}

				List<string> tags = collection.Select(p => p.InnerText).ToList();
				string release = tags.First();

				// the current version tag is not at the top of the list
				if (release != version) {

					// downloads the update package automatically or asks the user, depending on the user setting and verbosity
					if (verbose) {
						DialogResult dialog = MessageBox.Show(translation.newVersion.Replace("{version}", tags[0]), "Gmail Notifier Update", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);

						if (dialog == DialogResult.Yes) {
							DownloadUpdate(release);
						}
					} else if (Settings.Default.UpdateDownload) {
						DownloadUpdate(release);
					}
				} else if (verbose && !startup) {
					MessageBox.Show(translation.latestVersion, "Gmail Notifier Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			} catch (Exception) {

				// indicates to the user that the update service is not reachable for the moment
				if (verbose) {
					MessageBox.Show(translation.updateServiceUnreachable, "Gmail Notifier Update", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			} finally {

				// restores default check icon and check for update button state
				linkCheckForUpdate.Enabled = true;
				linkCheckForUpdate.Image = Resources.update_check;
				buttonCheckForUpdate.Enabled = true;

				// stores the latest update datetime control
				Settings.Default.UpdateControl = DateTime.Now;

				// updates the update control label
				labelUpdateControl.Text = DateTime.Now.ToString();

				// synchronizes the inbox if the updates has been checked at startup after asynchronous authentication
				if (startup) {
					AsyncSyncInbox();
				}
			}
		}

		/// <summary>
		/// Downloads and launch the setup installer
		/// </summary>
		/// <param name="release">Version number package to download</param>
		private void DownloadUpdate(string release) {

			// defines that the application is currently updating
			updating = true;

			// defines the new number version and temp path
			string newversion = release.Split('-')[0].Substring(1);
			string updatepath = GetAppData() + "/gmnupdate-" + newversion + ".exe";
			string package = GITHUB_REPOSITORY + "/releases/download/" + release + "/Gmail.Notifier." + newversion + ".exe";

			try {

				// disables the context menu and displays the update icon in the systray
				notifyIcon.ContextMenu = null;
				notifyIcon.Icon = Resources.updating;
				notifyIcon.Text = translation.updating;

				// creates a new web client instance
				WebClient client = new WebClient();

				// displays the download progression on the systray icon, and prevents the application from restoring the context menu and systray icon at startup
				client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((object o, DownloadProgressChangedEventArgs target) => {
					notifyIcon.ContextMenu = null;
					notifyIcon.Icon = Resources.updating;
					notifyIcon.Text = translation.updating + " " + target.ProgressPercentage.ToString() + "%";
				});

				// starts the setup installer when the download has complete and exits the current application
				client.DownloadFileCompleted += new AsyncCompletedEventHandler((object o, AsyncCompletedEventArgs target) => {
					Process.Start(new ProcessStartInfo(updatepath, Settings.Default.UpdateQuiet ? "/verysilent" : ""));
					Application.Exit();
				});

				// ensures that the Github package URI is callable
				client.OpenRead(package).Close();

				// starts the download of the new version from the Github repository
				client.DownloadFileAsync(new Uri(package), updatepath);
			} catch (Exception) {

				// indicates to the user that the update service is not reachable for the moment
				MessageBox.Show(translation.updateServiceUnreachable, "Gmail Notifier Update", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

				// defines that the application has exited the updating state
				updating = false;

				// restores the context menu to the systray icon and start a synchronization
				notifyIcon.ContextMenu = contextMenu;
				AsyncSyncInbox();
			}
		}

		/// <summary>
		/// Cleans all temporary update files present in the use local application data folder
		/// </summary>
		public void CleanTemporaryUpdateFiles() {
			if (!Directory.Exists(GetAppData())) {
				return;
			}

			IEnumerable<string> executables = Directory.EnumerateFiles(GetAppData(), "*.exe", SearchOption.TopDirectoryOnly);

			foreach (string executable in executables) {
				try {
					File.Delete(executable);
				} catch (Exception) {
					// nothing to catch: executable is currently locked
					// setup package will be removed next time
				}
			}
		}

		/// <summary>
		/// Indicates if the update service is currently updating the application
		/// </summary>
		public bool IsUpdating() {
			return updating;
		}
	}
}