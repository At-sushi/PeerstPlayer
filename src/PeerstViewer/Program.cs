﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace PeerstViewer
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			Application.Run(new ThreadViewer());
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				Exception ex = (Exception)e.ExceptionObject;
				using (StreamWriter writer = new StreamWriter("Error:Viewer.txt", true))
				{
					writer.WriteLine("BEGIN-------------------------------------------");
					writer.WriteLine(string.Format("UnhandledExecption {0}", DateTime.Now.ToString()));
					writer.WriteLine(ex.StackTrace);
					writer.WriteLine("------------------------------------------------");
					writer.WriteLine(ex.ToString());
					writer.WriteLine("END---------------------------------------------");
				}
				// MessageBox.Show("エラー", "例外", MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1);
			}
			catch (Exception)
			{
			}
		}
	}
}