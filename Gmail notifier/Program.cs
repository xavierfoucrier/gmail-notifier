﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using notifier.Languages;
using notifier.Properties;

namespace notifier {
	static class Program {

		/// <summary>
		/// Mutex associated to the application instance
		/// </summary>
		static Mutex mutex = new Mutex(true, "gmailnotifier-115e363ecbfefd771e55c6874680bc0a");

		[STAThread]
		static void Main() {

			// initializes the interface with the specified culture, depending on the user settings
			System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Default.Language == "Français" ? "fr-FR" : "en-US");

			// check if there is an instance running
			if (!mutex.WaitOne(TimeSpan.Zero, true)) {
				MessageBox.Show(translation.mutexError, translation.multipleInstances, MessageBoxButtons.OK, MessageBoxIcon.Warning);

				return;
			}

			// sets some default properties
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// run the main window
			Application.Run(new Main());

			mutex.ReleaseMutex();
		}
	}
}