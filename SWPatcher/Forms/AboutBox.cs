/*
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

using SWPatcher.Helpers;
using SWPatcher.Helpers.GlobalVariables;
using System;
using System.Windows.Forms;

namespace SWPatcher.Forms
{
    internal partial class AboutBox : Form
    {
        private int ImagesCount = 72;

        internal AboutBox()
        {
            InitializeComponent();
            InitializeTextComponent();
        }

        private void InitializeTextComponent()
        {
            ButtonOk.Text = StringLoader.GetText("button_ok");
            Text = $"About {AssemblyAccessor.Title}";
            LabelProductName.Text = AssemblyAccessor.Product;
            LabelVersion.Text = $"Version {AssemblyAccessor.Version}";
            LabelCopyright.Text += "\nFork by Asaduji && Eperty123";
            TextBoxDescription.Text = StringLoader.GetText("patcher_description");
            LinkLabelWebsite.Links.Add(0, LinkLabelWebsite.Text.Length, Urls.SoulworkerWebsite);
            LogoPictureBox.ImageLocation = $"https://raw.githubusercontent.com/Miyuyami/SWPatcher/master/Images/{(new Random()).Next(ImagesCount) + 1}.png";
        }

        private void LinkLabelWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LinkLabelWebsite.LinkVisited = true;
            System.Diagnostics.Process.Start(Urls.SoulworkerWebsite);
        }

        private void LabelCopyright_Click(object sender, EventArgs e)
        {
        }

        private void LabelProductName_Click(object sender, EventArgs e)
        {
        }

        private void TableLayoutPanel_Paint(object sender, PaintEventArgs e)
        {
        }

        private void forkLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            forkLinkLabel.LinkVisited = true;
            System.Diagnostics.Process.Start(Urls.ForkWebsite);
        }
    }
}