using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms; // Required for FolderBrowserDialog

namespace VerteMarkPackager {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
		}

		private void OnBrowseDicomFilesDirectory(object sender, RoutedEventArgs e) {
			using (var folderBrowser = new FolderBrowserDialog()) {
				folderBrowser.Description = "Vyberte složku s DICOM soubory";
				folderBrowser.ShowNewFolderButton = false;

				if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					DicomFilesDirectoryTextBox.Text = folderBrowser.SelectedPath;
				}
			}
		}

		private void OnBrowseSaveDirectory(object sender, RoutedEventArgs e) {
			using (var folderBrowser = new FolderBrowserDialog()) {
				folderBrowser.Description = "Vyberte složku pro uložení";
				folderBrowser.ShowNewFolderButton = true;

				if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					SaveDirectoryTextBox.Text = folderBrowser.SelectedPath;
				}
			}
		}

		private void DicomFilesCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if (DicomFilesCountTextBox != null) {
				DicomFilesCountTextBox.Text = DicomFilesCountSlider.Value.ToString("F0");
			}
		}


		private void OnDicomFilesCountTextChanged(object sender, TextChangedEventArgs e) {
			if (int.TryParse(DicomFilesCountTextBox.Text, out int value)) {
				if (value >= DicomFilesCountSlider.Minimum && value <= DicomFilesCountSlider.Maximum) {
					DicomFilesCountSlider.Value = value;
				}
				else {
					System.Windows.MessageBox.Show($"Prosím, vyberte hodnotu pouze mezi {DicomFilesCountSlider.Minimum} a {DicomFilesCountSlider.Maximum}. Pro neomezený výběr zvolte hodnotu 0.", "Chybná hodnota", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}


		private void OnCreateButtonClick(object sender, RoutedEventArgs e) {
			if (!int.TryParse(DicomFilesCountTextBox.Text, out int value)) {
				System.Windows.MessageBox.Show($"Prosím, vyberte hodnotu pouze mezi {DicomFilesCountSlider.Minimum} a {DicomFilesCountSlider.Maximum}. Pro neomezený výběr zvolte hodnotu 0.", "Chybná hodnota", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			else {
				System.Windows.MessageBox.Show("Vytvoření úspěšné!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
	}
}