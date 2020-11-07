﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CompendiumMapCreator.Data;
using CompendiumMapCreator.ViewModel;

namespace CompendiumMapCreator.Format.Export
{
	public class Legend : IDrawable
	{
		private const int ImageCenterX = ImageWidth / 2;
		private const int ImageCenterY = ImageHeight / 2;
		private const int ImageHeight = 14;
		private const int ImageWidth = 18;
		private const int LineHeight = 20;
		private const int TextX = XOffset + ImageWidth + 2;
		private const int XOffset = 10;
		private readonly bool addLegend;
		private readonly IList<ElementVM> elements;
		private readonly Font font;
		private bool hasPossible;
		private Size size;
		private List<ElementData> types;

		public Legend(Font font, IList<ElementVM> elements, bool addLegend)
		{
			this.font = font;
			this.elements = elements;
			this.addLegend = addLegend;
			this.hasPossible = false;
		}

		public void Draw(Graphics g, Point p)
		{
			if (!this.addLegend)
			{
				return;
			}

			int x = p.X + XOffset;
			int y = p.Y;

			for (int i = 0; i < this.types.Count; i++)
			{
				Image image = Image.GetImageFromElementId(this.types[i].Id);

				g.DrawImage(image.DrawingImage, x + (ImageCenterX - (image.Width / 2)), y + (ImageCenterY - (image.Height / 2)));

				string name;

				if (this.types[i].ExportName != null)
				{
					name = this.types[i].ExportName;
				}
				else
				{
					name = this.types[i].Name;
				}

				g.DrawString(name, this.font, Brushes.White, TextX, y);

				y += LineHeight;
			}

			if (this.hasPossible)
			{
				Image image = Image.GetImageFromFileName("possLoc");

				g.DrawImage(image.DrawingImage, x + (ImageCenterX - (image.Width / 2)), y + (ImageCenterY - (image.Height / 2)));
				g.DrawString("Possible Location", this.font, Brushes.White, TextX, y);
			}

			g.DrawVerticalLine(p.X + this.size.Width - 2, p.Y - 1, p.Y + this.size.Height + 1);
		}

		public Size Layout(int width, int height)
		{
			if (!this.addLegend)
			{
				return default;
			}

			this.types = this.elements.Select(t => t.Element).Where(t => !t.Hidden).Distinct().ToList();

			this.types.Sort((l, r) => l.Order.CompareTo(r.Order));

			if (this.elements.Any((e) => e.Optional))
			{
				this.hasPossible = true;
			}

			this.size = new Size(150, (this.types.Count * LineHeight) + (this.hasPossible ? LineHeight : 0));

			return this.size;
		}
	}
}