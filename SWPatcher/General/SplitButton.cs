﻿/*
 * This file is part of Soulworker Patcher.
 * Copyright (C) 2016-2017 Miyu, Dramiel Leayal
 * 
 * Soulworker Patcher is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Soulworker Patcher is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Soulworker Patcher. If not, see <http://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SWPatcher.General
{
    internal class SplitButton : Button
    {
        [DefaultValue(null), Browsable(true),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        internal ContextMenuStrip ContextMenuStripSplit { get; set; }

        [DefaultValue(20), Browsable(true),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        internal int SplitWidth { get; set; }

        internal SplitButton()
        {
            SplitWidth = 20;
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            var splitRect = new Rectangle(Width - SplitWidth, 0, SplitWidth, Height);

            // Figure out if the button click was on the button itself or the menu split
            if (ContextMenuStripSplit != null && mevent.Button == MouseButtons.Left && splitRect.Contains(mevent.Location))
            {
                ContextMenuStripSplit.Show(this, 0, Height);    // Shows menu under button
                //Menu.Show(this, mevent.Location); // Shows menu at click location
            }
            else
            {
                base.OnMouseDown(mevent);
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            if (ContextMenuStripSplit != null && SplitWidth > 0)
            {
                // Draw the arrow glyph on the right side of the button
                int arrowX = ClientRectangle.Width - 14;
                int arrowY = ClientRectangle.Height / 2 - 1;

                Brush arrowBrush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
                Point[] arrows = new[] { new Point(arrowX, arrowY), new Point(arrowX + 7, arrowY), new Point(arrowX + 3, arrowY + 4) };
                pevent.Graphics.FillPolygon(arrowBrush, arrows);

                // Draw a dashed separator on the left of the arrow
                int lineX = ClientRectangle.Width - SplitWidth;
                int lineYFrom = arrowY - 4;
                int lineYTo = arrowY + 8;
                using (var separatorPen = new Pen(Brushes.DarkGray) { DashStyle = DashStyle.Dot })
                {
                    pevent.Graphics.DrawLine(separatorPen, lineX, lineYFrom, lineX, lineYTo);
                }
            }
        }
    }
}
