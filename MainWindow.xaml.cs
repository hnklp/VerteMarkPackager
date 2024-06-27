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
using System.Windows.Forms;
using System.IO; // Required for FolderBrowserDialog
using FellowOakDicom;
using FellowOakDicom.IO;
using System;
using System.Diagnostics;

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

		/// <summary>
		/// Spuštění backendu
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCreateButtonClick(object sender, RoutedEventArgs e) {
			if (!int.TryParse(DicomFilesCountTextBox.Text, out int value)) {
				System.Windows.MessageBox.Show($"Prosím, vyberte hodnotu pouze mezi {DicomFilesCountSlider.Minimum} a {DicomFilesCountSlider.Maximum}. Pro neomezený výběr zvolte hodnotu 0.", "Chybná hodnota", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			else {
				int done = Start();
				switch (done) {
					case 0:
						System.Windows.MessageBox.Show("Vytvoření úspěšné!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
						break;
					case 1:
						System.Windows.MessageBox.Show("Vybrané adresáře neexistují! Prosím, vyberte existující adresáře.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
						break;
					case 2:
						System.Windows.MessageBox.Show("Ve vybraném adresáři nejsou žádné soubory dicom.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
						break;
				}
			}
		}

		private void HelpMenuItem_Click(object sender, RoutedEventArgs e) {
			string helpText = "1. Vyberte adresář (složku), kde se nachází všechny dicom soubory ke zpracování.\n\n" +
							  "2. Vyberte adresář, kde se vytvoří složka s názvem 'VerteMarkPack' se všemi vytvořenými soubory \".vmk\".\n\n" +
							  "3. Zvolte, po kolika dicom souborech se budou tvořit jednotlivé soubory \".vmk\" (kolik souborů bude uvnitř). Pro vytvoření pouze jednoho souboru zvolte počet \"0\".\n\n" +
							  "4. Tlačítkem \"Vytvořit\" spustíte tvoření projektových souborů. Upozornění: Vytváření může při větším počtu dicom souborů chvilku trvat.";
			System.Windows.MessageBox.Show(helpText, "Nápověda", MessageBoxButton.OK, MessageBoxImage.Question);
		}

		private void InfoMenuItem_Click(object sender, RoutedEventArgs e) {
			string infoText = "Tato aplikace slouží pro vytvoření souborů se správnou strukturou a koncovkou pro správné pracování aplikace VerteMark.\n\n" +
										 "Z adresáře, kde se nacházejí soubory typu \"Dicom\" vytvoří soubory pro aplikaci VerteMark obsahující určitý počet souborů \"Dicom\". " +
										 "Tyto vytvořené soubory se uloží v nové složce s názvem 'VerteMarkPack' v adresáři, který si uživatel zvolí.\n\n" +
										 "V případě chyb, nesprávné práce aplikace nebo podrobnější nápovědy, prosím kontaktujte vývojáře této aplikace skrze email: schonf.alex@gmail.com.";
			System.Windows.MessageBox.Show(infoText, "Informace", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		/*========================================================
		BACKEND
		========================================================*/

		private int Start() {
			if (!CheckFolder(SaveDirectoryTextBox.Text) && !CheckFolder(DicomFilesDirectoryTextBox.Text)) {
				return 1;
			}
			if (!CheckDicoms()) {
				return 2;
			}


			return 0;
		}





		// CHECKING
		private bool CheckFolder(string path) {
			return Directory.Exists(path);
		}

		private bool CheckDicoms() {
			var files = Directory.GetFiles(DicomFilesDirectoryTextBox.Text);

			// Iterace přes každý soubor a kontrola, zda je to DICOM soubor
			foreach (var file in files) {
				// Kontrola hlavičky souboru
				if (IsDicomFile(file)) {
					return true;
				}
			}
			return false;
		}

		private bool IsDicomFile(string filePath) {
			try {
				DicomFile dicomFile = DicomFile.Open(filePath);
				return dicomFile != null && dicomFile.Dataset != null && dicomFile.Dataset.Contains(DicomTag.SOPClassUID);
			}
			catch {
				return false;
			}
		}
	}
}