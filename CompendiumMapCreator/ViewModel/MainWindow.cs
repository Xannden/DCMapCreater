﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CompendiumMapCreator.Data;
using CompendiumMapCreator.Edits;
using CompendiumMapCreator.Format;
using Microsoft.Win32;
using MBrush = System.Windows.Media.Brush;
using MColor = System.Windows.Media.Color;

namespace CompendiumMapCreator.ViewModel
{
	public class MainWindow : INotifyPropertyChanged
	{
		private IDrag dragging;
		private string imageDir;
		private Project project;
		private string projectDir;
		private ToolVM selectedTool;

		public MainWindow()
		{
			this.Editing.Closing += (text, label) => this.Project?.AddEdit(new ChangeLabel(label, text));
			this.ToolList = App.Config.GetTools();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public bool AddLegend { get; set; } = true;

		public Editing Editing { get; } = new Editing();

		public Project Project
		{
			get => this.project;

			set
			{
				if (this.project != null)
				{
					this.project.PropertyChanged -= this.ProjectChanged;
					this.project.Edits.CollectionChanged -= this.ProjectEditsChanged;
				}

				this.project = value;
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Project)));
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Title)));

				this.project.PropertyChanged += this.ProjectChanged;
				this.project.Edits.CollectionChanged += this.ProjectEditsChanged;
			}
		}

		public ToolVM SelectedTool
		{
			get => this.selectedTool;

			set
			{
				this.selectedTool = value;
				this.selectedTool.IsSelected = true;
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedTool)));
			}
		}

		public Rectangle Selection => this.dragging?.Selection ?? new Rectangle(0, 0, 0, 0);

		public MBrush SelectionFill
		{
			get
			{
				MColor color = this.dragging?.Color ?? Colors.Transparent;

				color.A = 60;

				return new SolidColorBrush(color);
			}
		}

		public MBrush SelectionStroke
		{
			get
			{
				MColor color = this.dragging?.Color ?? Colors.Transparent;

				color.A = 255;

				return new SolidColorBrush(color);
			}
		}

		public string Title
		{
			get
			{
				StringBuilder builder = new StringBuilder("DDO Compendium Map Creator");

				if (!string.IsNullOrEmpty(this.Project?.Title))
				{
					builder.Append(" - ");
					builder.Append(this.Project.Title);
				}

				if (!string.IsNullOrEmpty(this.Project?.File))
				{
					builder.Append(" - ");
					builder.Append(this.Project.File);

					if (this.Project?.HasUnsaved() ?? false)
					{
						builder.Append("*");
					}
				}

				return builder.ToString();
			}
		}

		public List<ToolVM> ToolList { get; }

		public void AddElement(ElementVM element)
		{
			if (this.Project?.Image == null)
			{
				return;
			}

			this.Project.AddEdit(new Add(element));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Title)));
		}

		public void ChangeImage()
		{
			if (this.Project == null)
			{
				return;
			}

			OpenFileDialog dialog = new OpenFileDialog
			{
				DefaultExt = ".png",
				Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
				InitialDirectory = this.imageDir,
			};

			bool? result = dialog.ShowDialog();

			if (result.GetValueOrDefault())
			{
				this.imageDir = Path.GetDirectoryName(dialog.FileName);

				try
				{
					this.Project.AddEdit(new ChangeMap(this.Project, new Image(File.ReadAllBytes(dialog.FileName))));

					this.SelectedTool = ToolVM.Cursor;
				}
				catch (Exception)
				{
					MessageBox.Show("Unable to load image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		public bool Changing(Window owner)
		{
			if (this.Project?.HasUnsaved() ?? false)
			{
				MessageBoxResult result = MessageBox.Show(owner, "Do you want to save changes?", "DDO Compendium Map Creator", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);

				if (result == MessageBoxResult.Yes)
				{
					this.SaveProject();
				}
				else if (result == MessageBoxResult.Cancel)
				{
					return true;
				}
			}

			return false;
		}

		public void Click(ImagePoint p)
		{
			if (this.Project == null)
			{
				return;
			}

			if (this.SelectedTool != ToolVM.Cursor && this.SelectedTool.ToolElement.HasValue)
			{
				ElementVM element = ElementVM.CreateElement(this.SelectedTool.ToolElement.Value);

				ImagePoint position = p - new ImagePoint(element.Width / 2, element.Height / 2);

				element.X = position.X;
				element.Y = position.Y;

				if (element is NumberedElementVM ne)
				{
					ne.Number = this.project.Elements.Count((e) => e.Id == ne.Id && !e.IsCopy);
				}

				this.AddElement(element);
				this.Project.Selected.Clear();
				this.Project.OnPropertyChanged(nameof(this.Project.Selected));
			}
			else
			{
				this.Project.Select(p, !Keyboard.IsKeyDown(Key.LeftCtrl));
			}
		}

		public void Delete()
		{
			if (this.Project?.Selected.Count == 0)
			{
				return;
			}

			this.Project?.AddEdit(new Remove(this.Project.Selected, this.project.Elements));
			this.Project?.Selected.Clear();
			this.Project?.OnPropertyChanged(nameof(this.Project.Selected));
		}

		public void Deselect()
		{
			this.Project?.Selected.Clear();
			this.Project?.OnPropertyChanged(nameof(this.Project.Selected));
		}

		public void DragEnd()
		{
			(bool apply, Edit element) = this.dragging?.End() ?? (false, null);

			if (element != null)
			{
				this.Project.AddEdit(element, apply);
			}

			this.dragging = null;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Selection)));
		}

		public void DragStart(ImagePoint p)
		{
			if (this.Project == null)
			{
				return;
			}

			if (Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				return;
			}

			if (!Keyboard.IsKeyDown(Key.Space))
			{
				if (this.SelectedTool.IsArea)
				{
					this.dragging = new DragAreaElement(p, this.SelectedTool.ToolElement.Value);
				}
				else
				{
					if (!this.Project.Selected.Any(e => e.Contains(p)))
					{
						this.Project.Select(p);
					}

					if (this.Project.Selected.Count != 0)
					{
						this.dragging = new DragMove(new List<ElementVM>(this.Project.Selected), p);
					}
					else if (this.SelectedTool == ToolVM.Cursor)
					{
						this.dragging = new DragSelect(p);
					}
				}

				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectionStroke)));
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectionFill)));
			}
		}

		public void DragUpdate(ImagePoint p)
		{
			this.dragging?.Update(p.X, p.Y, this.Project);
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Selection)));
		}

		public void Edit(WindowPoint p)
		{
			if (this.Project?.Selected.Count != 1 || !(this.Project.Selected[0] is LabelElementVM))
			{
				return;
			}

			this.Editing.Start(p, (LabelElementVM)this.Project.Selected[0]);
		}

		public void Export()
		{
			this.Project?.Export(this.AddLegend, ref this.imageDir);
		}

		public void LoadImage(Window window)
		{
			if (this.Changing(window))
			{
				return;
			}

			OpenFileDialog dialog = new OpenFileDialog
			{
				DefaultExt = ".png",
				Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
				InitialDirectory = this.imageDir,
			};

			bool? result = dialog.ShowDialog();

			if (result.GetValueOrDefault())
			{
				this.imageDir = Path.GetDirectoryName(dialog.FileName);

				try
				{
					this.Project = Project.FromImage(new Image(File.ReadAllBytes(dialog.FileName)));

					this.SelectedTool = ToolVM.Cursor;
				}
				catch (Exception)
				{
					MessageBox.Show("Unable to load image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		public void LoadProject(Window window)
		{
			if (this.Changing(window))
			{
				return;
			}

			Project result = Project.Load(ref this.projectDir);
			if (result != null)
			{
				this.Project = result;
				this.Project.Edits.Clear();
				this.SelectedTool = ToolVM.Cursor;
			}
		}

		public void Redo()
		{
			this.Project?.Redo();
		}

		public void RotateClockwise()
		{
			if (this.Project.Selected.Count != 1 || !this.Project.Selected[0].CanRotate)
			{
				return;
			}

			this.Project.AddEdit(new Rotate(this.Project.Selected[0], clockwise: true));
		}

		public void RotateCounterClockwise()
		{
			if (this.Project.Selected.Count != 1 || !this.Project.Selected[0].CanRotate)
			{
				return;
			}

			this.Project.AddEdit(new Rotate(this.Project.Selected[0], clockwise: false));
		}

		public void SaveProject(bool forcePrompt = false)
		{
			this.Project?.Save(ref this.projectDir, forcePrompt);
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Title)));
		}

		public void SelectAll()
		{
			this.Project?.Selected.Clear();

			for (int i = 0; i < this.Project.Elements?.Count; i++)
			{
				this.Project.Selected.Add(this.Project.Elements[i]);
			}

			this.Project?.OnPropertyChanged(nameof(this.Project.Selected));
		}

		public void SetTitle(string title)
		{
			this.Project.Title = title;
		}

		public void SetTool(ToolVM tool)
		{
			this.SelectedTool = tool;
			this.Project?.Selected.Clear();
			this.Project?.OnPropertyChanged(nameof(this.Project.Selected));
		}

		public void Undo()
		{
			this.Project?.Undo();
		}

		private void ProjectChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "File" || e.PropertyName == "Title")
			{
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Title)));
			}
		}

		private void ProjectEditsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Title)));
		}
	}
}