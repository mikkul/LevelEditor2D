using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using Myra;
using Myra.Graphics2D.Brushes;
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
		private const int _defaultGridCellSizePixels = 32;

		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private Desktop _desktop;

		private Menu _topMenu;
		private Window _editorPreferencesWindow;
		private Panel _editorAreaPanel;

		private bool _isPopupActive;
		private string _openedFilePath;
		private Level _unmodifiedLevel;
		private Level _currentLevel;
		private float _zoomLevel;

		public Level CurrentLevel
		{
			get
			{
				return _currentLevel;
			}
			private set
			{
				_currentLevel = value;
				_unmodifiedLevel = Level.Clone(value);
				UpdateLevelPropertyGrid(value);
			}
		}

		public bool HasUnsavedChanges
		{
			get => !_unmodifiedLevel.Equals(_currentLevel);
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
				editorPreferences.GridLinesColor = new Color(100, 100, 100);
				editorPreferences.ToolbarBackgroundColor = new Color(125, 125, 125);
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

			// TODO: figure out how to create a prompt on window close
			//Form windowForm = (Form)Form.FromHandle(Window.Handle);
			//windowForm.FormClosing += WindowForm_FormClosing;
		}

		//private void WindowForm_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
		//{
		//	if(HasUnsavedChanges)
		//	{
		//		e.Cancel = true;
		//	}
		//}

		protected override void Initialize()
		{
			_graphics.PreferredBackBufferWidth = 1280;
			_graphics.PreferredBackBufferHeight = 800;
			_graphics.ApplyChanges();

			_zoomLevel = 1f;

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
			_editorPreferencesWindow = GetWidget<Window>("editor-preferences-window");

			var editorPreferencesPropertyGrid = new PropertyGrid
			{
				Id = "editor-preferences-property-grid"
			};
			editorPreferencesPropertyGrid.Object = Global.EditorPreferences;
			_editorPreferencesWindow.Content = editorPreferencesPropertyGrid;
			_editorPreferencesWindow.Closed += (s, a) =>
			{
				_isPopupActive = false;
			};

			_editorPreferencesWindow.Close();

			_topMenu = GetWidget<Menu>("top-menu");

			var menuFile = _topMenu.FindMenuItemById("menu-file");
			var menuEdit = _topMenu.FindMenuItemById("menu-edit");

			var menuFileNew = _topMenu.FindMenuItemById("menu-file-new");
			menuFileNew.Selected += OnFileNewClicked;

			var menuFileOpen = _topMenu.FindMenuItemById("menu-file-open");
			menuFileOpen.Selected += OnFileOpenClicked;

			var menuFileSave = _topMenu.FindMenuItemById("menu-file-save");
			menuFileSave.Selected += OnFileSaveClicked;

			var menuFileSaveAs = _topMenu.FindMenuItemById("menu-file-save-as");
			menuFileSaveAs.Selected += OnFileSaveAsClicked;

			var menuFileExit = _topMenu.FindMenuItemById("menu-file-exit");
			menuFileExit.Selected += OnFileExitClicked;

			var menuEditPreferences = _topMenu.FindMenuItemById("menu-edit-preferences");
			menuEditPreferences.Selected += OnEditPreferencesClicked;

			var mainSplitPane = GetWidget<HorizontalSplitPane>("main-split-pane");
			mainSplitPane.SetSplitterPosition(0, Global.EditorPreferences.LeftSplitterPosition);
			mainSplitPane.SetSplitterPosition(1, Global.EditorPreferences.RightSplitterPosition);
			mainSplitPane.ProportionsChanged += OnMainSplitPaneProportionsChanged;

			var leftSidebar = GetWidget<Panel>("left-sidebar");
			var rightSidebar = GetWidget<Panel>("right-sidebar");
			leftSidebar.Background = new SolidBrush(Global.EditorPreferences.ToolbarBackgroundColor);
			rightSidebar.Background = new SolidBrush(Global.EditorPreferences.ToolbarBackgroundColor);

			Global.EditorPreferences.ToolbarBackgroundColorChanged += EditorPreferences_ToolbarBackgroundColorChanged;

			_editorAreaPanel = GetWidget<Panel>("editor-area");
		}

		private void EditorPreferences_ToolbarBackgroundColorChanged(object sender, EventArgs e)
		{
			var leftSidebar = GetWidget<Panel>("left-sidebar");
			var rightSidebar = GetWidget<Panel>("right-sidebar");
			leftSidebar.Background = new SolidBrush(Global.EditorPreferences.ToolbarBackgroundColor);
			rightSidebar.Background = new SolidBrush(Global.EditorPreferences.ToolbarBackgroundColor);
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
			_editorPreferencesWindow.ShowModal(_desktop);
			_isPopupActive = true;
		}

		private void OnFileOpenClicked(object sender, EventArgs e)
		{
			var dialog = new FileDialog(FileDialogMode.OpenFile)
			{
				Filter = "*.lvl"
			};

			dialog.Closed += (s, a) =>
			{
				_isPopupActive = false;

				if (!dialog.Result)
				{
					return;
				}

				OpenFile(dialog.FilePath);
			};

			dialog.ShowModal(_desktop);
			_isPopupActive = true;
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
				_isPopupActive = false;

				if (!dialog.Result)
				{
					return;
				}

				SaveFileAs(dialog.FilePath);
			};

			dialog.ShowModal(_desktop);
			_isPopupActive = true;
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
			_unmodifiedLevel = Level.Clone(CurrentLevel);
			_openedFilePath = string.Empty;
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
			_unmodifiedLevel = Level.Clone(_currentLevel);
		}

		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			HandleInput();
			UpdateWindowTitle();

			base.Update(gameTime);
		}

		private void HandleInput()
		{
			var keyboardState = KeyboardExtended.GetState();
			var mouseState = MouseExtended.GetState();

			if (keyboardState.IsKeyDown(Keys.Subtract))
			{
				_zoomLevel += 0.1f;
			}
			else if (keyboardState.IsKeyDown(Keys.Add))
			{
				_zoomLevel -= 0.1f;
				if(_zoomLevel < 0.1f)
				{
					_zoomLevel = 0.1f;
				}
			}

			if (mouseState.WasButtonJustDown(MouseButton.Left))
			{
				if(_editorAreaPanel.ActualBounds.Contains(mouseState.Position) && !_topMenu.IsOpen && !_isPopupActive)
				{
					var vertex = new Vertex(mouseState.Position.X * _zoomLevel, mouseState.Position.Y * _zoomLevel);
					_currentLevel.Vertices.Add(vertex);
					Console.WriteLine(vertex);
				}
			}
		}

		private void UpdateWindowTitle()
		{
			Window.Title = " ";
			if (HasUnsavedChanges)
			{
				Window.Title += "*";
			}
			if (string.IsNullOrEmpty(_openedFilePath))
			{
				Window.Title += "untitled";
			}
			else
			{
				Window.Title += _openedFilePath;
			}
			Window.Title += " - LevelEditor2D";
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Global.EditorPreferences.BackgroundColor);

			_spriteBatch.Begin();

			// draw the grid
			var windowWidth = _graphics.PreferredBackBufferWidth;
			var windowHeight = _graphics.PreferredBackBufferHeight;
			float cellGridSize = _defaultGridCellSizePixels / _zoomLevel;
			int horizontalLineCount = (int)((float)windowHeight / cellGridSize);
			int verticalLineCount = (int)((float)windowWidth / cellGridSize);
			// horizontal lines
			for (int i = 0; i < horizontalLineCount; i++)
			{
				float y = cellGridSize + cellGridSize * i;
				_spriteBatch.DrawLine(0, y, windowWidth, y, Global.EditorPreferences.GridLinesColor);
			}
			// vertical lines
			for (int i = 0; i < verticalLineCount; i++)
			{
				float x = cellGridSize + cellGridSize * i;
				_spriteBatch.DrawLine(x, 0, x, windowHeight, Global.EditorPreferences.GridLinesColor);
			}

			foreach (var vertex in _currentLevel.Vertices)
			{
				var normalizedX = vertex.X / _zoomLevel;
				var normalizedY = vertex.Y / _zoomLevel;
				_spriteBatch.DrawPoint(normalizedX, normalizedY, Color.Yellow, 5);
			}

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
