using System;
using System.Collections.Generic;
using System.IO;
using WinForms = System.Windows.Forms;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    class RandomTextFromFolderProvider
    {
        private WinForms.FolderBrowserDialog openFileDialog;
        private List<string> files;
        private Random randomIndexGenerator;

        public string SelectedFolder;
        public Boolean IsDone;

        public RandomTextFromFolderProvider(string rootFolder)
        {
            this.openFileDialog = new WinForms.FolderBrowserDialog();
            this.openFileDialog.SelectedPath = rootFolder;
            this.randomIndexGenerator = new Random();
            this.IsDone = false;
        }

        private WinForms.DialogResult OpenDialog()
        {
            WinForms.DialogResult result = this.openFileDialog.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                this.SelectedFolder = this.openFileDialog.SelectedPath;
                string[] files = Directory.GetFiles(this.SelectedFolder);
                this.files = new List<string>();
                foreach (string file in files)
                {
                    this.files.Add(file);
                    Console.WriteLine(file);
                }
            }

            return result;
        }

        public string GetNextRandomText()
        {
            if (this.files == null)
            {
                if (this.OpenDialog() != WinForms.DialogResult.OK)
                {
                    return "";
                }
            }

            if (this.files.Count <= 0)
            {
                Console.WriteLine("WARNING: File list already empty");
                return "";
            }

            int randIndex = this.randomIndexGenerator.Next(0, this.files.Count);
            string fileContents = File.ReadAllText(this.files[randIndex]);
            this.files.RemoveAt(randIndex);

            if (this.files.Count <= 0)
            {
                this.IsDone = true;
            }

            return fileContents;
        }
    }
}
