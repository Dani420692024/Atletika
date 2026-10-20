using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace Atletika
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Versenyzo> Versenyzok { get; } = new();
        private ICollectionView nezet;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            nezet = CollectionViewSource.GetDefaultView(Versenyzok);
            nezet.Filter = RekordSzures;

            LetszamFrissit();
        }

        private void Eletkor_Valtozott(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtEletkor == null) return;

            txtEletkor.Text = (int)e.NewValue + " év";
        }

        private void Szuro_Valtozott(object sender, SelectionChangedEventArgs e)
        {
            if (txtLetszam == null) return;

            nezet.Refresh();
            LetszamFrissit();
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
                ShowWarning("A névben pontosvessző és függőleges vonal nem használható.");
                return;
            }

            if (cmbVersenyszam.SelectedItem == null)
            {
                ShowWarning("Válasszon versenyszámot.");
                return;
            }

            if (rbFerfi.IsChecked != true && rbNo.IsChecked != true)
            {
                ShowWarning("Adja meg a versenyző nemét.");
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

            Versenyzo newRacer = new(raceNumber, nev, raceType, rbFerfi.IsChecked == true ? "F" : "N", chosenAge, chkOlimpiai.IsChecked == true, chkVB.IsChecked == true, chkEB.IsChecked == true);

            Versenyzok.Add(newRacer);
            LetszamFrissit();

            txtNev.Clear();
            txtRajtszam.Clear();

            MessageBox.Show("A regisztráció sikeres. Kiosztott rajtszám: " + raceNumber, "Információ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Mentes_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new();
            dialog.FileName = "atletika";
            dialog.Filter = "CSV-fájl (*.csv)|*.csv|Szövegfájl (*.txt)|*.txt";
            dialog.FilterIndex = 1;

            if (dialog.ShowDialog() != true) return;

            string fileName = dialog.FileName;
            string extension = Path.GetExtension(fileName).ToLower();
            string expectedExtension = dialog.FilterIndex == 1 ? ".csv" : ".txt";

            if (extension == "")
            {
                fileName += expectedExtension;
            }
            else if (extension != expectedExtension)
            {
                ShowWarning("A kiválasztott fájltípus és a fájlnév kiterjesztése nem egyezik.");
                return;
            }

            try
            {
                StreamWriter sw = new StreamWriter(fileName, false, Encoding.UTF8);

                if (dialog.FilterIndex == 1)
                {
                    sw.WriteLine("Rajtszam;Nev;Versenyszam;Nem;Eletkor;OR;VR;ER");

                    foreach (Versenyzo v in Versenyzok)
                    {
                        sw.WriteLine(string.Join(";", v.Rajtszam, v.Nev, v.Versenyszam, v.Nem, v.Eletkor, v.OR ? 1 : 0, v.VR ? 1 : 0, v.ER ? 1 : 0));
                    }
                }
                else
                {
                    for (int i = 0; i < Versenyzok.Count; i++)
                    {
                        Versenyzo v = Versenyzok[i];

                        if (i > 0) sw.WriteLine();

                        sw.WriteLine(v.Nev);
                        sw.WriteLine("A " + v.Eletkor + " éves " + (v.Nem == "F" ? "férfi" : "női") + " versenyző a(z) " + v.Versenyszam + " versenyszámban indul, rajtszáma " + v.Rajtszam + ". " + "Olimpiai csúccsal " + Rendelkezik(v.OR) + ", világcsúccsal " + Rendelkezik(v.VR) + ", Európa-csúccsal " + Rendelkezik(v.ER) + ".");
                    }
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

            if (dialog.ShowDialog() != true) return;

            List<Versenyzo> newData = new();
            List<int> newRaceNumbers = new();

            try
            {
                StreamReader sr = new (dialog.FileName, Encoding.UTF8);

                sr.ReadLine();
                while (!sr.EndOfStream)
                {
                    string sor = sr.ReadLine()!;

                    if (string.IsNullOrEmpty(sor.Trim())) continue;

                    Versenyzo racer = new(sor);

                    if (newRaceNumbers.Contains(racer.Rajtszam))
                    {
                        sr.Close();
                        MessageBox.Show("A fájlban ismétlődő rajtszám található: " + racer.Rajtszam +"\nA betöltés megszakadt, a korábbi lista megmaradt.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    newRaceNumbers.Add(racer.Rajtszam);
                    newData.Add(racer);
                }

                sr.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a betöltés közben: " + ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Versenyzok.Clear();

            foreach (Versenyzo v in newData) Versenyzok.Add(v);

            LetszamFrissit();
        }

        private bool RekordSzures(object item)
        {
            Versenyzo v = (Versenyzo)item;

            return cmbRekordSzuro.SelectedIndex switch
            {
                1 => v.OR,
                2 => v.VR,
                3 => v.ER,
                _ => true
            };
        }

        private string Rendelkezik(bool van)
        {
            return van ? "rendelkezik" : "nem rendelkezik";
        }

        private void ShowWarning(string uzenet)
        {
            MessageBox.Show(uzenet, "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void LetszamFrissit()
        {
            txtLetszam.Text = "Látható: " + nezet.Cast<Versenyzo>().Count() + " / Összesen: " + Versenyzok.Count;
        }

        private List<int> GetUsedRaceNumbers()
        {
            return Versenyzok.Select(v => v.Rajtszam).ToList();
        }
    }
}
