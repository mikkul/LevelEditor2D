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
using Myra.MML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
		private ObservableCollection<GameObject> _selectedObjects;
		private Point _lastClickPosition;
		private Stack<Level> _undoHistory;
		private Stack<Level> _redoHistory;

		public Level CurrentLevel
		{
			get
			{
				return _currentLevel;
			}
			private set
			{
				_currentLevel = value;
				_unmodifiedLevel = Level.Clone(_currentLevel);
				Global.InitIds(_currentLevel);
				_undoHistory.Clear();
				SaveLevelState();
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
			_selectedObjects = new ObservableCollection<GameObject>();
			_selectedObjects.Cleared += OnSelectedVerticesCleared;
			_selectedObjects.ItemAdded += OnSelectedVerticesItemAdded;
			_selectedObjects.ItemRemoved += OnSelectedVerticesItemRemoved;
			_undoHistory = new Stack<Level>();
			_redoHistory = new Stack<Level>();

			base.Initialize();
		}

		private void SaveLevelState()
		{
			Console.WriteLine("state modified");
			_undoHistory.Push(Level.Clone(CurrentLevel));
			_redoHistory.Clear();
		}

		private void OnSelectedVerticesItemRemoved(object sender, ItemEventArgs<GameObject> e)
		{
			if (_selectedObjects.Count == 1)
			{
				_objectPropertiesPropGrid.Object = _selectedObjects[0];
			}
			else
			{
				_objectPropertiesPropGrid.Object = null;
			}
			
			if(_selectedObjects.Count == 0)
			{
				var deleteButton = GetWidget<TextButton>("properties-delete-object");
				deleteButton.Visible = false;
			}
		}

		private void OnSelectedVerticesItemAdded(object sender, ItemEventArgs<GameObject> e)
		{
			if(_selectedObjects.Count == 1)
			{
				_objectPropertiesPropGrid.Object = _selectedObjects[0];
			}
			else
			{
				_objectPropertiesPropGrid.Object = null;
			}

			if(_selectedObjects.Count >= 1)
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

			var menuEditUndo = _topMenu.FindMenuItemById("menu-edit-undo");
			menuEditUndo.Selected += OnEditUndo;

			var menuEditRedo = _topMenu.FindMenuItemById("menu-edit-redo");
			menuEditRedo.Selected += OnEditRedo;

			var menuEditPreferences = _topMenu.FindMenuItemById("menu-edit-preferences");
			menuEditPreferences.Selected += OnEditPreferencesClicked;

			var mainSplitPane = GetWidget<HorizontalSplitPane>("main-split-pane");
			mainSplitPane.SetSplitterPosition(0, Global.EditorPreferences.LeftSplitterPosition);
			mainSplitPane.SetSplitterPosition(1, Global.EditorPreferences.RightSplitterPosition);
			mainSplitPane.ProportionsChanged += OnMainSplitPaneProportionsChanged;

			OnEditorPreferencesSidebarBackgroundColorChanged(null, EventArgs.Empty);

			Global.EditorPreferences.SidebarBackgroundChanged += OnEditorPreferencesSidebarBackgroundColorChanged;

			var toolbarPanel = GetWidget<VerticalStackPanel>("toolbar-panel");
			Global.EditorPreferences.ToolbarButtonBackgroundChanged += OnEditorPreferencesToolbarBackgroundChanged;
			Global.EditorPreferences.ToolbarButtonHoverBackgroundChanged += OnEditorPreferencesToolbarHoverBackgroundChanged;
			OnEditorPreferencesToolbarBackgroundChanged(null, EventArgs.Empty);
			OnEditorPreferencesToolbarHoverBackgroundChanged(null, EventArgs.Empty);
			OnToolSelected((TextButton)toolbarPanel.Widgets[0]);

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

		private void OnEditRedo(object sender, EventArgs e)
		{
			if (_redoHistory.Count == 0)
			{
				return;
			}

			Console.WriteLine("redo");
			_currentLevel = _redoHistory.Pop();
			_undoHistory.Push(Level.Clone(_currentLevel));
		}

		private void OnEditUndo(object sender, EventArgs e)
		{
			if(_undoHistory.Count == 0)
			{
				return;
			}
			Console.WriteLine("undo");
			_currentLevel = _undoHistory.Pop();
			_redoHistory.Push(Level.Clone(_currentLevel));
		}

		private void OnEditorPreferencesToolbarHoverBackgroundChanged(object sender, EventArgs e)
		{
			var toolbarPanel = GetWidget<VerticalStackPanel>("toolbar-panel");
			foreach (TextButton button in toolbarPanel.Widgets)
			{
				button.OverBackground = new SolidBrush(Global.EditorPreferences.ToolbarButtonHoverBackground);
			}
		}

		private void OnEditorPreferencesToolbarBackgroundChanged(object sender, EventArgs e)
		{
			var toolbarPanel = GetWidget<VerticalStackPanel>("toolbar-panel");
			foreach (TextButton button in toolbarPanel.Widgets)
			{
				button.Background = new SolidBrush(Global.EditorPreferences.ToolbarButtonBackground);
			}
		}

		private void OnPropertiesDeleteObjectButtonClicked(object sender, EventArgs e)
		{
			foreach (var gameObject in _selectedObjects)
			{
				CurrentLevel.Objects.Remove(gameObject);
				if(gameObject is Vertex vertex)
				{
					DeleteVertex(vertex);
				}
				else if(gameObject is Edge edge)
				{
					DeleteEdge(edge);
				}
			}
			_selectedObjects.Clear();
			SaveLevelState();
		}

		private void DeleteEdge(Edge edge)
		{
			var affectedVertices = CurrentLevel.Objects
				.Where(x =>
				{
					if (x is Vertex vertex)
					{
						return edge.A.Equals(vertex) || edge.B.Equals(vertex);
					}
					return false;
				})
				.ToList();
			foreach (Vertex vertex in affectedVertices)
			{
				if(vertex.ConnectedEdges.Count > 1)
				{
					CurrentLevel.Objects.Remove(vertex);
				}
				else
				{
					vertex.ConnectedEdges.Remove(edge);
				}
			}
		}

		private void DeleteVertex(Vertex vertex)
		{
			var affectedEdges = CurrentLevel.Objects
				.Where(x =>
				{
					if(x is Edge edge)
					{
						return edge.A.Equals(vertex) || edge.B.Equals(vertex);
					}
					return false;
				})
				.ToList();
			foreach (var edge in affectedEdges)
			{
				CurrentLevel.Objects.Remove(edge);
			}
		}

		private void OnToolEdgeClicked(object sender, EventArgs e)
		{
			OnToolSelected((TextButton)sender);
			_selectedTool = Tool.Edge;
			Console.WriteLine(_selectedTool);
		}

		private void OnToolVertexClicked(object sender, EventArgs e)
		{
			OnToolSelected((TextButton)sender);
			_selectedTool = Tool.Vertex;
			Console.WriteLine(_selectedTool);
		}

		private void OnToolSelectClicked(object sender, EventArgs e)
		{
			OnToolSelected((TextButton)sender);
			_selectedTool = Tool.Select;
			Console.WriteLine(_selectedTool);
		}

		private void OnToolSelected(TextButton toolButton)
		{
			var toolbarPanel = GetWidget<VerticalStackPanel>("toolbar-panel");
			foreach (TextButton button in toolbarPanel.Widgets)
			{
				button.Background = new SolidBrush(Global.EditorPreferences.ToolbarButtonBackground);
			}
			toolButton.Background = new SolidBrush(Global.EditorPreferences.ToolbarButtonSelectedBackground);
		}

		private void OnEditorPreferencesSidebarBackgroundColorChanged(object sender, EventArgs e)
		{
			var toolbarPanel = GetWidget<VerticalStackPanel>("toolbar-panel");
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
				_selectedObjects.Clear();
			}

			if(_keyboardState.IsControlDown())
			{
				if(_keyboardState.WasKeyJustUp(Keys.Z))
				{
					OnEditUndo(null, EventArgs.Empty);
				}
				else if(_keyboardState.WasKeyJustUp(Keys.Y))
				{
					OnEditRedo(null, EventArgs.Empty);
				}
			}

			//if (_mouseState.WasButtonJustDown(MouseButton.Left))
			//{
			//	HandleTools(_mouseState, _keyboardState);
			//}
			HandleTools();
		}

		private void HandleTools()
		{
			if (!_editorAreaPanel.ActualBounds.Contains(_mouseState.Position) || _topMenu.IsOpen || _isPopupActive)
			{
				return;
			}

			var worldX = _mouseState.Position.X * _zoomLevel;
			var worldY = _mouseState.Position.Y * _zoomLevel;

			if (_selectedTool == Tool.Select)
			{
				HandleSelectTool(worldX, worldY);
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
			if(!_mouseState.WasButtonJustDown(MouseButton.Left))
			{
				return;
			}

			var vertex = Vertex.Create(worldX, worldY);
			_currentLevel.Objects.Add(vertex);

			if(_selectedObjects.Count == 1 && _selectedObjects[0] is Vertex selectedVertex)
			{
				var edge = Edge.Create(selectedVertex, vertex);
				_currentLevel.Objects.Add(edge);
				vertex.ConnectedEdges.Add(edge);
				selectedVertex.ConnectedEdges.Add(edge);
			}

			_selectedObjects.Clear();
			_selectedObjects.Add(vertex);
			Console.WriteLine(vertex);
			SaveLevelState();
		}

		private void HandleVertexTool(float worldX, float worldY)
		{
			if (!_mouseState.WasButtonJustDown(MouseButton.Left))
			{
				return;
			}

			var vertex = Vertex.Create(worldX, worldY);
			_currentLevel.Objects.Add(vertex);
			_selectedObjects.Clear();
			_selectedObjects.Add(vertex);
			Console.WriteLine(vertex);
			SaveLevelState();
		}

		private void HandleSelectTool(float worldX, float worldY)
		{
			if (_mouseState.WasButtonJustDown(MouseButton.Left))
			{
				SelectToolSingle(worldX, worldY);
				_lastClickPosition = _mouseState.Position;
			}
			else if(_mouseState.WasButtonJustUp(MouseButton.Left) && _mouseState.Position != _lastClickPosition)
			{
				SelectToolDrag(worldX, worldY);
			}
		}

		private void SelectToolDrag(float worldX, float worldY)
		{
			float lastClickPositionWorldX = _lastClickPosition.X * _zoomLevel;
			float lastClickPositionWorldY = _lastClickPosition.Y * _zoomLevel;
			float rectX = Math.Min(lastClickPositionWorldX, worldX);
			float rectY = Math.Min(lastClickPositionWorldY, worldY);
			float rectWidth = Math.Max(lastClickPositionWorldX - worldX, worldX - lastClickPositionWorldX);
			float rectHeight = Math.Max(lastClickPositionWorldY - worldY, worldY - lastClickPositionWorldY);
			var rect = new RectangleF(rectX, rectY, rectWidth, rectHeight);
			var selectedObjects = CurrentLevel.Objects.Where(x =>
			{
				if(x is Vertex vertex)
				{
					return rect.Contains(new Point2(vertex.X, vertex.Y));
				}
				return false;
			});
			_selectedObjects.Clear();
			foreach (var selectedObject in selectedObjects)
			{
				_selectedObjects.Add(selectedObject);
			}
		}

		private void SelectToolSingle(float worldX, float worldY)
		{
			var selectMultiple = _keyboardState.IsKeyDown(Keys.LeftShift) || _keyboardState.IsKeyDown(Keys.RightShift);

			var selectedVertex = CurrentLevel.Objects.FirstOrDefault(x =>
			{
				if (x is Vertex vertex)
				{
					var deltaX = Math.Abs(vertex.X - worldX);
					var deltaY = Math.Abs(vertex.Y - worldY);
					var dist = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
					return dist <= Global.VertexSelectionDistanceTolerance;
				}
				return false;
			});

			if (selectedVertex != null)
			{
				if (_selectedObjects.Contains(selectedVertex))
				{
					_selectedObjects.Remove(selectedVertex);
				}
				else
				{
					if (!selectMultiple)
					{
						_selectedObjects.Clear();
					}
					_selectedObjects.Add(selectedVertex);
				}
				return;
			}

			var selectedEdge = CurrentLevel.Objects.FirstOrDefault(x =>
			{
				if (x is Edge edge)
				{
					var deltaX1 = Math.Abs(edge.A.X - worldX);
					var deltaY1 = Math.Abs(edge.A.Y - worldY);
					var deltaX2 = Math.Abs(edge.B.X - worldX);
					var deltaY2 = Math.Abs(edge.B.Y - worldY);
					var dist1 = Math.Sqrt(deltaX1 * deltaX1 + deltaY1 * deltaY1);
					var dist2 = Math.Sqrt(deltaX2 * deltaX2 + deltaY2 * deltaY2);
					var edgeDistX = Math.Abs(edge.A.X - edge.B.X);
					var edgeDistY = Math.Abs(edge.A.Y - edge.B.Y);
					var edgeLength = Math.Sqrt(edgeDistX * edgeDistX + edgeDistY * edgeDistY);
					return Math.Abs(dist1 + dist2 - edgeLength) <= Global.EdgeSelectionDistanceTolerance;
				}
				return false;
			});

			if (selectedEdge != null)
			{
				if (_selectedObjects.Contains(selectedEdge))
				{
					_selectedObjects.Remove(selectedEdge);
				}
				else
				{
					if (!selectMultiple)
					{
						_selectedObjects.Clear();
					}
					_selectedObjects.Add(selectedEdge);
				}
				return;
			}

			_selectedObjects.Clear();
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
			foreach (var gameObject in _currentLevel.Objects)
			{
				if(gameObject is Vertex vertex)
				{
					Color vertexColor = Global.EditorPreferences.VertexColor;
					if (_selectedObjects.Contains(vertex))
					{
						vertexColor = Global.EditorPreferences.SelectedVertexColor;
					}
					var normalizedX = vertex.X / _zoomLevel;
					var normalizedY = vertex.Y / _zoomLevel;
					_spriteBatch.DrawPoint(normalizedX, normalizedY, vertexColor, Global.VertexRenderSize);
				}
				else if(gameObject is Edge edge)
				{
					Color edgeColor = Global.EditorPreferences.EdgeColor;
					if (_selectedObjects.Contains(edge))
					{
						edgeColor = Global.EditorPreferences.SelectedEdgeColor;
					}
					var normalizedX1 = edge.A.X / _zoomLevel;
					var normalizedY1 = edge.A.Y / _zoomLevel;
					var normalizedX2 = edge.B.X / _zoomLevel;
					var normalizedY2 = edge.B.Y / _zoomLevel;
					_spriteBatch.DrawLine(normalizedX1, normalizedY1, normalizedX2, normalizedY2, edgeColor, Global.EdgeRenderSize);
				}
			}

			// in in vertex or edge mode, render potentially placed vertex
			//if (_selectedTool == Tool.Vertex || _selectedTool == Tool.Edge)
			//{
			//	_spriteBatch.DrawPoint(_mouseState.X, _mouseState.Y, Color.Black, Global.VertexRenderSize);
			//}

			// in in edge mode, render potentially placed edge
			if (_selectedTool == Tool.Edge && _selectedObjects.Count == 1 && _selectedObjects[0] is Vertex selectedVertex)
			{
				_spriteBatch.DrawLine(selectedVertex.X, selectedVertex.Y, _mouseState.X, _mouseState.Y, Color.Gray, Global.EdgeRenderSize);
			}

			// render selection rectangle
			if(_selectedTool == Tool.Select && _mouseState.IsButtonDown(MouseButton.Left))
			{
				float rectX = Math.Min(_lastClickPosition.X, _mouseState.Position.X);
				float rectY = Math.Min(_lastClickPosition.Y, _mouseState.Position.Y);
				float rectWidth = Math.Max(_lastClickPosition.X - _mouseState.Position.X, _mouseState.Position.X - _lastClickPosition.X);
				float rectHeight = Math.Max(_lastClickPosition.Y - _mouseState.Position.Y, _mouseState.Position.Y - _lastClickPosition.Y);
				var rect = new RectangleF(rectX, rectY, rectWidth, rectHeight);
				_spriteBatch.DrawRectangle(rect, Color.White, 1);
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
