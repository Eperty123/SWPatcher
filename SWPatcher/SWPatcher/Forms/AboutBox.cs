﻿using System;
using System.Windows.Forms;
using SWPatcher.Helpers.GlobalVar;

namespace SWPatcher.Forms
{
    partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyAccessor.Title);
            this.labelProductName.Text = AssemblyAccessor.Product;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyAccessor.Version);
            this.textBoxDescription.Text = AssemblyAccessor.Description;
            this.linkLabelWebsite.Links.Add(0, 17, Uris.SWHQWebsite);
        }

        private void linkLabelWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabelWebsite.LinkVisited = true;
            System.Diagnostics.Process.Start(Uris.SWHQWebsite);
        }
    }
}