using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using FellowOakDicom;

namespace VerteMarkPackager {
	public partial class MainWindow : Window {

		string? savePath;
		string? folderPath;
		int? count;

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

		private async void OnCreateButtonClick(object sender, RoutedEventArgs e) {
			// Hide the CreateButton
			CreateButton.Visibility = Visibility.Collapsed;

			if (!int.TryParse(DicomFilesCountTextBox.Text, out int value)) {
				System.Windows.MessageBox.Show($"Prosím, vyberte hodnotu pouze mezi {DicomFilesCountSlider.Minimum} a {DicomFilesCountSlider.Maximum}. Pro neomezený výběr zvolte hodnotu 0.", "Chybná hodnota", MessageBoxButton.OK, MessageBoxImage.Warning);
				// Show the CreateButton again on error
				CreateButton.Visibility = Visibility.Visible;
			}
			else {
				int result = await StartAsync();
				switch (result) {
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
			CreateButton.Visibility = Visibility.Visible;
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

		private void UpdateProgress(int percentage) {
			Dispatcher.Invoke(() => {
				ProgressBar.Value = percentage;
				if (ProgressBar.Visibility == Visibility.Collapsed) {
					ProgressBar.Visibility = Visibility.Visible;
				}
			});
		}

		/*========================================================
		BACKEND
		========================================================*/

		private async Task<int> StartAsync() {
			count = Convert.ToInt32(DicomFilesCountSlider.Value);

			// Check directories existence
			if (!CheckFolder(SaveDirectoryTextBox.Text) || !CheckFolder(DicomFilesDirectoryTextBox.Text)) {
				return 1;
			}

			// Check if DICOM files exist in the folder
			if (!CheckDicoms()) {
				return 2;
			}

			CreateFolder();

			// Run the zipping process in the background
			await Task.Run(() => CreateDicomZips(UpdateProgress));

			ProgressBar.Visibility = Visibility.Collapsed;

			return 0;
		}

		private void CreateFolder() {
			folderPath = DicomFilesDirectoryTextBox.Text;
			savePath = Path.Combine(SaveDirectoryTextBox.Text, "VerteMarkPack");
			Directory.CreateDirectory(savePath);
		}

		private void CreateDicomZips(Action<int> reportProgress) {
			var dicomFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly);
			int fileIndex = 0;
			int zipIndex = 0;
			int totalFiles = dicomFiles.Length;

			// If count is set to 0, include all files in one ZIP
			if (count == 0) {
				count = totalFiles;
			}

			while (fileIndex < dicomFiles.Length) {
				string date = DateTime.Now.ToString("ddMMyy");
				string zipFileName = Path.Combine(savePath, $"{date}_{zipIndex + 1}.vmk");

				// Creating ZIP file with SharpZipLib
				using (var zipStream = new FileStream(zipFileName, FileMode.Create))
				using (var zipWriter = new ZipOutputStream(zipStream)) {
					zipWriter.SetLevel(0); // Set compression level to 0 (no compression)

					string dicomsFolderInZip = "dicoms/";

					for (int i = 0; i < count && fileIndex < dicomFiles.Length; i++, fileIndex++) {
						string dicomFilePath = dicomFiles[fileIndex];
						string dicomFileName = Path.GetFileName(dicomFilePath);
						string entryName = dicomsFolderInZip + dicomFileName;

						// Adding file to ZIP without compression
						var entry = new ZipEntry(entryName) {
							DateTime = DateTime.Now,
							Size = new FileInfo(dicomFilePath).Length,
							CompressionMethod = CompressionMethod.Stored // No compression
						};

						zipWriter.PutNextEntry(entry);

						// Copying file into ZIP archive
						using (var fileStream = new FileStream(dicomFilePath, FileMode.Open, FileAccess.Read)) {
							fileStream.CopyTo(zipWriter);
						}

						zipWriter.CloseEntry();

						// Report progress
						int progressPercentage = (int)((fileIndex / (double)totalFiles) * 100);
						reportProgress(progressPercentage);
					}
				}

				zipIndex++;
			}

			// Ensure progress reaches 100% at the end
			reportProgress(100);
		}

		private bool CheckFolder(string path) {
			return Directory.Exists(path);
		}

		private bool CheckDicoms() {
			var files = Directory.GetFiles(DicomFilesDirectoryTextBox.Text);
			foreach (var file in files) {
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
