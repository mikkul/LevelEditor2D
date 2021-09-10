using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Properties;
using System;
using System.IO;
using System.Xml.Serialization;

namespace LevelEditor2D
{
	public class Main : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private Desktop _desktop;

		private string _openedFilePath;
		private Level _currentLevel;

		public Level CurrentLevel
		{
			get
			{
				return _currentLevel;
			}
			private set
			{
				_currentLevel = value;
				UpdateLevelPropertyGrid(value);
			}
		}

		public Main(string[] args)
		{
			_graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;

			// check if app data folder already exists
			// if not, create the directory
			if (!Directory.Exists(Global.AppDataPath))
			{
				Directory.CreateDirectory(Global.AppDataPath);
			}

			// load preferences
			// if the preferences file doesn't exist, create a new EditorPreferences object with default values
			// and save it on the disk
			var editorPreferences = EditorPreferences.Load();
			if (editorPreferences == null)
			{
				editorPreferences = new EditorPreferences();
				editorPreferences.BackgroundColor = new Color(82, 82, 82);
				editorPreferences.LeftSplitterPosition = 0.15f;
				editorPreferences.RightSplitterPosition = 0.75f;
			}
			Global.EditorPreferences = editorPreferences;

			// if editor was opened with arguments, try to open a file with the given path
			// else create a new empty file
			if(args.Length > 0)
			{
				var filePath = args[0];
				if (!File.Exists(filePath))
				{
					throw new ArgumentException("Couldn't find the specified file");
				}
				_openedFilePath = filePath;
			}
		}

		protected override void Initialize()
		{
			_graphics.PreferredBackBufferWidth = 1280;
			_graphics.PreferredBackBufferHeight = 800;
			_graphics.ApplyChanges();

			base.Initialize();
		}

		protected override void LoadContent()
		{
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			MyraEnvironment.Game = this;

			var uiData = File.ReadAllText("Content/UI.xmmp");
			var project = Project.LoadFromXml(uiData);

			_desktop = new Desktop();
			_desktop.Root = project.Root;
			BuildUI();

			if(string.IsNullOrEmpty(_openedFilePath))
			{
				NewFile();
			}
			else
			{
				OpenFile(_openedFilePath);
			}
		}

		private void BuildUI()
		{
			var editorPreferencesWindow = GetWidget<Window>("editor-preferences-window");

			var editorPreferencesPropertyGrid = new PropertyGrid
			{
				Id = "editor-preferences-property-grid"
			};
			editorPreferencesPropertyGrid.Object = Global.EditorPreferences;
			editorPreferencesWindow.Content = editorPreferencesPropertyGrid;

			editorPreferencesWindow.Close();

			var menu = GetWidget<Menu>("top-menu");

			var menuFileNew = menu.FindMenuItemById("menu-file-new");
			menuFileNew.Selected += OnFileNewClicked;

			var menuFileOpen = menu.FindMenuItemById("menu-file-open");
			menuFileOpen.Selected += OnFileOpenClicked;

			var menuFileSave = menu.FindMenuItemById("menu-file-save");
			menuFileSave.Selected += OnFileSaveClicked;

			var menuFileSaveAs = menu.FindMenuItemById("menu-file-save-as");
			menuFileSaveAs.Selected += OnFileSaveAsClicked;

			var menuFileExit = menu.FindMenuItemById("menu-file-exit");
			menuFileExit.Selected += OnFileExitClicked;

			var menuEditPreferences = menu.FindMenuItemById("menu-edit-preferences");
			menuEditPreferences.Selected += OnEditPreferencesClicked;

			var mainSplitPane = GetWidget<HorizontalSplitPane>("main-split-pane");
			mainSplitPane.SetSplitterPosition(0, Global.EditorPreferences.LeftSplitterPosition);
			mainSplitPane.SetSplitterPosition(1, Global.EditorPreferences.RightSplitterPosition);
			mainSplitPane.ProportionsChanged += OnMainSplitPaneProportionsChanged;
		}

		private void UpdateLevelPropertyGrid(Level level)
		{
			var levelPropertyGrid = GetWidget<PropertyGrid>("level-property-grid");
			levelPropertyGrid.RemoveFromParent();

			var newLevelPropertyGrid = new PropertyGrid()
			{
				Id = "level-property-grid",
			};
			newLevelPropertyGrid.Object = level;
			var rightSidebar = GetWidget<Panel>("right-sidebar");
			rightSidebar.Widgets.Add(newLevelPropertyGrid);
		}

		private T GetWidget<T>(string id) where T : class
		{
			return _desktop.Root.FindWidgetById(id) as T;
		}

		private void OnMainSplitPaneProportionsChanged(object sender, EventArgs e)
		{
			var splitPane = (HorizontalSplitPane)sender;
			Global.EditorPreferences.LeftSplitterPosition = splitPane.GetSplitterPosition(0);
			Global.EditorPreferences.RightSplitterPosition = splitPane.GetSplitterPosition(1);
		}

		private void OnEditPreferencesClicked(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void OnFileOpenClicked(object sender, EventArgs e)
		{
			var dialog = new FileDialog(FileDialogMode.OpenFile)
			{
				Filter = "*.lvl"
			};

			dialog.Closed += (s, a) =>
			{
				if (!dialog.Result)
				{
					return;
				}

				OpenFile(dialog.FilePath);
			};

			dialog.ShowModal(_desktop);
		}

		private void OnFileExitClicked(object sender, EventArgs e)
		{
			Exit();
		}

		private void OnFileSaveAsClicked(object sender, EventArgs e)
		{
			var dialog = new FileDialog(FileDialogMode.SaveFile)
			{
				Filter = "*.lvl"
			};

			dialog.Closed += (s, a) =>
			{
				if (!dialog.Result)
				{
					return;
				}

				SaveFileAs(dialog.FilePath);
			};

			dialog.ShowModal(_desktop);
		}

		private void OnFileSaveClicked(object sender, EventArgs e)
		{
			if(string.IsNullOrEmpty(_openedFilePath))
			{
				OnFileSaveAsClicked(null, EventArgs.Empty);
				return;
			}

			SaveFile();
		}

		private void OnFileNewClicked(object sender, EventArgs e)
		{
			NewFile();
		}

		private void NewFile()
		{
			CurrentLevel = new Level();
		}

		private void OpenFile(string filePath)
		{
			try
			{
				using (var stream = new StreamReader(filePath))
				{
					var serializer = new XmlSerializer(typeof(Level));
					CurrentLevel = (Level)serializer.Deserialize(stream);
				}
				_openedFilePath = filePath;
			}
			catch(Exception e)
			{
				var errorDialog = Dialog.CreateMessageBox("Error", $"Couldn't open the specified file. \n {e.Message}");
				errorDialog.ButtonCancel.Visible = false;
				errorDialog.ShowModal(_desktop);
			}
		}

		private void SaveFile()
		{
			SaveFileAs(_openedFilePath);
		}

		private void SaveFileAs(string filePath)
		{
			using (var stream = new StreamWriter(filePath, false))
			{
				var serializer = new XmlSerializer(typeof(Level));
				serializer.Serialize(stream, _currentLevel);
			}
			_openedFilePath = filePath;
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Global.EditorPreferences.BackgroundColor);

			_spriteBatch.Begin();

			// draw the grid

			_spriteBatch.End();

			_desktop.Render();

			base.Draw(gameTime);
		}

		protected override void OnExiting(object sender, EventArgs args)
		{
			Global.EditorPreferences.Save();

			base.OnExiting(sender, args);
		}
	}
}
