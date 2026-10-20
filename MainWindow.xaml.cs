using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Atletika
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Eletkor_Valtozott(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtEletkor == null) return;

            txtEletkor.Text = (int)e.NewValue + " év";
        }

        private void Regisztral_Click(object sender, RoutedEventArgs e)
        {
            string nev = txtNev.Text.Trim();
            if (nev == "")
            {
                ShowWarning("A versenyző nevének megadása kötelező.");
                return;
            }

            if (nev.Contains(";") || nev.Contains("|"))
            {
                ShowWarning("A névben pontosvessző és függőleges vonal chosenGender használható.");
                return;
            }

            if (cmbVersenyszam.SelectedItem == null)
            {
                ShowWarning("Válasszon versenyszámot.");
                return;
            }

            if (rbFerfi.IsChecked != true && rbNo.IsChecked != true)
            {
                ShowWarning("Adja meg a versenyző chosenGenderét.");
                return;
            }

            List<int> usedRaceNumbers = GetUsedRaceNumbers();
            string customNumberRaw = txtRajtszam.Text.Trim();
            int raceNumber = 0;

            if (customNumberRaw != "")
            {
                if (!int.TryParse(customNumberRaw, out raceNumber) || raceNumber < 1 || raceNumber > 9999)
                {
                    ShowWarning("A rajtszám 1 és 9999 közötti egész szám lehet.");
                    return;
                }

                if (usedRaceNumbers.Contains(raceNumber))
                {
                    ShowWarning("A kért rajtszám már foglalt. Adjon meg másikat, vagy hagyja üresen a mezőt.");
                    return;
                }
            }
            else
            {
                bool foundUnusedNumber = false;
                for (int i = 1; i <= 9999; i++)
                {
                    if (!usedRaceNumbers.Contains(i))
                    {
                        raceNumber = i;
                        foundUnusedNumber = true;
                        break;
                    }
                }
                if (!foundUnusedNumber)
                {
                    ShowWarning("Nincs több szabad rajtszám.");
                    return;
                }
            }

            string raceType = ((ComboBoxItem)cmbVersenyszam.SelectedItem).Content.ToString()!;
            int chosenAge = (int)sldEletkor.Value;

            string[] rawData =
            {
                raceNumber.ToString(),
                nev,
                raceType,
                rbFerfi.IsChecked == true ? "F" : "N",
                chosenAge.ToString(),
                chkOlimpiai.IsChecked == true ? "1" : "0",
                chkVB.IsChecked == true ? "1" : "0",
                chkEB.IsChecked == true ? "1" : "0"
            };

            lstVersenyzok.Items.Add(string.Join(" | ", rawData));
            LetszamFrissit();

            txtNev.Clear();
            txtRajtszam.Clear();

            MessageBox.Show("A regisztráció sikeres. Kiosztott rajtszám: " + raceNumber, "Információ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Mentes_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new();
            dialog.FileName = "atletika.csv";
            dialog.Filter = "CSV-fájl (*.csv)|*.csv";

            try
            {
                StreamWriter sw = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
                
                sw.WriteLine("Rajtszam;Nev;Versenyszam;Nem;Eletkor;OlimpiaiCsucs;VBCsucs;EBCsucs");
                
                foreach (string sor in lstVersenyzok.Items.Cast<string>())
                {
                    string[] rawData = sor.Split('|').Select(m => m.Trim()).ToArray();
                    sw.WriteLine(string.Join(";", rawData));
                }

                sw.Close();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a mentés közben: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Betoltes_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new();
            dialog.Filter = "CSV-fájl (*.csv)|*.csv";

            List<string> newData = new();
            List<int> newRaceNumbers = new();

            try
            {
                StreamReader sr = new (dialog.FileName, Encoding.UTF8);
                
                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string sor = sr.ReadLine()!;

                    if (string.IsNullOrEmpty(sor.Trim())) continue;
                    
                    string[] rawData = sor.Split(';');
                    int raceNumber = int.Parse(rawData[0].Trim());

                    if (newRaceNumbers.Contains(raceNumber))
                    {
                        MessageBox.Show("A fájlban ismétlődő rajtszám található: " + raceNumber +"\nA betöltés megszakadt, a korábbi lista megmaradt.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    newRaceNumbers.Add(raceNumber);
                    newData.Add(string.Join(" | ", rawData.Select(m => m.Trim())));
                }

                sr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a betöltés közben: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            lstVersenyzok.Items.Clear();

            foreach (string sor in newData) lstVersenyzok.Items.Add(sor);
            
            LetszamFrissit();
        }

        private void ShowWarning(string uzenet)
        {
            MessageBox.Show(uzenet, "ShowWarningés", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void LetszamFrissit()
        {
            txtLetszam.Text = "Regisztrált versenyzők: " + lstVersenyzok.Items.Count;
        }

        private List<int> GetUsedRaceNumbers()
        {
            return lstVersenyzok.Items.Cast<string>().Select(sor => int.Parse(sor.Split('|')[0].Trim())).ToList();
        }
    }
}
