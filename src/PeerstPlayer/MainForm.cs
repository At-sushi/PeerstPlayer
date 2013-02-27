﻿
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Shule.Peerst.Util;
using Shule.Peerst.BBS;

namespace PeerstPlayer
{
	public partial class MainForm : Form
	{
		/// <summary>
		/// 掲示板操作クラス
		/// </summary>
		OperationBbs operationBbs = null;

		/// <summary>
		/// チャンネル情報
		/// </summary>
		public ChannelInfo ChannelInfo;

		/// <summary>
		/// WMP
		/// </summary>
		WMPEx wmp;

		/// <summary>
		/// スレッドの一覧
		/// </summary>
		List<ThreadInfo> ThreadList = new List<ThreadInfo>();

		/// <summary>
		/// スレ番号
		/// </summary>
		string ThreadNo = "";

		/// <summary>
		/// チャンネル詳細
		/// </summary>
		string ChannelDetail = "";

		/// <summary>
		/// ブラウザアドレス
		/// </summary>
		string BrowserAddress = "";

		/// <summary>
		/// スレッドブラウザアドレス
		/// </summary>
		string ThreadBrowserAddress = "";

		/// <summary>
		/// 前回書き込んだスレ番号
		/// </summary>
		string BeforeWriteThreadNo = "";

		/// <summary>
		/// レスボックスの操作
		/// true（Enter：改行 / Shift+Enter：書き込み）
		/// false（Enter：書き込み / Shift+Enter：改行）
		/// </summary>
		bool ResBoxType = true;

		/// <summary>
		/// レスボックスを自動的に隠すか
		/// </summary>
		bool ResBoxAutoVisible = false;

		/// <summary>
		/// 終了時にリレーを切断する
		/// </summary>
		public bool RlayCutOnClose = true;

		/// <summary>
		/// XPであるか
		/// </summary>
		public bool WindowsXP = true;

		/// <summary>
		/// クリックした時レスボックスを閉じるか
		/// </summary>
		bool ClickToResBoxClose = true;

		/// <summary>
		/// 終了時に位置を保存するか
		/// </summary>
		bool SaveLocationOnClose = false;

		/// <summary>
		/// 終了時にサイズを保存するか
		/// </summary>
		bool SaveSizeOnClose = false;

		/// <summary>
		/// アスペクト比を維持
		/// </summary>
		bool AspectRate = true;

		/// <summary>
		/// 書き込み時にレスボックスを閉じる
		/// </summary>
		bool CloseResBoxOnWrite = true;

		/// <summary>
		/// バックスペースでレスボックスを閉じるか
		/// </summary>
		bool CloseResBoxOnBackSpace = false;

		/// <summary>
		/// 最小化ミュート時
		/// </summary>
		bool MiniMute = false;

		/// <summary>
		/// デフォルト拡大率
		/// </summary>
		int DefaultScale = -1;

		/// <summary>
		/// 起動時に動画サイズを合わせる
		/// </summary>
		bool FitSizeMovie = true;

		/// <summary>
		/// スクリーン吸着距離
		/// </summary>
		int ScreenMagnetDockDist = 20;

		/// <summary>
		/// マウスジェスチャーを使うか
		/// </summary>
		public bool UseMouseGesture = true;

		/// <summary>
		/// 終了時に一緒にビューワも終了するか
		/// </summary>
		bool CloseViewerOnClose = true;

		/// <summary>
		/// 終了時のボリュームを保存するか
		/// </summary>
		bool SaveVolumeOnClose = false;

		/// <summary>
		/// スクリーン吸着をするか
		/// </summary>
		bool UseScreenMagnet = true;

		// スレッドビューワのプロセス
		Process ThreadViewerProcess = null;

		/// <summary>
		/// ショートカットリスト
		/// </summary>
		List<string[]> ShortcutList = new List<string[]>();

		/// <summary>
		/// WMPパネルの大きさ
		/// </summary>
		public Size PanelWMPSize = new Size(320, 240);

		/// <summary>
		/// コマンド表示カウント
		/// </summary>
		int CommandShowCount = 0;

		/// <summary>
		/// 初回のスレ選択をしたか
		/// </summary>
		bool InitThreadSelected = false;

		/// <summary>
		/// ツールチップが表示されているか
		/// </summary>
		public bool ToolStipVisile
		{
			get
			{
				if (toolStripDropDownButtonSize.Pressed || toolStripDropDownButtonDivide.Pressed)
				{
					return true;
				}

				return false;
			}
		}

		#region WMPパネルのサイズ

		/// <summary>
		/// WMPパネルのサイズ
		/// </summary>
		public Size WMPSize
		{
			get
			{
				return panelWMP.Size;
			}
		}

		#endregion

		#region スクリーンサイズ

		/// <summary>
		/// スクリーンサイズ
		/// </summary>
		Rectangle Screen
		{
			get
			{
				return System.Windows.Forms.Screen.GetWorkingArea(this);
			}
		}
	
		#endregion

		#region タイトルバー

		private enum SWP : int
		{
			NOSIZE = 0x0001,
			NOMOVE = 0x0002,
			NOZORDER = 0x0004,
			NOREDRAW = 0x0008,
			NOACTIVATE = 0x0010,
			FRAMECHANGED = 0x0020,
			SHOWWINDOW = 0x0040,
			HIDEWINDOW = 0x0080,
			NOCOPYBITS = 0x0100,
			NOOWNERZORDER = 0x0200,
			NOSENDCHANGING = 0x400
		}

		private const UInt32 WS_CAPTION = (UInt32)0x00C00000;
		private const int GWL_STYLE = -16;

		[DllImport("user32.dll")]
		private static extern UInt32 GetWindowLong(IntPtr hWnd, int index);
		[DllImport("user32.dll")]
		private static extern UInt32 SetWindowLong(IntPtr hWnd, int index, UInt32 unValue);
		[DllImport("user32.dll")]
		private static extern UInt32 SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, SWP flags);

		bool titleBar = false;
		public bool TitleBar
		{
			get
			{
				return titleBar;
			}
			set
			{
				titleBar = value;

				// アンカーを解除
				PanelAnchor = false;

				if (value)
				{
					// タイトルバーを出す
					UInt32 style = GetWindowLong(this.Handle, GWL_STYLE);	// 現在のスタイルを取得
					style = (style | WS_CAPTION);							// キャプションのスタイルを削除
					SetWindowLong(Handle, GWL_STYLE, style);				// スタイルを反映
					SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE | SWP.NOZORDER | SWP.FRAMECHANGED); // ウィンドウを再描画
				}
				else
				{
					// タイトルバーを消す
					UInt32 style = GetWindowLong(this.Handle, GWL_STYLE);	// 現在のスタイルを取得
					style = (style & ~WS_CAPTION);							// キャプションのスタイルを削除
					SetWindowLong(Handle, GWL_STYLE, style);				// スタイルを反映
					SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE | SWP.NOZORDER | SWP.FRAMECHANGED); // ウィンドウを再描画
				}

				OnPanelSizeChange();
			}
		}

		#endregion

		#region フレーム

		/// <summary>
		/// フレーム
		/// </summary>
		bool isBeforeTitleBar = true;
		public bool Frame
		{
			get
			{
				return (FormBorderStyle == FormBorderStyle.Sizable);
			}
			set
			{
				if (value)
				{
					FormBorderStyle = FormBorderStyle.Sizable;
					TitleBar = isBeforeTitleBar;
				}
				else
				{
					/*
					isBeforeTitleBar = TitleBar;
					TitleBar = true;
					 */
					TitleBar = false;
					FormBorderStyle = FormBorderStyle.None;
				}
			}
		}

		#endregion

		// コンストラクタ
		public MainForm()
		{
			// 初期化
			InitializeComponent();

			ChannelInfo = new ChannelInfo();

			// WMP初期化
			wmp = new WMPEx(this);
			wmp.MouseDownEvent += new AxWMPLib._WMPOCXEvents_MouseDownEventHandler(wmp_MouseDownEvent);
			wmp.MouseUpEvent += new AxWMPLib._WMPOCXEvents_MouseUpEventHandler(wmp_MouseUpEvent);
			wmp.MouseMoveEvent += new AxWMPLib._WMPOCXEvents_MouseMoveEventHandler(wmp_MouseMoveEvent);
			wmp.MovieStart += new EventHandler(wmp_MovieStart);
			wmp.VolumeChange += new EventHandler(wmp_VolumeChange);
			wmp.Gesture += new EventHandlerString(wmp_Gesture);
			wmp.DurationChange += new EventHandler(wmp_DurationChange);

			// タイトルバー
			TitleBar = false;
			panelResBox.Visible = false;
			WindowsXP = IsWindowsXP();
		}

		/// <summary>
		/// コマンドから文字に変換する
		/// </summary>
		string CommandToString(string command)
		{
			switch (command)
			{
				case "Volume+1":
					return "ボリューム：+1";
				case "Volume-1":
					return "ボリューム：-1";
				case "Volume+5":
					return "ボリューム：+5";
				case "Volume-5":
					return "ボリューム：-5";
				case "Volume+10":
					return "ボリューム：+10";
				case "Volume-10":
					return "ボリューム：-10";
				case "balance-10":
					return "音量バランス：-10";
				case "balance+10":
					return "音量バランス：+10";
				case "Size+10%":
					return "サイズ：+10%";
				case "Size-10%":
					return "サイズ：-10%";
				case "Size+10":
					return "サイズ：+10";
				case "Size-10":
					return "サイズ：-10";
				case "Bump":
					return "再接続する";
				case "Close&RelayCut":
					return "リレーを切断して終了する";
				case "OpenThreadViewer":
					return "スレッドビューワを開く";
				case "ThreadListUpdate":
					return "スレッド一覧を更新する";
				case "Close":
					return "終了する";
				case "AspectRate":
					return "アスペクト比固定切り替え";
				case "TopMost":
					return "最前列表示切り替え";
				case "ResBox":
					return "レスボックスの表示切り替え";
				case "StatusLabel":
					return "ステータスラベルの表示切り替え";
				case "Frame":
					return "フレームの表示切り替え";
				case "TitleBar":
					return "タイトルバーの表示切り替え";
				case "Size=50%":
					return "サイズ：50%";
				case "Size=75%":
					return "サイズ：75%";
				case "Size=100%":
					return "サイズ：100%";
				case "Size=150%":
					return "サイズ：150%";
				case "Size=200%":
					return "サイズ：200%";
				case "Width=160":
					return "サイズ：幅160";
				case "Width=320":
					return "サイズ：幅320";
				case "Width=480":
					return "サイズ：幅480";
				case "Width=640":
					return "サイズ：幅640";
				case "Width=800":
					return "サイズ：幅800";
				case "Height=120":
					return "サイズ：高さ120";
				case "Height=240":
					return "サイズ：高さ240";
				case "Height=360":
					return "サイズ：高さ360";
				case "Height=480":
					return "サイズ：高さ480";
				case "Height=600":
					return "サイズ：高さ600";
				case "ScreenSplitWidth=5":
					return "サイズ：幅5分の1";
				case "ScreenSplitWidth=4":
					return "サイズ：幅4分の1";
				case "ScreenSplitWidth=3":
					return "サイズ：幅3分の1";
				case "ScreenSplitWidth=2":
					return "サイズ：幅2分の1";
				case "ScreenSplitHeight=5":
					return "サイズ：高さ5分の1";
				case "ScreenSplitHeight=4":
					return "サイズ：高さ4分の1";
				case "ScreenSplitHeight=3":
					return "サイズ：高さ3分の1";
				case "ScreenSplitHeight=2":
					return "サイズ：高さ2分の1";
				case "ScreenSplit=5":
					return "サイズ：5 x 5";
				case "ScreenSplit=4":
					return "サイズ：4 x 4";
				case "ScreenSplit=3":
					return "サイズ：3 x 3";
				case "ScreenSplit=2":
					return "サイズ：2 x 2";
				case "VolumeBalance-10":
					return "音量バランス：-10";
				case "VolumeBalance+10":
					return "音量バランス：+10";
				case "VolumeBalanceLeft":
					return "音量バランス：左";
				case "VolumeBalanceRight":
					return "音量バランス：右";
				case "VolumeBalanceMiddle":
					return "音量バランス：中央";
				case "Mute":
					return "音量：ミュート切り替え";
				case "FullScreen":
					return "サイズ：フルスクリーン切り替え";
				case "ChannelInfoUpdate":
					return "チャンネル情報を更新";
				case "OpenContextMenu":
					return "コンテキストメニューを表示";
				case "SelectResBox":
					return "レスボックスを選択";
				case "Mini&Mute":
					return "最小化ミュート";
				case "ScreenShot":
					return "スクリーンショット";
				case "OpenScreenShotFolder":
					return "スクリーンショットフォルダを開く";
				case "FitSizeMovie":
					return "動画サイズに合わせる";
				case "Retry":
					return "リトライ";
				case "OpenClipBoard":
					return "クリップボードから開く";
				case "OpenFile":
					return "ファイルを開く";
				case "WMPFullScreen":
					return "WMPフルスクリーン";
				default:
					return command;
			}
		}

		/// <summary>
		/// コマンドを実行
		/// </summary>
		public void ExeCommand(string command)
		{
			string value = "";
			switch (command)
			{
				case "Size=50%":
					SetScale(50);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=75%":
					SetScale(75);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=100%":
					SetScale(100);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=150%":
					SetScale(150);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size=200%":
					SetScale(200);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=160":
					SetWidth(160);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=320":
					SetWidth(320);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=480":
					SetWidth(480);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=640":
					SetWidth(640);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Width=800":
					SetWidth(800);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=120":
					SetWidth(120);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=240":
					SetWidth(240);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=360":
					SetWidth(360);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=480":
					SetWidth(480);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Height=600":
					SetWidth(600);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=5":
					ScreenSplitWidth(5);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=4":
					ScreenSplitWidth(4);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=3":
					ScreenSplitWidth(3);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitWidth=2":
					ScreenSplitWidth(2);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=5":
					ScreenSplitHeight(5);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=4":
					ScreenSplitHeight(4);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=3":
					ScreenSplitHeight(3);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplitHeight=2":
					ScreenSplitHeight(2);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=5":
					ScreenSplit(5);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=4":
					ScreenSplit(4);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=3":
					ScreenSplit(3);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "ScreenSplit=2":
					ScreenSplit(2);
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Volume+1":
					wmp.Volume += 1;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume-1":
					wmp.Volume -= 1;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume+5":
					wmp.Volume += 5;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume-5":
					wmp.Volume -= 5;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume+10":
					wmp.Volume += 10;
					value = " (" + wmp.Volume + ")";
					break;
				case "Volume-10":
					wmp.Volume -= 10;
					value = " (" + wmp.Volume + ")";
					break;
				case "Bump":
					wmp.Bump();
					break;
				case "Close&RelayCut":
					try
					{
						wmp.RelayCut();
						Visible = false;
						Close();
						//Application.Exit();
					}
					catch
					{
					}
					break;
				case "VolumeBalance-10":
					wmp.settings.balance -= 10;
					value = " (" + wmp.settings.balance + ")";
					break;
				case "VolumeBalance+10":
					wmp.settings.balance += 10;
					value = " (" + wmp.settings.balance + ")";
					break;
				case "VolumeBalanceLeft":
					wmp.settings.balance = -100;
					break;
				case "VolumeBalanceRight":
					wmp.settings.balance = 100;
					break;
				case "VolumeBalanceMiddle":
					wmp.settings.balance = 0;
					break;
				case "Size+10%":
					{
						double SizePercent = ((int)(wmp.Width / (float)wmp.ImageWidth * 10 + 1 + 0.6)) / 10.0f;
						panelWMP.Size = new Size((int)(wmp.ImageWidth * SizePercent), (int)(wmp.ImageHeight * SizePercent));
						OnPanelSizeChange();
						value = " " + ((int)(SizePercent * 100)).ToString() + "% (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					}
					break;
				case "Size-10%":
					{
						double SizePercent = ((int)(wmp.Width / (float)wmp.ImageWidth * 10 - 1 + 0.6)) / 10.0f;
						panelWMP.Size = new Size((int)(wmp.ImageWidth * SizePercent), (int)(wmp.ImageHeight * SizePercent));
						OnPanelSizeChange();
						value = " " + ((int)(SizePercent * 100)).ToString() + "% (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					}
					break;
				case "Size+10":
					panelWMP.Width += 10;
					panelWMP.Height += 10;
					OnPanelSizeChange();
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "Size-10":
					panelWMP.Width -= 10;
					panelWMP.Height -= 10;
					OnPanelSizeChange();
					value = " (" + panelWMP.Width + " x " + panelWMP.Height + ")";
					break;
				case "TopMost":
					TopMost = !TopMost;
					if (TopMost)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "ResBox":
					panelResBox.Visible = !panelResBox.Visible;
					OnPanelSizeChange();
					if (panelResBox.Visible)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "StatusLabel":
					panelStatusLabel.Visible = !panelStatusLabel.Visible;
					OnPanelSizeChange();
					if (panelStatusLabel.Visible)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "Frame":
					Frame = !Frame;
					OnPanelSizeChange();
					if (Frame)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "TitleBar":
					TitleBar = !TitleBar;
					OnPanelSizeChange();
					if (TitleBar)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "Mute":
					wmp.Mute = !wmp.Mute;
					if (wmp.Mute)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "WMPFullScreen":
					try
					{
						wmp.fullScreen = true;
					}
					catch
					{
					}
					break;

				case "FullScreen":
					if (WindowState == FormWindowState.Maximized)
					{
						WindowState = FormWindowState.Normal;
						OnPanelSizeChange(PanelWMPSize);
						value = " (無効)";
					}
					else
					{
						PanelWMPSize = WMPSize;
						WindowState = FormWindowState.Maximized;
						value = " (有効)";
					}
					OnPanelSizeChange();
					break;
				case "OpenThreadViewer":
					スレッドビューワを開くToolStripMenuItem_Click(this, EventArgs.Empty);
					break;
				case "AspectRate":
					AspectRate = !AspectRate;
					if (AspectRate)
					{
						value = " (有効)";
					}
					else
					{
						value = " (無効)";
					}
					break;
				case "ThreadListUpdate":
					ThreadListUpdate();
					break;
				case "ChannelInfoUpdate":
					ChannelInfoUpdate();
					break;
				case "Close":
					try
					{
						Visible = false;
						Close();
					}
					catch
					{
					}
					break;
				case "OpenContextMenu":
					CommandShowCount = 10;
					labelDetail.Text = CommandToString(command) + value;
					wmp.enableContextMenu = true;
					Win32API.SendMessage(wmp.Handle, Win32API.WM_CONTEXTMENU, new IntPtr(MousePosition.X), new IntPtr(MousePosition.Y));
					wmp.enableContextMenu = false;
					break;
				case "SelectResBox":
					if (!panelResBox.Visible)
					{
						panelResBox.Visible = true;
						OnPanelSizeChange();
					}
					resBox.Selected = true;
					break;
				case "Mini&Mute":
					WindowState = FormWindowState.Minimized;
					wmp.Mute = true;
					MiniMute = true;
					break;
				case "ScreenShot":
					ScreenShot();
					break;
				case "OpenScreenShotFolder":
					OpenScreenShotFolder();
					break;

				// 動画サイズを合わせる
				case "FitSizeMovie":
					try
					{
						if ((wmp.Height / (float)wmp.Width) > wmp.AspectRate)
						{
							SetWidth(wmp.Width);
						}
						else
						{
							SetHeight(wmp.Height);
						}
					}
					catch
					{
					}
					break;

				// リトライ
				case "Retry":
					wmp.Retry(true);
					break;

				// クリップボードから開く
				case "OpenClipBoard":
					//クリップボードの文字列データを取得する
					IDataObject iData = Clipboard.GetDataObject();
					//クリップボードに文字列データがあるか確認
					if (iData.GetDataPresent(DataFormats.Text))
					{
						//文字列データがあるときはこれを取得する
						wmp.URL = (string)iData.GetData(DataFormats.Text);
					}
					break;

				// ファイルを開く
				case "OpenFile":
					OpenFileDialog ofd = new OpenFileDialog();
					ofd.ShowDialog();
					wmp.URL = ofd.FileName;
					break;

				/*
			case "Shift+Up":
				panelWMP.Height -= 1;
				OnPanelSizeChange();
				break;
			case "Shift+Down":
				panelWMP.Height += 1;
				OnPanelSizeChange();
				break;
			case "Shift+Right":
				panelWMP.Width += 1;
				OnPanelSizeChange();
				break;
			case "Shift+Left":
				panelWMP.Width -= 1;
				OnPanelSizeChange();
				break;
			case "Alt+Up":
				panelWMP.Height -= 10;
				OnPanelSizeChange();
				break;
			case "Alt+Down":
				panelWMP.Height += 10;
				OnPanelSizeChange();
				break;
			case "Alt+Right":
				panelWMP.Width += 10;
				OnPanelSizeChange();
				break;
			case "Alt+Left":
				panelWMP.Width -= 10;
				OnPanelSizeChange();
				break;
			case "Up":
				panelWMP.Height -= 30;
				OnPanelSizeChange();
				break;
			case "Down":
				panelWMP.Height += 30;
				OnPanelSizeChange();
				break;
			case "Right":
				panelWMP.Width += 30;
				OnPanelSizeChange();
				break;
			case "Left":
				panelWMP.Width -= 30;
				OnPanelSizeChange();
				break;
				 */
				default:
					return;
			}

			if (command != "OpenContextMenu")
			{
				CommandShowCount = 10;
				labelDetail.Text = CommandToString(command) + value;
			}
		}

		/// <summary>
		/// スクリーンショットフォルダを開く
		/// </summary>
		private void OpenScreenShotFolder()
		{
			// フォルダパス
			string folderPath = GetCurrentDirectory() + "\\ScreenShot";

			// フォルダがなければ作る
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			// フォルダを開く
			System.Diagnostics.Process.Start(folderPath);
		}

		/// <summary>
		/// スクリーンショットを撮る
		/// </summary>
		private void ScreenShot()
		{
			// ウィンドウレスモード有効
			wmp.windowlessVideo = true;

			// 現在の時間
			DateTime dtNow = DateTime.Now;

			// フォルダパス
			string folderPath = GetCurrentDirectory() + "\\ScreenShot";
			// ファイルパス
			string fileName = "【" + ChannelInfo.Name + "】" + dtNow.Year + "／" + dtNow.Month.ToString().PadLeft(2, '0') + "／" + dtNow.Day.ToString().PadLeft(2, '0') + "（" + dtNow.Hour.ToString().PadLeft(2, '0') + "：" + dtNow.Minute.ToString().PadLeft(2, '0') + "：" + dtNow.Second.ToString().PadLeft(2, '0') + "." + dtNow.Millisecond.ToString().PadLeft(3, '0') + "）.jpg";
			fileName = fileName.Replace('\\', ' ').Replace('/', ' ').Replace(':', ' ').Replace('*', ' ').Replace('?', ' ').Replace('"', ' ').Replace('<', ' ').Replace('>', ' ').Replace('|', ' ');
			string filePath = folderPath + "\\" + fileName;

			// フォルダがなければ作る
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			// 表示してあるものを消す
			contextMenuStripResBox.Visible = false;
			toolStrip.Visible = false;

			// 再描画
			wmp.Refresh();

			{
				// スクリーン・キャプチャの範囲を設定
				Rectangle myRectangle = wmp.RectangleToScreen(wmp.Bounds);

				// Bitmapオブジェクトにスクリーン・キャプチャ
				Bitmap myBmp = new Bitmap(myRectangle.Width, myRectangle.Height, PixelFormat.Format32bppArgb);

				using (Graphics g = Graphics.FromImage(myBmp))
				{
					g.CopyFromScreen(myRectangle.X, myRectangle.Y, 0, 0,
					myRectangle.Size, CopyPixelOperation.SourceCopy);
				}

				// クリップボードにコピー
				Clipboard.SetDataObject(myBmp, false);

				// クリップボードに格納された画像の取得
				IDataObject data = Clipboard.GetDataObject();
				if (data.GetDataPresent(DataFormats.Bitmap))
				{
					Bitmap bmp = (Bitmap)data.GetData(DataFormats.Bitmap);
					// 取得した画像の保存
					bmp.Save(filePath, ImageFormat.Jpeg);
				}
			}

			// ウィンドウレスモード無効
			wmp.windowlessVideo = false;
		}

		/// <summary>
		/// WindowsXPであるか
		/// </summary>
		private bool IsWindowsXP()
		{
			System.OperatingSystem os = System.Environment.OSVersion;

			if (os.Platform == PlatformID.Win32NT)
			{
				if (os.Version.Major == 6)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// コマンドラインからチャンネル名を取得
		/// </summary>
		private string GetChannelName()
		{
			if (Environment.GetCommandLineArgs().Length > 2)
			{
				return Environment.GetCommandLineArgs()[2];
			}

			return "";
		}

		/// <summary>
		/// コンテキストメニューを表示するか
		/// </summary>
		public bool IsOpenContextMenu = true;

		/// <summary>
		/// 初期化ファイル設定
		/// </summary>
		private void LoadInitFile()
		{
			string iniFileName = GetCurrentDirectory() + "\\PeerstPlayer.ini";
			IniFile iniFile = new IniFile(iniFileName);

			if (!System.IO.File.Exists(iniFileName))
			{

				string str = @"/**************************************************************************/
/* ここからは、Ｐｌａｙｅｒの設定です
/* 全ての値は、デフォルト値となります
/**************************************************************************/
[Player]

// スレッド内容を字幕表示するか
// True ： 表示
// False ： 表示しない
ThreadCaption =True

// タイトルバー
// True ： 表示
// False ： 表示しない
TitleBar =False

// レスボックスの表示
// True ： 表示
// False ： 表示しない
ResBox =False

// ステータスバーの表示
// True ： 表示
// False ： 表示しない
StatusLabel =True

// フレームの表示
// True ： 表示
// False ： 表示しない
Frame =True

// アスペクト比を維持
// True : 維持する
// False : 維持しない
AspectRate =True

// 最前列表示
// True ： 最前列表示 ON
// False ： 最前列表示 OFF
TopMost =False

// ボリュームの値
Volume =50

// レスボックスの操作
// True（Enter：改行 / Shift+Enter：書き込み）
// False（Enter：書き込み / Shift+Enter：改行）
ResBoxType =True

// レスボックスを自動に隠すか
// True ： 自動に隠れる
// False ： ステータスバークリックで表示
ResBoxAutoVisible =False

// レスボックスの表示 / 非表示
// （スーテタスラベルをクリックした時の動作）
// True ： レスボックスを閉じる
// False ： レスボックスを閉じない
ClickToResBoxClose =False

// 終了時にリレーを切断するか
// True　：　終了時にリレー切断
// False ： 終了時にリレー切断しない
RlayCutOnClose =Flase

// 終了時に位置を保存
// True ： 保存する
// False ： 保存しない
SaveLocationOnClose =False

// 起動時の位置（X, Y）
// 指定しない場合は空白「X =」「Y =」
X =
Y =

// 起動時のサイズ（Width, Height）
// 指定しない場合は空白「Width =」「Height =」
Width =
Height =

// 起動時の拡大率
Scale =100

// 起動時に動画サイズを合わせる
FitSizeMovie =True

// 終了時にサイズを保存
// True ： 保存する
// False ： 保存しない
SaveSizeOnClose =False

// 終了時に音量を保存
// True ： 保存する
// False ： 保存しない
SaveVolumeOnClose =False

// ステータスラベルのフォント指定
FontName =MS UI Gothic

// フォントの大きさ(pt)
FontSize =9

// フォントの色(RGB)
FontColorR =0
FontColorG =255
FontColorB =128

// 書き込み時にレスボックスを閉じる
// True ： 閉じる
// False ： 閉じない
CloseResBoxOnWrite =False

// バックスペースでレスボックスを閉じる
CloseResBoxOnBackSpace =False

// マウスジェスチャーを使うか
// falseにすると、ショートカットにマウスジェスチャーが指定されていても反応しません
// True ： 使う
// False ： 使わない
UseMouseGesture =True

// スクリーン吸着をするか
// True ： する
// False ： しない
UseScreenMagnet =True

// プレイヤーを閉じたときにビューワも閉じるか
// True ： 閉じる
// False ： 閉じない
CloseViewerOnClose=True

// スクリーン吸着の感度（指定したドット範囲内だと吸着します）
ScreenMagnetDockDist=20

// マウスジェスチャー感度
MouseGestureInterval =10

/// ブラウザアドレス(exeの場所を入力してください)
BrowserAddress =

// スレッドブラウザアドレス(exeの場所を入力してください)
ThreadBrowserAddress =

/**************************************************************************/
/* ここからは、Viewerの設定です
/**************************************************************************/
[Viewer]
// 起動時の位置（X, Y）
// 指定しない場合は空白「X =」「Y =」
X =
Y =

// 起動時のサイズ（Width, Height）
// 指定しない場合は空白「Width =」「Height =」
Width =
Height =

// 最前列表示にするか
// True : 最前列表示
// False : 最前列表示にしない
TopMost =False

// 自動更新をするか
// True : 自動更新をする
// False : 自動行進をしない
AutoReload =True

// 自動更新時間（秒）（7、10、15、20、25、30）
AutoReloadInterval =7

// 書き込み欄を表示するか
// True : 表示
// False : 非表示
WriteView =False

// フォント指定
// AAがずれないフォント：<body style=""font-family:'ＭＳ Ｐゴシック','ＭＳＰゴシック','MSPゴシック','MS Pゴシック';font-size:16px;line-height:18px;"" >
// htmlのbodyタグを埋め込んでフォントを変えています。 いろいろいじってみてね！
// FontName = <body bgcolor=""背景色"" style=""font-family:'フォント名';font-size:サイズpx;line-height:高さpx;"" >

FontName =<body bgcolor=""#E6EEF3"" style=""font-family:'※※※','ＭＳ Ｐゴシック','ＭＳＰゴシック','MSPゴシック','MS Pゴシック';font-size:16px;line-height:18px;"" >

// 書き込み欄の高さ
WriteHeight =223

// スレッドを開いた時に最下位まで移動させるか
// True : スクロールする
// False : スクロールしない
ScrollBottom =True
/**************************************************************************/
/* ここからは、ショートカットの設定です
/**************************************************************************/
[PlayerShortcut]
↓ = OpenThreadViewer
↓↑ = ChannelInfoUpdate
↑↓ = ThreadListUpdate
↓→ = Close
← = balance-10
→ = balance+10
MiddleClick = Mini&Mute
Shift+WheelUp = Volume+1
Shift+WheelDown = Volume-1
Control+WheelUp = Volume+5
Control+WheelDown = Volume-5
WheelUp = Volume+10
WheelDown = Volume-10
Up = Volume+10
Down = Volume-10
Alt+B = Bump
Alt+X = Close&RelayCut
Right = VolumeBalance+10
Left = VolumeBalance-10
Alt+Left = VolumeBalanceLeft
Alt+Right = VolumeBalanceRight
Alt+Down = VolumeBalanceMiddle
RightClick+WheelUp = Size-10%
RightClick+WheelDown = Size+10%
Shift+Up = Size-10%
Shift+Down = Size+10%
Control+Up = Size-10
Control+Down = Size+10
Space = SelectResBox
O = OpenThreadViewer
U = ThreadListUpdate
Escape = Close
T = TopMost
A = ResBox
S = StatusLabel
D = Frame
F = TitleBar
Z = AspectRate
Return = OpenContextMenu
Delete = Mute
Alt+Return = FullScreen
D1 = Size=50%
D2 = Size=75%
D3 = Size=100%
D4 = Size=150%
D5 = Size=200%
Alt+D1 = ScreenSplit=5
Alt+D2 = ScreenSplit=4
Alt+D3 = ScreenSplit=3
Alt+D4 = ScreenSplit=2
Q = ScreenSplitWidth=5
W = ScreenSplitWidth=4
E = ScreenSplitWidth=3
R = ScreenSplitWidth=2
Alt+Q = ScreenSplitHeight=5
Alt+W = ScreenSplitHeight=4
Alt+E = ScreenSplitHeight=3
Alt+R = ScreenSplitHeight=2
Alt+LeftClick = Frame
Right->LeftClick = Mute
P = ScreenShot
↑ = ScreenShot
L = OpenScreenShotFolder
G = FitSizeMovie
Control+V = OpenClipBoard
K = OpenFile
H = Retry
";

				FileStream writer = new FileStream(iniFileName,
									   FileMode.Create,  // 上書き
									   FileAccess.Write);

				byte[] bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(str);
				for (int i = 0; i < bytes.Length; i++)
				{
					writer.WriteByte(bytes[i]);
				}
				writer.Close();

				/*

				iniFile.Write("Player", "Frame", "False");

				iniFile.Write("PlayerShortcut", "↓", "OpenThreadViewer");
				iniFile.Write("PlayerShortcut", "↓↑", "ChannelInfoUpdate");
				iniFile.Write("PlayerShortcut", "↑↓", "ThreadListUpdate");
				iniFile.Write("PlayerShortcut", "↓→", "Close");
				iniFile.Write("PlayerShortcut", "←", "balance-10");
				iniFile.Write("PlayerShortcut", "→", "balance+10");
				iniFile.Write("PlayerShortcut", "MiddleClick", "Mini&Mute");
				iniFile.Write("PlayerShortcut", "Shift+WheelUp", "Volume+1");
				iniFile.Write("PlayerShortcut", "Shift+WheelDown", "Volume-1");
				iniFile.Write("PlayerShortcut", "Control+WheelUp", "Volume+5");
				iniFile.Write("PlayerShortcut", "Control+WheelDown", "Volume-5");
				iniFile.Write("PlayerShortcut", "WheelUp", "Volume+10");
				iniFile.Write("PlayerShortcut", "WheelDown", "Volume-10");
				iniFile.Write("PlayerShortcut", "Up", "Volume+10");
				iniFile.Write("PlayerShortcut", "Down", "Volume-10");
				iniFile.Write("PlayerShortcut", "Alt+B", "Bump");
				iniFile.Write("PlayerShortcut", "Alt+X", "Close&RelayCut");
				iniFile.Write("PlayerShortcut", "Right", "VolumeBalance+10");
				iniFile.Write("PlayerShortcut", "Left", "VolumeBalance-10");
				iniFile.Write("PlayerShortcut", "Alt+Left", "VolumeBalanceLeft");
				iniFile.Write("PlayerShortcut", "Alt+Right", "VolumeBalanceRight");
				iniFile.Write("PlayerShortcut", "Alt+Down", "VolumeBalanceMiddle");
				iniFile.Write("PlayerShortcut", "RightClick+WheelUp", "Size-10%");
				iniFile.Write("PlayerShortcut", "RightClick+WheelDown", "Size+10%");
				iniFile.Write("PlayerShortcut", "Shift+Up", "Size-10%");
				iniFile.Write("PlayerShortcut", "Shift+Down", "Size+10%");
				iniFile.Write("PlayerShortcut", "Control+Up", "Size-10");
				iniFile.Write("PlayerShortcut", "Control+Down", "Size+10");
				iniFile.Write("PlayerShortcut", "Space", "SelectResBox");
				iniFile.Write("PlayerShortcut", "O", "OpenThreadViewer");
				iniFile.Write("PlayerShortcut", "U", "ThreadListUpdate");
				iniFile.Write("PlayerShortcut", "Escape", "Close");
				iniFile.Write("PlayerShortcut", "T", "TopMost");
				iniFile.Write("PlayerShortcut", "A", "ResBox");
				iniFile.Write("PlayerShortcut", "S", "StatusLabel");
				iniFile.Write("PlayerShortcut", "D", "Frame");
				iniFile.Write("PlayerShortcut", "F", "TitleBar");
				iniFile.Write("PlayerShortcut", "Z", "AspectRate");
				iniFile.Write("PlayerShortcut", "Return", "OpenContextMenu");
				iniFile.Write("PlayerShortcut", "Delete", "Mute");
				iniFile.Write("PlayerShortcut", "Alt+Return", "FullScreen");
				iniFile.Write("PlayerShortcut", "D1", "Size=50%");
				iniFile.Write("PlayerShortcut", "D2", "Size=75%");
				iniFile.Write("PlayerShortcut", "D3", "Size=100%");
				iniFile.Write("PlayerShortcut", "D4", "Size=150%");
				iniFile.Write("PlayerShortcut", "D5", "Size=200%");
				iniFile.Write("PlayerShortcut", "Alt+D1", "ScreenSplit=5");
				iniFile.Write("PlayerShortcut", "Alt+D2", "ScreenSplit=4");
				iniFile.Write("PlayerShortcut", "Alt+D3", "ScreenSplit=3");
				iniFile.Write("PlayerShortcut", "Alt+D4", "ScreenSplit=2");
				iniFile.Write("PlayerShortcut", "Q", "ScreenSplitWidth=5");
				iniFile.Write("PlayerShortcut", "W", "ScreenSplitWidth=4");
				iniFile.Write("PlayerShortcut", "E", "ScreenSplitWidth=3");
				iniFile.Write("PlayerShortcut", "R", "ScreenSplitWidth=2");
				iniFile.Write("PlayerShortcut", "Alt+Q", "ScreenSplitHeight=5");
				iniFile.Write("PlayerShortcut", "Alt+W", "ScreenSplitHeight=4");
				iniFile.Write("PlayerShortcut", "Alt+E", "ScreenSplitHeight=3");
				iniFile.Write("PlayerShortcut", "Alt+R", "ScreenSplitHeight=2");
				iniFile.Write("PlayerShortcut", "Alt+LeftClick", "Frame");
				iniFile.Write("PlayerShortcut", "Right->LeftClick", "Frame");
				iniFile.Write("PlayerShortcut", "P", "ScreenShot");
				iniFile.Write("PlayerShortcut", "↑", "ScreenShot");
				iniFile.Write("PlayerShortcut", "L", "OpenScreenShotFolder");
				iniFile.Write("PlayerShortcut", "G", "FitSizeMovie");
				iniFile.Write("PlayerShortcut", "Control+V", "OpenClipBoard");
				iniFile.Write("PlayerShortcut", "K", "OpenFile");
				iniFile.Write("PlayerShortcut", "H", "Retry");
				*/
			}


			#region デフォルト設定
			{
				// デフォルト
				string[] keys = iniFile.GetKeys("Player");

				for (int i = 0; i < keys.Length; i++)
				{
					string data = iniFile.ReadString("Player", keys[i]);
					switch (keys[i])
					{
						// タイトルバー
						case "TitleBar":
							TitleBar = (data == "True");
							break;

						// レスボックス
						case "ResBox":
							panelResBox.Visible = (data == "True");
							break;

						// ステータスラベル
						case "StatusLabel":
							panelStatusLabel.Visible = (data == "True");
							break;

						// 最前列表示
						case "TopMost":
							TopMost = (data == "True");
							break;

						case "AspectRate":
							AspectRate = (data == "True");
							break;

						// レスボックスの操作方法
						case "ResBoxType":
							ResBoxType = (data == "True");
							break;

						// レスボックスを自動表示
						case "ResBoxAutoVisible":
							ResBoxAutoVisible = (data == "True");
							break;

						// 終了時にリレーを終了
						case "RlayCutOnClose":
							RlayCutOnClose = (data == "True");
							break;

						// マウスジェスチャーを使うか
						case "UseMouseGesture":
							UseMouseGesture = (data == "True");
							break;

						//　書き込み後にレスボックスを閉じる
						case "CloseResBoxOnWrite":
							CloseResBoxOnWrite = (data == "True");
							break;

						case "UseScreenMagnet":
							UseScreenMagnet = (data == "True");
							break;

						// 終了時に一緒にビューワも終了するか
						case "CloseViewerOnClose":
							CloseViewerOnClose = (data == "True");
							break;

						// バックスペースでレスボックスを閉じるか
						case "CloseResBoxOnBackSpace":
							CloseResBoxOnBackSpace = (data == "True");
							break;

						// クリックした時にレスボックスを閉じるか
						case "ClickToResBoxClose":
							ClickToResBoxClose = (data == "True");
							break;

						// 終了時に位置を保存するか
						case "SaveLocationOnClose":
							SaveLocationOnClose = (data == "True");
							break;

						// 終了時にボリュームを保存するか
						case "SaveVolumeOnClose":
							SaveVolumeOnClose = (data == "True");
							break;

						// 終了時にサイズを保存するか
						case "SaveSizeOnClose":
							SaveSizeOnClose = (data == "True");
							break;
							
						// 再生時に動画サイズに合わせる
						case "FitSizeMovie":
							FitSizeMovie = (data == "True");
							break;

						// 初期位置X
						case "X":
							try
							{
								Left = int.Parse(data);
							}
							catch
							{
							}
							break;

						// 初期位置Y
						case "Y":
							try
							{
								Top = int.Parse(data);
							}
							catch
							{
							}
							break;

						// 初期Width
						case "Width":
							try
							{
								Width = int.Parse(data);
							}
							catch
							{
							}
							break;

						// 初期Height
						case "Height":
							try
							{
								Height = int.Parse(data);
							}
							catch
							{
							}
							break;


						// ボリューム
						case "Volume":
							try
							{
								wmp.Volume = int.Parse(data);
							}
							catch
							{
							}
							break;

						// ボリューム
						case "Scale":
							try
							{
								if (data == "")
									DefaultScale = -1;

								DefaultScale = int.Parse(data);

								if (DefaultScale < 0)
									DefaultScale = -1;
							}
							catch
							{
							}
							break;

						// フォント名
						case "FontName":
							SetFont(data, labelDetail.Font.Size);
							break;

						// フォントのサイズ
						case "FontSize":
							try
							{
								SetFont(labelDetail.Font.Name, float.Parse(data));
							}
							catch
							{
							}
							break;

						case "FontColorR":
							try
							{
								int R = int.Parse(data);
								Color color = Color.FromArgb(255, R, labelDetail.ForeColor.G, labelDetail.ForeColor.B);
								labelDetail.ForeColor = color;
								labelDuration.ForeColor = color;
								labelVolume.ForeColor = color;
							}
							catch
							{
							}
							break;

						case "FontColorG":
							try
							{
								int G = int.Parse(data);
								Color color = Color.FromArgb(255, labelDetail.ForeColor.R, G, labelDetail.ForeColor.B);
								labelDetail.ForeColor = color;
								labelDuration.ForeColor = color;
								labelVolume.ForeColor = color;
							}
							catch
							{
							}
							break;

						case "FontColorB":
							try
							{
								int B = int.Parse(data);
								Color color = Color.FromArgb(255, labelDetail.ForeColor.R, labelDetail.ForeColor.G, B);
								labelDetail.ForeColor = color;
								labelDuration.ForeColor = color;
								labelVolume.ForeColor = color;
							}
							catch
							{
							}
							break;

						case "ScreenMagnetDockDist":
							try
							{
								ScreenMagnetDockDist = int.Parse(data);
							}
							catch
							{
							}
							break;

						case "MouseGestureInterval":
							try
							{
								wmp.mouseGesture.Interval = int.Parse(data);
							}
							catch
							{
							}
							break;

						case "BrowserAddress":
							BrowserAddress = data;
							break;

						case "ThreadBrowserAddress":
							ThreadBrowserAddress = data;
							break;
					}
				}
				OnPanelSizeChange();

				// Iniを書きだす
				// WriteIniFile();
			}

			#endregion

			#region ショートカット設定

			{
				// ショートカット
				string[] keys = iniFile.GetKeys("PlayerShortcut");

				for (int i = 0; i < keys.Length; i++)
				{
					string data = iniFile.ReadString("PlayerShortcut", keys[i]);

					string[] shortcut = new string[2];
					shortcut[0] = keys[i];
					shortcut[1] = data;
					ShortcutList.Add(shortcut);
				}
			}

			#endregion
		}

		/// <summary>
		/// ステータスラベルのフォントを変える
		/// </summary>
		private void SetFont(string name, float size)
		{
			// 最大文字にする
			string duration = labelDuration.Text;
			string volume = labelVolume.Text;
			labelDuration.Text = "接続中...";
			labelVolume.Text = "100";

			// フォント指定
			labelDetail.Font = new Font(name, size);
			labelDuration.Font = new Font(name, size);
			labelVolume.Font = new Font(name, size);

			// 右側ラベルの適応
			panelStatusLabel.Height = labelDetail.Height + 8;
			panelDetailRight.Left = panelStatusLabel.Width - (labelDuration.Width + labelVolume.Width);
			panelDetailRight.Width = labelDuration.Width + labelVolume.Width;
			labelVolume.Left = panelDetailRight.Width - labelVolume.Width;

			// 戻す
			labelDuration.Text = duration;
			labelVolume.Text = volume;

			OnPanelSizeChange();
		}

		/// <summary>
		/// Iniを書きだす
		/// </summary>
		private void WriteIniFile()
		{
			IniFile iniFile = new IniFile(GetCurrentDirectory() + "\\PeerstPlayer.ini");

			// 一度書き込み
			// タイトルバー
			iniFile.Write("Player", "TitleBar", TitleBar.ToString());

			// レスボックス
			iniFile.Write("Player", "ResBox", panelResBox.Visible.ToString());

			// ステータスラベル
			iniFile.Write("Player", "StatusLabel", panelStatusLabel.Visible.ToString());

			// フレーム
			iniFile.Write("Player", "Frame", Frame.ToString());

			// 最前列表示
			iniFile.Write("Player", "TopMost", TopMost.ToString());

			// アスペクト比
			iniFile.Write("Player", "AspectRate", AspectRate.ToString());

			// レスボックスの操作方法
			iniFile.Write("Player", "ResBoxType", ResBoxType.ToString());

			// レスボックスを自動表示
			iniFile.Write("Player", "ResBoxAutoVisible", ResBoxAutoVisible.ToString());

			// 終了時にリレーを終了
			iniFile.Write("Player", "RlayCutOnClose", RlayCutOnClose.ToString());

			// マウスジェスチャーを使うか
			iniFile.Write("Player", "UseMouseGesture", UseMouseGesture.ToString());

			//　書き込み後にレスボックスを閉じる
			iniFile.Write("Player", "CloseResBoxOnWrite", CloseResBoxOnWrite.ToString());

			// スクリーン吸着を使うか
			iniFile.Write("Player", "UseScreenMagnet", UseScreenMagnet.ToString());

			// 終了時に一緒にビューワも終了するか
			iniFile.Write("Player", "CloseViewerOnClose", CloseViewerOnClose.ToString());

			// クリックした時にレスボックスを閉じるか
			iniFile.Write("Player", "ClickToResBoxClose", ClickToResBoxClose.ToString());

			// 終了時に位置を保存するか
			iniFile.Write("Player", "SaveLocationOnClose", SaveLocationOnClose.ToString());

			// 終了時にボリュームを保存するか
			iniFile.Write("Player", "SaveVolumeOnClose", SaveVolumeOnClose.ToString());

			// 終了時にサイズを保存するか
			iniFile.Write("Player", "SaveSizeOnClose", SaveSizeOnClose.ToString());
			/*
			if (!SaveLocationOnClose)
			{
				// 初期位置X
				iniFile.Write("Player", "X", "");

				// 初期位置Y
				iniFile.Write("Player", "Y", "");
			}

			if (!SaveSizeOnClose)
			{
				// 初期Width
				iniFile.Write("Player", "Width", "");

				// 初期Height
				iniFile.Write("Player", "Height", "");
			}
			*/
			// ボリューム
			//iniFile.Write("Player", "Volume", wmp.Volume.ToString());

			// フォント名
			iniFile.Write("Player", "FontName", labelDetail.Font.Name);

			// フォント色
			iniFile.Write("Player", "FontColorR", labelDetail.ForeColor.R.ToString());
			iniFile.Write("Player", "FontColorG", labelDetail.ForeColor.G.ToString());
			iniFile.Write("Player", "FontColorB", labelDetail.ForeColor.B.ToString());
		}


		/// <summary>
		/// 更新完了
		/// </summary>
		void ChannelInfo_UpdateComp()
		{
			Text = ChannelInfo.Name;
			ChannelDetail = ChannelInfo.ToString();
			labelDetail.Text = ChannelDetail;
			ThreadListUpdate();

			if (ChannelInfo.IconURL != "")
			{
				try
				{
					pictureBoxIcon.Load(ChannelInfo.IconURL);
				}
				catch
				{
				}
				pictureBoxIcon.BackColor = Color.White;
				labelDetail.Left = 23;
			}
			else
			{
				labelDetail.Left = 3;
			}
		}

		/// <summary>
		/// スレッド一覧スレッド
		/// </summary>
		System.Threading.Thread httpGetThreadList;

		delegate void HttpGetThreadListDelegate();

		void HttpGetThreadListWorker()
		{
			// 初回だけスレ情報取得＋スレ移動
			if (!InitThreadSelected)
			{
				operationBbs = new OperationBbs(ChannelInfo.ContactURL);
				
				InitThreadSelected = true;
			}

			// スレッド一覧を取得(スレッド)
			ThreadList = operationBbs.GetThreadList();

			if (comboBoxThreadList.InvokeRequired)
			{
				Invoke(new HttpGetThreadListDelegate(HttpGetThreadListMethod));
			}
			else
			{
				HttpGetThreadListMethod();
			}
		}

		/// <summary>
		/// スレッド一覧取得（スレッド）
		/// </summary>
		void HttpGetThreadListMethod()
		{
			// コンボボックスにセット
			comboBoxThreadList.Items.Clear();
			for (int i = 0; i < ThreadList.Count; i++)
			{
				// スレタイ(レス数)
				comboBoxThreadList.Items.Add(ThreadList[i].Title + "(" + ThreadList[i].ResCount + ")");
			}

			// TODO スレッドURLが指定されている場合は、コンボボックスを選択
			if (ThreadNo != "")
			{
				// コンボボックスのスレッドを選択
				int index = 0;
				for (int i = 0; i < ThreadList.Count; i++)
				{
					if (ThreadNo == ThreadList[i].ThreadNo)
					{
						index = i;
					}
				}
				if (comboBoxThreadList.Items.Count > index)
				{
					comboBoxThreadList.SelectedIndex = index;
				}
			}
			// TODO スレッドURLが指定されていなかった場合は、1番上を選択
			else
			{
				// １番上を選択
				if (comboBoxThreadList.Items.Count > 0)
				{
					comboBoxThreadList.SelectedIndex = 0;
				}
			}

			if (resBox.Selected)
			{
				resBox.Text = ResBoxText;
			}
		}

		/// <summary>
		/// スレッド一覧を更新
		/// </summary>
		private void ThreadListUpdate()
		{
			if (resBox.Selected)
			{
				ResBoxText = resBox.Text;
			}
			httpGetThreadList = new System.Threading.Thread(new System.Threading.ThreadStart(HttpGetThreadListWorker));
			httpGetThreadList.IsBackground = true;
			httpGetThreadList.Start();
		}

		/// <summary>
		/// スレッド一覧スレッド
		/// </summary>
		System.Threading.Thread channelInfoUpdateThread;

		/// <summary>
		/// チャンネル情報を更新
		/// </summary>
		void ChannelInfoUpdate()
		{
			channelInfoUpdateThread = new System.Threading.Thread(new System.Threading.ThreadStart(ChannelInfoUpdateWorker));
			channelInfoUpdateThread.IsBackground = true;
			channelInfoUpdateThread.Start();
		}

		delegate void ChannelInfoUpdateDelegate();

		/// <summary>
		/// チャンネル情報更新(スレッド処理)
		/// </summary>
		void ChannelInfoUpdateWorker()
		{
			// チャンネル更新
			ChannelInfoUpdateMethod();

			// GUIの更新
			Invoke(new ChannelInfoUpdateDelegate(ChannelInfo_UpdateComp));
		}

		/// <summary>
		/// チャンネル情報を更新スレッド
		/// </summary>
		void ChannelInfoUpdateMethod()
		{
			ChannelInfo.Update(wmp.URLData);
		}


		/// <summary>
		/// パネルのアンカー設定
		/// </summary>
		bool PanelAnchor
		{
			set
			{
				// アンカー設定
				if (value)
				{
					panelWMP.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
					panelResBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
					panelStatusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
				}
				// アンカー解除
				else
				{
					panelWMP.Anchor = AnchorStyles.None;
					panelResBox.Anchor = AnchorStyles.None;
					panelStatusLabel.Anchor = AnchorStyles.None;
				}
			}
		}
		/// <summary>
		/// サイズチェンジ
		/// </summary>
		public void OnPanelSizeChange()
		{
			if (WindowState == FormWindowState.Normal)
			{
				// アンカーをNone
				PanelAnchor = false;

				// FormSize変更
				int height = 0;
				height += (panelWMP.Visible ? panelWMP.Height : 0);
				height += (panelResBox.Visible ? panelResBox.Height : 0);
				height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

				Size frame = Size - ClientSize;
				Size = new Size(frame.Width + panelWMP.Width, frame.Height + height);

				// WMPパネル
				height = 0;
				panelWMP.Location = new Point(0, 0);
				panelWMP.Width = ClientSize.Width;
				height += (panelWMP.Visible ? panelWMP.Height : 0);

				// レスボックスパネル
				panelResBox.Location = new Point(0, height);
				panelResBox.Width = ClientSize.Width;
				height += (panelResBox.Visible ? panelResBox.Height : 0);

				// ステータスラベルパネル
				panelStatusLabel.Location = new Point(0, height);
				panelStatusLabel.Width = ClientSize.Width;
				height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

				// アンカーを再設定
				PanelAnchor = true;
			}
			else
			{
				// アンカーをNone
				PanelAnchor = false;

				int height = ClientSize.Height;
				
				// ステータスラベルパネル
				height -= (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);
				panelStatusLabel.Location = new Point(0, height);
				panelStatusLabel.Width = ClientSize.Width;

				// レスボックスパネル
				height -= (panelResBox.Visible ? panelResBox.Height : 0);
				panelResBox.Location = new Point(0, height);
				panelResBox.Width = ClientSize.Width;

				// WMPパネル
				panelWMP.Location = new Point(0, 0);
				panelWMP.Size = new Size(Width, height);
				panelWMP.Width = ClientSize.Width;

				// アンカーを再設定
				PanelAnchor = true;
			}
		}

		private bool CheckWrite()
		{
			if (BeforeWriteThreadNo != ThreadNo)
			{
				string text = "板名：" + operationBbs.GetBbsName() + "\nスレッド名：" + resBox.ThreadTitle + "\n書き込み内容：" + resBox.Text;
				DialogResult result = MessageBox.Show(text, "書き込み確認", MessageBoxButtons.YesNo);

				if (result == DialogResult.No)
				{
					return false;
				}
				else if (result == DialogResult.Yes)
				{
					return true;
				}
			}

			return true;
		}

		bool IsWriting = false;

		/// <summary>
		/// スレッドURLを取得
		/// </summary>
		private string GetThreadUrl()
		{
			return operationBbs.GetUrl();
		}

		#region APIで使う構造体
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}
		#endregion

		#region WIN32API
		/// <summary>
		/// 座標を含むウインドウのハンドルを取得
		/// </summary>
		/// <param name="Point">調査する座標</param>
		/// <returns>ポイントにウインドウがなければNULL</returns>
		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(POINT Point);

		/// <summary>
		/// ハンドルからウインドウの位置を取得
		/// </summary>
		/// <param name="hWnd">ウインドウのハンドル</param>
		/// <param name="lpRect">ウインドウの座標</param>
		/// <returns>成功すればtrue</returns>
		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		/// <summary>
		/// 指定したハンドルの祖先のハンドルを取得
		/// </summary>
		/// <param name="hwnd">ハンドル</param>
		/// <param name="gaFlags">フラグ</param>
		/// <returns>祖先のハンドル</returns>
		[DllImport("user32.dll")]
		private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

		private const uint GA_ROOT = 2;

		#endregion


		/// <summary>
		/// 画面分割：幅
		/// </summary>
		void ScreenSplitWidth(int num)
		{
			int width = (int)(Screen.Width / (float)num);
			Size = new Size(width, Height);
			panelWMP.Size = new Size(wmp.Width, (int)(wmp.AspectRate * wmp.Width));
			OnPanelSizeChange();
		}

		/// <summary>
		/// 画面分割：高さ
		/// </summary>
		void ScreenSplitHeight(int num)
		{
			int height = (int)(Screen.Height / (float)num);
			Size = new Size(Width, height);
			panelWMP.Size = new Size((int)(1 / wmp.AspectRate * wmp.Height), wmp.Height);
			OnPanelSizeChange();
		}

		/// <summary>
		/// 画面分割
		/// </summary>
		void ScreenSplit(int num)
		{
			int width = (int)(Screen.Width / (float)num);
			int height = (int)(Screen.Height / (float)num);
			Size = new Size(width, height);
			OnPanelSizeChange();
		}

		/// <summary>
		/// 幅指定
		/// </summary>
		void SetWidth(int width)
		{
			panelWMP.Size = new Size(width, (int)(wmp.AspectRate * width));
			OnPanelSizeChange();
		}

		/// <summary>
		/// 高さ指定
		/// </summary>
		void SetHeight(int height)
		{
			panelWMP.Size = new Size((int)(1 / wmp.AspectRate * height), height);
			OnPanelSizeChange();
		}

		/// <summary>
		/// サイズ指定
		/// </summary>
		void SetSize(int width, int height)
		{
			panelWMP.Size = new Size(width, height);
			OnPanelSizeChange();
		}

		/// <summary>
		/// 拡大率
		/// </summary>
		void SetScale(int scale)
		{
			panelWMP.Size = new Size((int)(wmp.ImageWidth * (scale / (float)100)), (int)(wmp.ImageHeight * (scale / (float)100)));
			OnPanelSizeChange();
		}


		/// <summary>
		/// 作業フォルダを取得
		/// </summary>
		string GetCurrentDirectory()
		{
			if (Environment.GetCommandLineArgs().Length > 0)
			{
				string folder = Environment.GetCommandLineArgs()[0];

				//string folder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				folder = folder.Substring(0, folder.LastIndexOf('\\'));

				return folder;
			}
			return "";
		}

		/// <summary>
		/// 最大化から解除した時のパネルサイズ変更
		/// </summary>
		public void OnPanelSizeChange(Size PanelWMP)
		{
			// アンカーをNone
			PanelAnchor = false;

			// FormSize変更
			int height = 0;
			height += (panelWMP.Visible ? PanelWMP.Height : 0);
			height += (panelResBox.Visible ? panelResBox.Height : 0);
			height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

			Size frame = Size - ClientSize;
			Size = new Size(frame.Width + PanelWMP.Width, frame.Height + height);

			// WMPパネル
			height = 0;
			panelWMP.Location = new Point(0, 0);
			panelWMP.Size = PanelWMP;
			height += (panelWMP.Visible ? PanelWMP.Height : 0);

			// レスボックスパネル
			panelResBox.Location = new Point(0, height);
			panelResBox.Width = ClientSize.Width;
			height += (panelResBox.Visible ? panelResBox.Height : 0);

			// ステータスラベルパネル
			panelStatusLabel.Location = new Point(0, height);
			panelStatusLabel.Width = ClientSize.Width;
			height += (panelStatusLabel.Visible ? panelStatusLabel.Height : 0);

			// アンカーを再設定
			PanelAnchor = true;
		}
		
		#region 規定のブラウザを開く

		private static string GetDefaultBrowserExePath()
		{
			return GetDefaultExePath(@"http\shell\open\command");
		}

		private static string GetDefaultMailerExePath()
		{
			return GetDefaultExePath(@"mailto\shell\open\command");
		}

		private static string GetDefaultExePath(string keyPath)
		{
			string path = "";

			// レジストリ・キーを開く
			// 「HKEY_CLASSES_ROOT\xxxxx\shell\open\command」
			RegistryKey rKey = Registry.ClassesRoot.OpenSubKey(keyPath);
			if (rKey != null)
			{
				// レジストリの値を取得する
				string command = (string)rKey.GetValue(String.Empty);
				if (command == null)
				{
					return path;
				}

				// 前後の余白を削る
				command = command.Trim();
				if (command.Length == 0)
				{
					return path;
				}

				// 「"」で始まる長いパス形式かどうかで処理を分ける
				if (command[0] == '"')
				{
					// 「"～"」間の文字列を抽出
					int endIndex = command.IndexOf('"', 1);
					if (endIndex != -1)
					{
						// 抽出開始を「1」ずらす分、長さも「1」引く
						path = command.Substring(1, endIndex - 1);
					}
				}
				else
				{
					// 「（先頭）～（スペース）」間の文字列を抽出
					int endIndex = command.IndexOf(' ');
					if (endIndex != -1)
					{
						path = command.Substring(0, endIndex);
					}
					else
					{
						path = command;
					}
				}
			}

			return path;
		}

		#endregion
	}
}