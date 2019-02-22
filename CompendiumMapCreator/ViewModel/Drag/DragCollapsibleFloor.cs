﻿using System;
using System.Drawing;
using CompendiumMapCreator.Data;
using CompendiumMapCreator.Edits;
using CompendiumMapCreator.Format;
using MColor = System.Windows.Media.Color;

namespace CompendiumMapCreator.ViewModel
{
	public class DragCollapsibleFloor : IDrag
	{
		private ImagePoint start;

		public Rectangle Selection { get; private set; }

		public MColor Color => CollapsibleFloor.DrawingColor.ToMediaColor();

		public DragCollapsibleFloor(ImagePoint start)
		{
			this.start = start;
		}

		public void Update(int x, int y, Project project) => this.Selection = Rectangle.FromLTRB(Math.Min(this.start.X, x), Math.Min(this.start.Y, y), Math.Max(this.start.X, x) + 1, Math.Max(this.start.Y, y) + 1);

		public (bool apply, Edit) End() => (true, new Add(new CollapsibleFloor(this.Selection.Width, this.Selection.Height) { X = this.Selection.Left, Y = this.Selection.Top }));
	}
}