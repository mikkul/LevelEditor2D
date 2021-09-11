using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Collections;
using MonoGame.Extended.Input;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.File;
using Myra.Graphics2D.UI.Properties;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace LevelEditor2D
{
	public class Main : Game
	{
		private GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private Desktop _desktop;

		private KeyboardStateExtended _keyboardState;
		private MouseStateExtended _mouseState;

		private Menu _topMenu;
		private Window _editorPreferencesWindow;
		private Panel _editorAreaPanel;
		private PropertyGrid _objectPropertiesPropGrid;

		private bool _isPopupActive;
		private string _openedFilePath;
		private Level _unmodifiedLevel;
		private Level _currentLevel;
		private float _zoomLevel;
		private Tool _selectedTool;
		private ObservableCollection<Vertex> _selectedVertices;

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
				Global.InitIds(value);
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
				editorPreferences.SidebarBackground = new Color(125, 125, 125);
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
			_selectedVertices = new ObservableCollection<Vertex>();
			_selectedVertices.Cleared += OnSelectedVerticesCleared;
			_selectedVertices.ItemAdded += OnSelectedVerticesItemAdded;
			_selectedVertices.ItemRemoved += OnSelectedVerticesItemRemoved;

			base.Initialize();
		}

		private void OnSelectedVerticesItemRemoved(object sender, ItemEventArgs<Vertex> e)
		{
			if (_selectedVertices.Count == 1)
			{
				_objectPropertiesPropGrid.Object = _selectedVertices[0];
			}
			else
			{
				_objectPropertiesPropGrid.Object = null;
			}
			
			if(_selectedVertices.Count == 0)
			{
				var deleteButton = GetWidget<TextButton>("properties-delete-object");
				deleteButton.Visible = false;
			}
		}

		private void OnSelectedVerticesItemAdded(object sender, ItemEventArgs<Vertex> e)
		{
			if(_selectedVertices.Count == 1)
			{
				_objectPropertiesPropGrid.Object = _selectedVertices[0];
			}
			else
			{
				_objectPropertiesPropGrid.Object = null;
			}

			if(_selectedVertices.Count >= 1)
			{
				var deleteButton = GetWidget<TextButton>("properties-delete-object");
				deleteButton.Visible = true;
			}
		}

		private void OnSelectedVerticesCleared(object sender, EventArgs e)
		{
			_objectPropertiesPropGrid.Object = null;
			var deleteButton = GetWidget<TextButton>("properties-delete-object");
			deleteButton.Visible = false;
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

			OnEditorPreferencesSidebarBackgroundColorChanged(null, EventArgs.Empty);

			Global.EditorPreferences.SidebarBackgroundChanged += OnEditorPreferencesSidebarBackgroundColorChanged;

			var toolbarPanel = GetWidget<Panel>("toolbar-panel");
			var propertiesPanel = GetWidget<VerticalStackPanel>("properties-panel");

			_objectPropertiesPropGrid = new PropertyGrid
			{
				Id = "object-properties",
			};
			propertiesPanel.Widgets.Add(_objectPropertiesPropGrid);

			var propertiesDeleteObjectButton = GetWidget<TextButton>("properties-delete-object");
			propertiesDeleteObjectButton.Click += OnPropertiesDeleteObjectButtonClicked;

			_editorAreaPanel = GetWidget<Panel>("editor-area");

			var toolSelectButton = GetWidget<TextButton>("tool-select");
			toolSelectButton.Click += OnToolSelectClicked;
			var toolVertexButton = GetWidget<TextButton>("tool-vertex");
			toolVertexButton.Click += OnToolVertexClicked;
			var toolEdgeButton = GetWidget<TextButton>("tool-edge");
			toolEdgeButton.Click += OnToolEdgeClicked;
		}

		private void OnPropertiesDeleteObjectButtonClicked(object sender, EventArgs e)
		{
			foreach (var vertex in _selectedVertices)
			{
				CurrentLevel.Vertices.Remove(vertex);
				var affectedEdges = CurrentLevel.Edges
					.Where(e => e.A == vertex || e.B == vertex)
					.ToList();
				foreach (var edge in affectedEdges)
				{
					CurrentLevel.Edges.Remove(edge);
				}
			}
			_selectedVertices.Clear();
		}

		private void OnToolEdgeClicked(object sender, EventArgs e)
		{
			_selectedTool = Tool.Edge;
			Console.WriteLine(_selectedTool);
		}

		private void OnToolVertexClicked(object sender, EventArgs e)
		{
			_selectedTool = Tool.Vertex;
			Console.WriteLine(_selectedTool);
		}

		private void OnToolSelectClicked(object sender, EventArgs e)
		{
			_selectedTool = Tool.Select;
			Console.WriteLine(_selectedTool);
		}

		private void OnEditorPreferencesSidebarBackgroundColorChanged(object sender, EventArgs e)
		{
			var toolbarPanel = GetWidget<Panel>("toolbar-panel");
			var propertiesPanel = GetWidget<VerticalStackPanel>("properties-panel");
			toolbarPanel.Background = new SolidBrush(Global.EditorPreferences.SidebarBackground);
			propertiesPanel.Background = new SolidBrush(Global.EditorPreferences.SidebarBackground);
		}

		private T GetWidget<T>(string id) where T : Widget
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
			HandleInput();
			UpdateWindowTitle();

			base.Update(gameTime);
		}

		private void HandleInput()
		{
			// Friendly reminder that developers of MonoGame.Extended made some method names confusing:
			// KeyboardState.WasKeyJustDown actually fires when the key is released, while
			// KeyboardState.WasKeyJustUp fires when the key is first pressed
			_keyboardState = KeyboardExtended.GetState();
			_mouseState = MouseExtended.GetState();

			if (_keyboardState.IsKeyDown(Keys.Subtract))
			{
				_zoomLevel += 0.1f;
			}
			else if (_keyboardState.IsKeyDown(Keys.Add))
			{
				_zoomLevel -= 0.1f;
				if(_zoomLevel < 0.1f)
				{
					_zoomLevel = 0.1f;
				}
			}

			if(_keyboardState.WasKeyJustUp(Keys.Delete))
			{
				OnPropertiesDeleteObjectButtonClicked(null, EventArgs.Empty);
			}

			if(_keyboardState.WasKeyJustUp(Keys.Escape))
			{
				_selectedVertices.Clear();
			}

			if (_mouseState.WasButtonJustDown(MouseButton.Left))
			{
				HandleTools(_mouseState, _keyboardState);
			}
		}

		private void HandleTools(MouseStateExtended mouseState, KeyboardStateExtended keyboardState)
		{
			if (!_editorAreaPanel.ActualBounds.Contains(mouseState.Position) || _topMenu.IsOpen || _isPopupActive)
			{
				return;
			}

			var worldX = mouseState.Position.X * _zoomLevel;
			var worldY = mouseState.Position.Y * _zoomLevel;

			if (_selectedTool == Tool.Select)
			{
				HandleSelectTool(keyboardState, worldX, worldY);
			}
			else if(_selectedTool == Tool.Vertex)
			{
				HandleVertexTool(worldX, worldY);
			}
			else if(_selectedTool == Tool.Edge)
			{
				HandleEdgeTool(worldX, worldY);
			}
		}

		private void HandleEdgeTool(float worldX, float worldY)
		{
			var vertex = Vertex.Create(worldX, worldY);
			_currentLevel.Vertices.Add(vertex);

			if(_selectedVertices.Count == 1)
			{
				var edge = new Edge(_selectedVertices[0], vertex);
				_currentLevel.Edges.Add(edge);
			}

			_selectedVertices.Clear();
			_selectedVertices.Add(vertex);
			Console.WriteLine(vertex);
		}

		private void HandleVertexTool(float worldX, float worldY)
		{
			var vertex = Vertex.Create(worldX, worldY);
			_currentLevel.Vertices.Add(vertex);
			_selectedVertices.Clear();
			_selectedVertices.Add(vertex);
			Console.WriteLine(vertex);
		}

		private void HandleSelectTool(KeyboardStateExtended keyboardState, float worldX, float worldY)
		{
			var selectMultiple = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

			var selectedVertex = CurrentLevel.Vertices.FirstOrDefault(v =>
			{
				var deltaX = Math.Abs(v.X - worldX);
				var deltaY = Math.Abs(v.Y - worldY);
				var dist = deltaX * deltaX + deltaY * deltaY;
				return dist <= Global.SelectionDistanceToleranceSquared;
			});

			if (selectedVertex != null)
			{
				if (_selectedVertices.Contains(selectedVertex))
				{
					_selectedVertices.Remove(selectedVertex);
				}
				else
				{
					if (!selectMultiple)
					{
						_selectedVertices.Clear();
					}
					_selectedVertices.Add(selectedVertex);
				}
				return;
			}

			//var selectedEdge = CurrentLevel.Edges.FirstOrDefault(e =>
			//{
			//	var deltaX1 = Math.Abs(e.A.X - worldX);
			//	var deltaY1 = Math.Abs(e.A.Y - worldY);
			//	var deltaX2 = Math.Abs(e.B.X - worldX);
			//	var deltaY2 = Math.Abs(e.B.Y - worldY);
			//	var dist1 = deltaX1 * deltaX1 + deltaY1 * deltaY1;
			//	var dist2 = deltaX2 * deltaX2 + deltaY2 * deltaY2;
			//	var edgeDistX = Math.Abs(e.A.X - e.B.X);
			//	var edgeDistY = Math.Abs(e.A.Y - e.B.Y);
			//	var edgeLength = edgeDistX * edgeDistX + edgeDistY + edgeDistY;
			//	return Math.Abs(dist1 + dist2 - edgeLength) <= Global.SelectionDistanceToleranceSquared;
			//});

			//if (selectedEdge != null)
			//{
			//	if (_selectedVertices.Contains(selectedVertex))
			//	{
			//		_selectedVertices.Remove(selectedVertex);
			//	}
			//	else
			//	{
			//		if (!selectMultiple)
			//		{
			//			_selectedVertices.Clear();
			//		}
			//		_selectedVertices.Add(selectedVertex);
			//	}
			//	return;
			//}

			_selectedVertices.Clear();
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
			float cellGridSize = Global.DefaultGridCellSize / _zoomLevel;
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

			// render vertices
			foreach (var vertex in _currentLevel.Vertices)
			{
				Color vertexColor = Global.EditorPreferences.VertexColor;
				if(_selectedVertices.Contains(vertex))
				{
					vertexColor = Global.EditorPreferences.SelectedVertexColor;
				}
				var normalizedX = vertex.X / _zoomLevel;
				var normalizedY = vertex.Y / _zoomLevel;
				_spriteBatch.DrawPoint(normalizedX, normalizedY, vertexColor, Global.VertexRenderSize);
			}

			// render edges
			foreach (var edge in _currentLevel.Edges)
			{
				Color edgeColor = Global.EditorPreferences.EdgeColor;
				//if (_selectedVertices.Contains(vertex))
				//{
				//	vertexColor = Global.EditorPreferences.SelectedVertexColor;
				//}
				var normalizedX1 = edge.A.X / _zoomLevel;
				var normalizedY1 = edge.A.Y / _zoomLevel;
				var normalizedX2 = edge.B.X / _zoomLevel;
				var normalizedY2 = edge.B.Y / _zoomLevel;
				_spriteBatch.DrawLine(normalizedX1, normalizedY1, normalizedX2, normalizedY2, edgeColor, Global.EdgeRenderSize);
			}

			// in in vertex or edge mode, render potentially placed vertex
			//if (_selectedTool == Tool.Vertex || _selectedTool == Tool.Edge)
			//{
			//	_spriteBatch.DrawPoint(_mouseState.X, _mouseState.Y, Color.Black, Global.VertexRenderSize);
			//}

			// in in edge mode, render potentially placed edge
			if (_selectedTool == Tool.Edge && _selectedVertices.Count == 1)
			{
				_spriteBatch.DrawLine(_selectedVertices[0].X, _selectedVertices[0].Y, _mouseState.X, _mouseState.Y, Color.Gray, Global.EdgeRenderSize);
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
