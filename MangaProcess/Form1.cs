using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MangaSplitter.Configuration;
using MangaSplitter.MangaStructure;
using MangaSplitter.MangaProcess;

namespace MangaSplitter
{
    public partial class MangaInitializer : Form
    {
        private string chosenDirectoryPath = "";


        BackgroundWorker bw = new BackgroundWorker();

        public MangaInitializer()
        {
            InitializeComponent();
        }

        private void PathChooser(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Choose the folder containing EDI files";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                chosenDirectoryPath = fbd.SelectedPath;
                this.PathDisplay.Text = fbd.SelectedPath;
            }
        }

        private void Submit_Click(object sender, EventArgs e)
        {
            WebValidator test = new WebValidator();

            try
            {
                ProcessLink(WebURL.Text, PathDisplay.Text);

                DialogResult dialogResult = MessageBox.Show("Continue Ripping Manga?", "Manga Ripper", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    return;
                }
                else if (dialogResult == DialogResult.No)
                {
                    Application.Exit();
                }
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }

            return;
        }

        private void ProcessLink(string inputLink, string folderPath)
        {
            MangaFactory fact = new MangaFactory(inputLink, folderPath);
            fact.Process();
            return;
        }
    }
}