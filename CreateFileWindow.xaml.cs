using System.Windows;

namespace FileManager
{
    public enum CreationType
    {
        Folder,
        File
    }
    /// <summary>
    /// Interaction logic for CreateFile.xaml
    /// </summary>
    public partial class CreateFileWindow : Window
    {
        public string Result;
        public CreateFileWindow(CreationType type)
        {
            InitializeComponent();
            if (type == CreationType.File)
                this.Title = "Create File";
            else
                this.Title = "Create Folder";
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Result = tbname.Text.Trim();
            bool p = !string.IsNullOrEmpty(Result);
            this.DialogResult = p;
        }
    }
}
